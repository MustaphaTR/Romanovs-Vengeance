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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI load unit related with " + nameof(SharedCargo) + " and " + nameof(SharedPassenger) + " traits.")]
	public class SharedCargoBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that can be targeted for load, must have " + nameof(SharedCargo) + ".")]
		public readonly HashSet<string> Transports = default;

		[Desc("Actor types that used for loading, must have " + nameof(SharedPassengerInfo) + ".")]
		public readonly HashSet<string> Passengers = default;

		[Desc("Actor relationship that can be targeted for load.")]
		public readonly bool OnlyEnterOwnerPlayer = true;

		[Desc("Scan suitable actors and target in this interval.")]
		public readonly int ScanTick = 443;

		[Desc("Load passengers max to this amount per scan.")]
		public readonly int PassengersPerScan = 2;

		[Desc("Load passengers max to this amount.")]
		public readonly int MaxPassengers = 6;

		[Desc("Don't load passengers to this actor if damage state is worse than this.")]
		public readonly DamageState ValidDamageState = DamageState.Heavy;

		[Desc("Don't load passengers that are further than this distance to this actor.")]
		public readonly WDist MaxDistance = WDist.FromCells(40);

		public override object Create(ActorInitializer init) { return new SharedCargoBotModule(init.Self, this); }
	}

	public class SharedCargoBotModule : ConditionalTrait<SharedCargoBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrdered;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;
		readonly Predicate<Actor> invalidTransport;

		readonly List<UnitWposWrapper> activePassengers = new();
		readonly List<Actor> stuckPassengers = new();
		int minAssignRoleDelayTicks;
		SharedCargoManager sharedCargoManager;

		public SharedCargoBotModule(Actor self, SharedCargoBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			if (info.OnlyEnterOwnerPlayer)
				invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			else
				invalidTransport = a => a == null || a.IsDead || !a.IsInWorld || a.Owner.RelationshipWith(player) != PlayerRelationship.Ally;
			unitCannotBeOrdered = a => a == null || a.IsDead || !a.IsInWorld || a.Owner != player;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || !(a.IsIdle || a.CurrentActivity is FlyIdle);
			unitCannotBeOrderedOrIsIdle = a => unitCannotBeOrdered(a) || a.IsIdle || a.CurrentActivity is FlyIdle;
		}

		protected override void Created(Actor self)
		{
			sharedCargoManager = self.TraitsImplementing<SharedCargoManager>().FirstOrDefault();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = world.LocalRandom.Next(0, Info.ScanTick);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--minAssignRoleDelayTicks <= 0 && sharedCargoManager != null && Info.MaxPassengers > sharedCargoManager.PassengerCount)
			{
				minAssignRoleDelayTicks = Info.ScanTick;

				activePassengers.RemoveAll(u => unitCannotBeOrderedOrIsIdle(u.Actor));
				stuckPassengers.RemoveAll(a => unitCannotBeOrdered(a));
				for (var i = 0; i < activePassengers.Count; i++)
				{
					var p = activePassengers[i];
					if (p.Actor.CurrentActivity.ChildActivity != null
						&& p.Actor.CurrentActivity.ChildActivity.ActivityType == ActivityType.Move
						&& p.Actor.CenterPosition == p.WPos)
					{
						stuckPassengers.Add(p.Actor);
						bot.QueueOrder(new Order("Stop", p.Actor, false));
						activePassengers.RemoveAt(i);
						i--;
					}

					p.WPos = p.Actor.CenterPosition;
				}

				if (!sharedCargoManager.HasSpace(1))
					return;

				var tcs = world.ActorsWithTrait<SharedCargo>().Where(
				at =>
				{
					if (!Info.Transports.Contains(at.Actor.Info.Name) || at.Trait.IsTraitDisabled || invalidTransport(at.Actor))
						return false;

					var health = at.Actor.TraitOrDefault<IHealth>()?.DamageState;
					return health == null || health < Info.ValidDamageState;
				}).ToArray();

				if (tcs.Length == 0)
					return;

				var tc = tcs.Random(world.LocalRandom);
				var cargo = tc.Trait;
				var transport = tc.Actor;
				var spaceTaken = 0;

				var passengers = world.ActorsWithTrait<SharedPassenger>().Where(at => !unitCannotBeOrderedOrIsBusy(at.Actor)
					&& Info.Passengers.Contains(at.Actor.Info.Name)
					&& !stuckPassengers.Contains(at.Actor)
					&& sharedCargoManager.HasSpace(at.Trait.Info.Weight)
					&& (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared <= Info.MaxDistance.LengthSquared)
						.OrderBy(at => (at.Actor.CenterPosition - transport.CenterPosition).HorizontalLengthSquared);

				var orderedActors = new List<Actor>();

				var passengerCount = 0;
				foreach (var p in passengers)
				{
					if (!AIUtils.PathExist(p.Actor, transport.Location, transport))
						continue;

					if (sharedCargoManager.HasSpace(spaceTaken + p.Trait.Info.Weight))
					{
						spaceTaken += p.Trait.Info.Weight;
						orderedActors.Add(p.Actor);
						passengerCount++;
						activePassengers.Add(new UnitWposWrapper(p.Actor));
					}

					if (!sharedCargoManager.HasSpace(spaceTaken + 1) || passengerCount >= Info.PassengersPerScan)
						break;
				}

				if (orderedActors.Count > 0)
					bot.QueueOrder(new Order("EnterSharedTransport", null, Target.FromActor(transport), false, groupedActors: orderedActors.ToArray()));
			}
		}
	}
}
