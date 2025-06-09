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
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI load unit related with " + nameof(Garrisonable) + " and " + nameof(Garrisoner) + " traits.")]
	public class LoadGarrisonerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that can be targeted for load, must have " + nameof(Garrisonable) + ".",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> GarrisonableUnit = null;

		[Desc("Actor types that used for loading, must have " + nameof(Passenger) + ".",
			"Leave this empty to include all actors.")]
		public readonly HashSet<string> GarrisonerUnit = null;

		[Desc("Scan suitable actors and target in this interval.")]
		public readonly int ScanTick = 457;

		[Desc("Don't load Garrisoner to this actor if damage state is worse than this.")]
		public readonly DamageState ValidDamageState = DamageState.Heavy;

		[Desc("Load passengers max to this amount per scan.")]
		public readonly int PassengersPerScan = 2;

		public override object Create(ActorInitializer init) { return new LoadGarrisonerBotModule(init.Self, this); }
	}

	public class LoadGarrisonerBotModule : ConditionalTrait<LoadGarrisonerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;
		readonly Predicate<Actor> invalidTransport;

		readonly List<UnitWposWrapper> activeGarrisoner = new();
		readonly List<Actor> stuckGarrisoner = new();
		int minAssignRoleDelayTicks;

		public LoadGarrisonerBotModule(Actor self, LoadGarrisonerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || (a.Owner.RelationshipWith(player) != PlayerRelationship.Neutral && a.Owner != player);
			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || !(a.IsIdle || a.CurrentActivity is FlyIdle);
			unitCannotBeOrderedOrIsIdle = a => unitCannotBeOrdered(a) || a.IsIdle || a.CurrentActivity is FlyIdle;
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = world.LocalRandom.Next(0, Info.ScanTick);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--minAssignRoleDelayTicks <= 0)
			{
				minAssignRoleDelayTicks = Info.ScanTick;

				activeGarrisoner.RemoveAll(u => unitCannotBeOrderedOrIsIdle(u.Actor));
				stuckGarrisoner.RemoveAll(a => unitCannotBeOrdered(a));
				for (var i = 0; i < activeGarrisoner.Count; i++)
				{
					var p = activeGarrisoner[i];
					if (p.Actor.CurrentActivity.ChildActivity != null
						&& p.Actor.CurrentActivity.ChildActivity.ActivityType == ActivityType.Move
						&& p.Actor.CenterPosition == p.WPos)
					{
						stuckGarrisoner.Add(p.Actor);
						bot.QueueOrder(new Order("Stop", p.Actor, false));
						activeGarrisoner.RemoveAt(i);
						i--;
					}

					p.WPos = p.Actor.CenterPosition;
				}

				var tcs = world.ActorsWithTrait<Garrisonable>().Where(
				at =>
				{
					var health = at.Actor.TraitOrDefault<IHealth>()?.DamageState;
					return (Info.GarrisonableUnit == null || Info.GarrisonableUnit.Contains(at.Actor.Info.Name)) && !invalidTransport(at.Actor)
					&& at.Trait.HasSpace(1) && (health == null || health < Info.ValidDamageState);
				}).ToArray();

				if (tcs.Length == 0)
					return;

				var tc = tcs.Random(world.LocalRandom);
				var garrisonable = tc.Trait;
				var transport = tc.Actor;
				var spaceTaken = 0;

				var garrisoner = world.ActorsWithTrait<Garrisoner>().Where(at => !unitCannotBeOrderedOrIsBusy(at.Actor)
					&& (Info.GarrisonerUnit == null || Info.GarrisonerUnit.Contains(at.Actor.Info.Name))
					&& !stuckGarrisoner.Contains(at.Actor)
					&& garrisonable.HasSpace(at.Trait.Info.Weight))
						.OrderBy(at => (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared);

				var orderedActors = new List<Actor>();

				var passengerCount = 0;
				foreach (var g in garrisoner)
				{
					if (!AIUtils.PathExist(g.Actor, transport.Location, transport))
						continue;

					if (garrisonable.HasSpace(spaceTaken + g.Trait.Info.Weight))
					{
						spaceTaken += g.Trait.Info.Weight;
						orderedActors.Add(g.Actor);
						activeGarrisoner.Add(new UnitWposWrapper(g.Actor));
						passengerCount++;
					}

					if (!garrisonable.HasSpace(spaceTaken + 1) || passengerCount >= Info.PassengersPerScan)
						break;
				}

				if (orderedActors.Count > 0)
					bot.QueueOrder(new Order("EnterGarrison", null, Target.FromActor(transport), false, groupedActors: orderedActors.ToArray()));
			}
		}
	}
}
