#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI MCVs For SP. Focus on aircraft MCV, MCV stuck problem and MCV build control")]
	public class McvManagerASBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that are considered MCVs (deploy into base builders).")]
		public readonly HashSet<string> McvTypes = new();

		[Desc("Actor types that are considered construction yards (base builders).")]
		public readonly HashSet<string> ConstructionYardTypes = new();

		[Desc("Actor types that are able to produce MCVs.")]
		public readonly HashSet<string> McvFactoryTypes = new();

		[Desc("Try to maintain at least this many ConstructionYardTypes, build an MCV if number is below this.",
			"Increased by AddtionalConstructionYardCount after AddtionalConstructionYardInterval, max to MaxmiumConstructionYardCount")]
		public readonly int MinimumConstructionYardCount = 1;

		[Desc("See description of MinimumConstructionYardCount")]
		public readonly int AddtionalConstructionYardInterval = 8000;

		[Desc("See description of MinimumConstructionYardCount")]
		public readonly int AddtionalConstructionYardCount = 1;

		[Desc("See description of MinimumConstructionYardCount")]
		public readonly int MaxmiumConstructionYardCount = 1;

		[Desc("Delay (in ticks) between looking for and giving out orders to new MCVs.")]
		public readonly int ScanForNewMcvInterval = 20;

		[Desc("Minimum distance in cells from center of the base when checking for MCV deployment location.")]
		public readonly int MinBaseRadius = 2;

		[Desc("Maximum distance in cells from center of the base when checking for MCV deployment location.",
			"Only applies if RestrictMCVDeploymentFallbackToBase is enabled and there's at least one construction yard.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Should deployment of additional MCVs be restricted to MaxBaseRadius if explicit deploy locations are missing or occupied?")]
		public readonly bool RestrictMCVDeploymentFallbackToBase = true;

		public override object Create(ActorInitializer init) { return new McvManagerASBotModule(init.Self, this); }
	}

	public class McvManagerASBotModule : ConditionalTrait<McvManagerASBotModuleInfo>,
		IBotTick, IBotPositionsUpdated, IGameSaveTraitData, INotifyActorDisposing
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = world.Actors.Where(a => a.Owner == player &&
				Info.ConstructionYardTypes.Contains(a.Info.Name))
				.RandomOrDefault(world.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		readonly World world;
		readonly Player player;
		readonly ActorIndex.OwnerAndNamesAndTrait<TransformsInfo> mcvs;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> constructionYards;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> mcvFactories;

		readonly Predicate<Actor> unitCannotBeOrdered;

		IBotPositionsUpdated[] notifyPositionsUpdated;
		IBotRequestUnitProduction[] requestUnitProduction;
		readonly List<UnitWposWrapper> activeMCV = new();

		CPos initialBaseCenter;
		int scanInterval;
		bool firstTick = true;
		int baseShouldHave;
		int countdown;

		public McvManagerASBotModule(Actor self, McvManagerASBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			unitCannotBeOrdered = a => a == null || a.Owner != player || a.IsDead || !a.IsInWorld;
			baseShouldHave = info.MinimumConstructionYardCount;
			countdown = info.AddtionalConstructionYardInterval;
			mcvs = new ActorIndex.OwnerAndNamesAndTrait<TransformsInfo>(world, info.McvTypes, player);
			constructionYards = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			mcvFactories = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.McvFactoryTypes, player);
		}

		protected override void Created(Actor self)
		{
			notifyPositionsUpdated = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			requestUnitProduction = self.Owner.PlayerActor.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			scanInterval = world.LocalRandom.Next(Info.ScanForNewMcvInterval, Info.ScanForNewMcvInterval * 2);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		void IBotTick.BotTick(IBot bot)
		{
			if (--countdown <= 0)
			{
				countdown = Info.AddtionalConstructionYardInterval;
				baseShouldHave += Info.AddtionalConstructionYardCount;

				if (baseShouldHave >= Info.MaxmiumConstructionYardCount)
				{
					baseShouldHave = Info.MaxmiumConstructionYardCount;
					countdown = int.MaxValue;
				}
			}

			if (firstTick)
			{
				DeployMcvsFirstTick(bot);
				firstTick = false;
			}

			if (--scanInterval <= 0)
			{
				scanInterval = Info.ScanForNewMcvInterval;
				DeployMcvs(bot, true);

				// No construction yards - Build a new MCV
				if (ShouldBuildMCV())
				{
					var unitBuilder = Array.Find(requestUnitProduction, Exts.IsTraitEnabled);
					if (unitBuilder != null)
					{
						var mcvType = Info.McvTypes.Random(world.LocalRandom);
						if (unitBuilder.RequestedProductionCount(bot, mcvType) == 0)
							unitBuilder.RequestUnitProduction(bot, mcvType);
					}
				}
			}
		}

		bool ShouldBuildMCV()
		{
			// Only build MCV if we don't already have one in the field.
			var allowedToBuildMCV = AIUtils.CountActorByCommonName(mcvs) == 0;
			if (!allowedToBuildMCV)
				return false;

			// Build MCV if we don't have the desired number of construction yards, unless we have no factory (can't build it).
			return AIUtils.CountActorByCommonName(constructionYards) < baseShouldHave &&
				AIUtils.CountActorByCommonName(mcvFactories) > 0;
		}

		void DeployMcvsFirstTick(IBot bot)
		{
			var newMCVs = world.ActorsHavingTrait<Transforms>()
				.Where(a => Info.McvTypes.Contains(a.Info.Name) && !unitCannotBeOrdered(a));

			foreach (var mcv in newMCVs)
				DeployMcv(bot, mcv, false, false);
		}

		void DeployMcvs(IBot bot, bool chooseLocation)
		{
			for (var i = 0; i < activeMCV.Count; i++)
			{
				var mw = activeMCV[i];

				if (unitCannotBeOrdered(mw.Actor))
				{
					activeMCV.RemoveAt(i);
					i--;
					continue;
				}

				if (mw.WPos == mw.Actor.CenterPosition)
					DeployMcv(bot, mw.Actor, chooseLocation, false);

				mw.WPos = mw.Actor.CenterPosition;
			}

			var newMCVs = world.ActorsHavingTrait<Transforms>()
				.Where(a => Info.McvTypes.Contains(a.Info.Name) && !unitCannotBeOrdered(a));

			foreach (var mcv in newMCVs)
			{
				var skip = false;

				foreach (var amcv in activeMCV)
				{
					if (mcv == amcv.Actor)
					{
						skip = true;
						break;
					}
				}

				if (!skip)
					activeMCV.Add(new UnitWposWrapper(mcv));
			}
		}

		// Find any MCV and deploy them at a sensible location.
		void DeployMcv(IBot bot, Actor mcv, bool move, bool queueOrder)
		{
			if (move)
			{
				// If we lack a base, we need to make sure we don't restrict deployment of the MCV to the base!
				var restrictToBase = Info.RestrictMCVDeploymentFallbackToBase && AIUtils.CountActorByCommonName(constructionYards) > 0;

				var transformsInfo = mcv.Info.TraitInfo<TransformsInfo>();
				var desiredLocation = ChooseMcvDeployLocation(transformsInfo.IntoActor, transformsInfo.Offset, restrictToBase);
				if (desiredLocation == null)
					return;

				bot.QueueOrder(new Order("Move", mcv, Target.FromCell(world, desiredLocation.Value), queueOrder));
			}

			// If the MCV has to move first, we can't be sure it reaches the destination alive, so we only
			// update base and defense center if the MCV is deployed immediately (i.e. at game start).
			// TODO: This could be addressed via INotifyTransform.
			foreach (var n in notifyPositionsUpdated)
			{
				n.UpdatedBaseCenter(mcv.Location);
				n.UpdatedDefenseCenter(mcv.Location);
			}

			bot.QueueOrder(new Order("DeployTransform", mcv, true));
		}

		CPos? ChooseMcvDeployLocation(string actorType, CVec offset, bool distanceToBaseIsImportant)
		{
			var actorInfo = world.Map.Rules.Actors[actorType];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return null;

			// Find the buildable cell that is closest to pos and centered around center
			var baseCenter = GetRandomBaseCenter();
			return ((Func<CPos, CPos, int, int, CPos?>)((center, target, minRange, maxRange) =>
				{
					var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);

					// Sort by distance to target if we have one
					if (center != target)
						cells = cells.OrderBy(c => (c - target).LengthSquared);
					else
						cells = cells.Shuffle(world.LocalRandom);

					foreach (var cell in cells)
						if (world.CanPlaceBuilding(cell + offset, actorInfo, bi, null))
							return cell;

					return null;
				}))(baseCenter, baseCenter, Info.MinBaseRadius,
				distanceToBaseIsImportant ? Info.MaxBaseRadius : world.Map.Grid.MaximumTileSearchRange);
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new("BaseShouldHave", FieldSaver.FormatValue(baseShouldHave)),
				new("Countdown", FieldSaver.FormatValue(countdown)),
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();

			if (nodes.TryGetValue("InitialBaseCenter", out var initialBaseCenterNode))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value);

			if (nodes.TryGetValue("BaseShouldHave", out var baseShouldHaveNode))
				baseShouldHave = FieldLoader.GetValue<int>("BaseShouldHave", baseShouldHaveNode.Value);

			if (nodes.TryGetValue("Countdown", out var countdownNode))
				countdown = FieldLoader.GetValue<int>("Countdown", countdownNode.Value);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			mcvs.Dispose();
			constructionYards.Dispose();
			mcvFactories.Dispose();
		}
	}
}
