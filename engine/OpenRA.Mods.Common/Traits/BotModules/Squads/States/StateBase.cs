#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	abstract class StateBase
	{
		protected static void GoToRandomOwnBuilding(Squad squad)
		{
			var loc = RandomBuildingLocation(squad);
			foreach (var u in squad.Units)
				squad.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(squad.World, loc), false));
		}

		protected static CPos RandomBuildingLocation(Squad squad)
		{
			var location = squad.SquadManager.GetRandomBaseCenter();
			var buildings = squad.World.ActorsHavingTrait<Building>()
				.Where(a => a.Owner == squad.Bot.Player).ToList();
			if (buildings.Count > 0)
				location = buildings.Random(squad.Random).Location;

			return location;
		}

		// Deprecated old method
		protected static bool BusyAttack(Actor a)
		{
			if (a.IsIdle)
				return false;

			var isAttacking = false;
			var activity = a.CurrentActivity;
			var type = activity.GetType();

			if (type == typeof(Attack) || type == typeof(FlyAttack))
				isAttacking = true;
			else
			{
				var next = activity.NextActivity;
				if (next == null)
					return false;

				var nextType = next.GetType();
				if (nextType == typeof(Attack) || nextType == typeof(FlyAttack))
					isAttacking = true;
			}

			if (!isAttacking)
				return false;

			var arms = a.TraitsImplementing<Armament>();
			foreach (var arm in arms)
			{
				if (arm.IsTraitDisabled)
					continue;

				if ((arm.Info.TargetRelationships & PlayerRelationship.Enemy) != 0)
					return true;
			}

			return false;
		}

		protected static (bool IsFiring, bool TryAttacking) IsAttackingAndTryAttack(Actor a)
		{
			if (a.IsIdle)
				return (false, false);

			var isFiring = false;
			var tryAttacking = false;
			var activity = a.CurrentActivity;
			var type = activity.ActivityType;

			var arms = a.TraitsImplementing<Armament>();
			var isValid = false;
			foreach (var arm in arms)
			{
				if (arm.IsTraitDisabled)
					continue;

				if ((arm.Info.TargetRelationships & PlayerRelationship.Enemy) != 0)
				{
					isValid = true;
					break;
				}
			}

			if (!isValid)
				return (false, false);

			if (type == ActivityType.Attack)
			{
				tryAttacking = true;

				var childActivity = activity.ChildActivity;
				if (childActivity == null)
					isFiring = true;
				else
				{
					var childType = childActivity.ActivityType;
					if (childType != ActivityType.Move)
						isFiring = true;
				}
			}

			return (isFiring, tryAttacking);
		}

		protected static bool CanAttackTarget(Actor a, Actor target)
		{
			if (!a.Info.HasTraitInfo<AttackBaseInfo>())
				return false;

			var targetTypes = target.GetEnabledTargetTypes();
			if (targetTypes.IsEmpty)
				return false;

			var arms = a.TraitsImplementing<Armament>();
			foreach (var arm in arms)
			{
				if (arm.IsTraitDisabled)
					continue;

				if ((arm.Info.TargetRelationships & PlayerRelationship.Enemy) == 0)
					continue;

				if (arm.Weapon.IsValidTarget(targetTypes))
					return true;
			}

			return false;
		}

		protected virtual bool ShouldFlee(Squad squad, Func<IReadOnlyCollection<Actor>, bool> flee)
		{
			if (!squad.IsValid)
				return false;

			var randomSquadUnit = squad.Units.Random(squad.Random);
			var dangerRadius = squad.SquadManager.Info.DangerScanRadius;
			var units = squad.World.FindActorsInCircle(randomSquadUnit.Actor.CenterPosition, WDist.FromCells(dangerRadius)).ToList();

			// If there are any own buildings within the DangerRadius, don't flee
			// PERF: Avoid LINQ
			foreach (var u in units)
				if (u.Owner == squad.Bot.Player && u.Info.HasTraitInfo<BuildingInfo>())
					return false;

			var enemyAroundUnit = units
				.Where(unit => squad.SquadManager.IsPreferredEnemyUnit(unit) && unit.Info.HasTraitInfo<AttackBaseInfo>())
				.ToList();
			if (enemyAroundUnit.Count == 0)
				return false;

			return flee(enemyAroundUnit);
		}

		// Note: There is a simple check without using costy AttackOrFleeFuzzy
		protected virtual bool ShouldFleeSimple(Squad squad)
		{
			if (!squad.IsValid)
				return false;

			var squadUnit = squad.Units[0].Actor;
			var dangerRadius = squad.SquadManager.Info.DangerScanRadius;
			var units = squad.World.FindActorsInCircle(squadUnit.CenterPosition, WDist.FromCells(dangerRadius)).ToList();

			var enemyAroundUnit = units.Where(unit => squad.SquadManager.IsPreferredEnemyUnit(unit) && unit.Info.HasTraitInfo<AttackBaseInfo>()).ToList();
			if (enemyAroundUnit.Count == 0)
				return false;

			var panic = (enemyAroundUnit.Count + squad.Units.Count - units.Count) * (int)DamageState.Critical;
			foreach (var u in squad.Units)
			{
				var health = u.Actor.TraitOrDefault<IHealth>();
				if (health != null)
					panic += (int)health.DamageState;
			}

			if (panic > squad.Units.Count * (int)DamageState.Medium)
				return true;

			return false;
		}

		protected static bool IsRearming(Actor a)
		{
			return !a.IsIdle && (a.CurrentActivity.ActivitiesImplementing<Resupply>().Any() || a.CurrentActivity.ActivitiesImplementing<ReturnToBase>().Any());
		}

		protected static bool FullAmmo(IEnumerable<AmmoPool> ammoPools)
		{
			foreach (var ap in ammoPools)
				if (!ap.HasFullAmmo)
					return false;

			return true;
		}

		protected static bool HasAmmo(IEnumerable<AmmoPool> ammoPools)
		{
			foreach (var ap in ammoPools)
				if (!ap.HasAmmo)
					return false;

			return true;
		}

		protected static bool ReloadsAutomatically(IEnumerable<AmmoPool> ammoPools, Rearmable rearmable)
		{
			if (rearmable == null)
				return true;

			foreach (var ap in ammoPools)
				if (!rearmable.Info.AmmoPools.Contains(ap.Info.Name))
					return false;

			return true;
		}

		// Retreat units from combat, or for supply only in idle
		protected static void Retreat(Squad squad, bool flee, bool rearm, bool repair)
		{
			// HACK: "alreadyRepair" is to solve AI repair orders performance,
			// which is only allow one goes to repairpad at the same time to avoid queueing too many orders.
			// if repairpad logic is better we can just drop it.
			var alreadyRepair = false;

			var rearmingUnits = new List<Actor>();
			var fleeingUnits = new List<Actor>();

			foreach (var u in squad.Units)
			{
				if (IsRearming(u.Actor))
					continue;

				var orderQueued = false;

				// Units need to rearm will be added to rearming group.
				if (rearm)
				{
					var ammoPools = u.Actor.TraitsImplementing<AmmoPool>().ToArray();
					if (!ReloadsAutomatically(ammoPools, u.Actor.TraitOrDefault<Rearmable>()) && !FullAmmo(ammoPools))
					{
						rearmingUnits.Add(u.Actor);
						orderQueued = true;
					}
				}

				// Don't retreat unit that has a target
				if (IsAttackingAndTryAttack(u.Actor).IsFiring)
					continue;

				// Try repair units.
				// Don't use grounp order here becuase we have 2 kinds of repaid orders and we need to find repair building for both traits.
				if (repair && !alreadyRepair)
				{
					Actor repairBuilding = null;
					var orderId = "Repair";
					var health = u.Actor.TraitOrDefault<IHealth>();

					if (health != null && health.DamageState > DamageState.Undamaged)
					{
						var repairable = u.Actor.TraitOrDefault<Repairable>();
						if (repairable != null)
							repairBuilding = repairable.FindRepairBuilding(u.Actor);
						else
						{
							var repairableNear = u.Actor.TraitOrDefault<RepairableNear>();
							if (repairableNear != null)
							{
								orderId = "RepairNear";
								repairBuilding = repairableNear.FindRepairBuilding(u.Actor);
							}
						}

						if (repairBuilding != null)
						{
							squad.Bot.QueueOrder(new Order(orderId, u.Actor, Target.FromActor(repairBuilding), orderQueued));
							orderQueued = true;
							alreadyRepair = true;
						}
					}
				}

				// If there is no order in queue and units should flee, add unit to fleeing group.
				if (flee && !orderQueued)
					fleeingUnits.Add(u.Actor);
			}

			if (rearmingUnits.Count > 0)
				squad.Bot.QueueOrder(new Order("ReturnToBase", null, true, groupedActors: rearmingUnits.ToArray()));

			if (fleeingUnits.Count > 0)
				squad.Bot.QueueOrder(new Order("Move", null, Target.FromCell(squad.World, RandomBuildingLocation(squad)), false, groupedActors: fleeingUnits.ToArray()));
		}

		protected static UnitWposWrapper GetPathfindLeader(Squad squad, HashSet<string> locomotorTypes)
		{
			if (!squad.IsValid)
				return null;

			var nonAircraft = new UnitWposWrapper(null); // HACK: Becuase Mobile is always affected by terrain, so we always select a nonAircraft as leader
			foreach (var u in squad.Units)
			{
				var mt = u.Actor.TraitsImplementing<Mobile>().FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
				if (mt != null)
				{
					nonAircraft = u;
					if (locomotorTypes.Contains(mt.Info.Locomotor))
						return u;
				}
			}

			if (nonAircraft.Actor != null)
				return nonAircraft;

			return squad.Units[0];
		}

		protected static bool CheckReachability(Actor sourceActor, Actor targetActor)
		{
			var mobile = sourceActor.TraitOrDefault<Mobile>();
			if (mobile == null)
				return false;
			else
			{
				var locomotor = mobile.Locomotor;
				return mobile.PathFinder.PathExistsForLocomotor(locomotor, sourceActor.Location, targetActor.Location);
			}
		}
	}
}
