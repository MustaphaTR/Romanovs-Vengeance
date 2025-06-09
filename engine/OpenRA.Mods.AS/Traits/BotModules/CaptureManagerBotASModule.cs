#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI capturing logic.")]
	public class CaptureManagerBotASModuleInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Actor types that can capture other actors (via `Captures`).")]
		public readonly HashSet<string> CapturingActorTypes = new();

		[Desc("Percentage chance of trying a priority capture.")]
		public readonly int PriorityCaptureChance = 75;

		[Desc("Actor types that should be priorizited to be captured.",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> PriorityCapturableActorTypes = new();

		[Desc("Actor types that can be targeted for capturing.",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> CapturableActorTypes = new();

		[Desc("Minimum delay (in ticks) between trying to capture with CapturingActorTypes.")]
		public readonly int MinimumCaptureDelay = 375;

		[Desc("Maximum number of options to consider for capturing.",
			"If a value less than 1 is given 1 will be used instead.")]
		public readonly int MaximumCaptureTargetOptions = 10;

		[Desc("Should visibility (Shroud, Fog, Cloak, etc) be considered when searching for capturable targets?")]
		public readonly bool CheckCaptureTargetsForVisibility = true;

		[Desc("Player stances that capturers should attempt to target.")]
		public readonly PlayerRelationship CapturableRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		public override object Create(ActorInitializer init) { return new CaptureManagerBotASModule(init.Self, this); }
	}

	public class CaptureManagerBotASModule : ConditionalTrait<CaptureManagerBotASModuleInfo>, IBotTick, IBotPositionsUpdated, IGameSaveTraitData
	{
		readonly World world;
		readonly Player player;
		readonly Func<Actor, bool> isEnemyUnit;
		readonly int maximumCaptureTargetOptions;

		int minCaptureDelayTicks;
		CPos initialBaseCenter;

		public CaptureManagerBotASModule(Actor self, CaptureManagerBotASModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			if (world.Type == WorldType.Editor)
				return;

			isEnemyUnit = unit =>
				player.RelationshipWith(unit.Owner) == PlayerRelationship.Enemy
					&& !unit.Info.HasTraitInfo<HuskInfo>()
					&& unit.Info.HasTraitInfo<ITargetableInfo>();

			maximumCaptureTargetOptions = Math.Max(1, Info.MaximumCaptureTargetOptions);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation) { }

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minCaptureDelayTicks = world.LocalRandom.Next(Info.MinimumCaptureDelay);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--minCaptureDelayTicks <= 0)
			{
				minCaptureDelayTicks = Info.MinimumCaptureDelay;
				QueueCaptureOrders(bot);
			}
		}

		internal Actor FindClosestEnemy(WPos pos)
		{
			return world.Actors.Where(isEnemyUnit).ClosestToIgnoringPath(pos);
		}

		internal Actor FindClosestEnemy(WPos pos, WDist radius)
		{
			return world.FindActorsInCircle(pos, radius).Where(isEnemyUnit).ClosestToIgnoringPath(pos);
		}

		IEnumerable<Actor> GetVisibleActorsBelongingToPlayer(Player owner)
		{
			foreach (var actor in GetActorsThatCanBeOrderedByPlayer(owner))
				if (actor.CanBeViewedByPlayer(player))
					yield return actor;
		}

		IEnumerable<Actor> GetActorsThatCanBeOrderedByPlayer(Player owner)
		{
			foreach (var actor in world.Actors)
				if (actor.Owner == owner && !actor.IsDead && actor.IsInWorld)
					yield return actor;
		}

		void QueueCaptureOrders(IBot bot)
		{
			if (player.WinState != WinState.Undefined)
				return;

			var newUnits = world.ActorsHavingTrait<Captures>()
				.Where(a => a.Owner == player && !a.IsDead && a.IsInWorld);

			if (!newUnits.Any())
				return;

			var capturers = newUnits
				.Where(a => a.IsIdle && Info.CapturingActorTypes.Contains(a.Info.Name))
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null);

			if (!capturers.Any())
				return;

			var baseCenter = world.Map.CenterOfCell(initialBaseCenter);

			if (world.LocalRandom.Next(100) < Info.PriorityCaptureChance)
			{
				var priorityTargets = world.Actors.Where(a =>
					!a.IsDead && a.IsInWorld && Info.CapturableRelationships.HasRelationship(player.RelationshipWith(a.Owner))
					&& Info.PriorityCapturableActorTypes.Contains(a.Info.Name.ToLowerInvariant()));

				if (Info.CheckCaptureTargetsForVisibility)
					priorityTargets = priorityTargets.Where(a => a.CanBeViewedByPlayer(player));

				if (priorityTargets.Any())
				{
					priorityTargets = priorityTargets.OrderBy(a => (a.CenterPosition - baseCenter).LengthSquared);

					var priorityCaptures = Math.Min(capturers.Count(), priorityTargets.Count());

					for (var i = 0; i < priorityCaptures; i++)
					{
						var capturer = capturers.First();
						var priorityTarget = priorityTargets.First();

						var captureManager = priorityTarget.TraitOrDefault<CaptureManager>();
						if (captureManager != null && capturer.Trait.CanTarget(captureManager))
						{
							bot.QueueOrder(new Order("CaptureActor", capturer.Actor, Target.FromActor(priorityTarget), true));
							AIUtils.BotDebug("AI ({0}): Ordered {1} {2} to capture {3} {4} in priority mode.",
								player.ClientIndex, capturer.Actor, capturer.Actor.ActorID, priorityTarget, priorityTarget.ActorID);

							capturers = capturers.Skip(1);
						}

						priorityTargets = priorityTargets.Skip(1);
					}
				}

				if (!capturers.Any())
					return;
			}

			var randPlayer = world.Players.Where(p => !p.Spectating
				&& Info.CapturableRelationships.HasRelationship(player.RelationshipWith(p))).Random(world.LocalRandom);

			var targetOptions = Info.CheckCaptureTargetsForVisibility
				? GetVisibleActorsBelongingToPlayer(randPlayer)
				: GetActorsThatCanBeOrderedByPlayer(randPlayer);

			var capturableTargetOptions = targetOptions
				.Where(target =>
				{
					var captureManager = target.TraitOrDefault<CaptureManager>();
					if (captureManager == null)
						return false;

					return capturers.Any(tp => tp.Trait.CanTarget(captureManager));
				})
				.OrderBy(target => (target.CenterPosition - baseCenter).LengthSquared)
				.Take(maximumCaptureTargetOptions);

			if (Info.CapturableActorTypes.Count > 0)
				capturableTargetOptions = capturableTargetOptions.Where(target => Info.CapturableActorTypes.Contains(target.Info.Name.ToLowerInvariant()));

			if (!capturableTargetOptions.Any())
				return;

			foreach (var capturer in capturers)
			{
				var targetActor = capturableTargetOptions.MinByOrDefault(target => (target.CenterPosition - capturer.Actor.CenterPosition).LengthSquared);
				if (targetActor == null)
					continue;

				bot.QueueOrder(new Order("CaptureActor", capturer.Actor, Target.FromActor(targetActor), true));
				AIUtils.BotDebug("AI ({0}): Ordered {1} {2} to capture {3} {4}.",
					player.ClientIndex, capturer.Actor, capturer.Actor.ActorID, targetActor, targetActor.ActorID);
			}
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var nodes = data.ToDictionary();

			if (nodes.TryGetValue("InitialBaseCenter", out var initialBaseCenterNode))
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value);
		}
	}
}
