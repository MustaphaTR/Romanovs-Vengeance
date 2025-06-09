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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	abstract class AirStateBase : StateBase
	{
		protected static int CountAntiAirUnits(Squad owner, IReadOnlyCollection<Actor> units)
		{
			if (units.Count == 0)
				return 0;

			var missileUnitsCount = 0;
			foreach (var unit in units)
			{
				if (unit == null)
					continue;

				foreach (var ab in unit.TraitsImplementing<AttackBase>())
				{
					if (ab.IsTraitDisabled || ab.IsTraitPaused)
						continue;

					foreach (var a in ab.Armaments)
					{
						if (a.Weapon.IsValidTarget(owner.SquadManager.Info.AircraftTargetType))
						{
							if (unit.Info.HasTraitInfo<AircraftInfo>())
								missileUnitsCount++;
							else
								missileUnitsCount += 3;
							break;
						}
					}
				}
			}

			return missileUnitsCount;
		}

		protected static bool NearToPosSafely(Squad owner, WPos loc)
		{
			return NearToPosSafely(owner, loc, out _);
		}

		protected static bool NearToPosSafely(Squad owner, WPos loc, out Actor detectedEnemyTarget)
		{
			detectedEnemyTarget = null;

			var dangerRadius = owner.SquadManager.Info.DangerScanRadius;
			var unitsAroundPos = owner.World.FindActorsInCircle(loc, WDist.FromCells(dangerRadius))
				.Where(owner.SquadManager.IsPreferredEnemyUnit).ToList();

			if (unitsAroundPos.Count == 0)
				return true;

			if (CountAntiAirUnits(owner, unitsAroundPos) < owner.Units.Count)
			{
				detectedEnemyTarget = unitsAroundPos.Random(owner.Random);
				return true;
			}

			return false;
		}

		// Checks the number of anti air enemies around units
		protected virtual bool ShouldFlee(Squad owner, Actor leader)
		{
			return ShouldFlee(owner, enemies => CountAntiAirUnits(owner, enemies) > owner.Units.Count);
		}
	}

	sealed class AirIdleState : AirStateBase, IState
	{
		const int MaxCheckTimesPerTick = 2;
		Map map;
		int dangerRadius;
		int columnCount;
		int rowCount;

		int[] airStrikeCheckIndices = null;
		int checkedIndex = 0;

		public void Activate(Squad owner)
		{
			dangerRadius = owner.SquadManager.Info.DangerScanRadius;
			map = owner.World.Map;
			columnCount = (map.MapSize.Width + dangerRadius - 1) / dangerRadius;
			rowCount = (map.MapSize.Height + dangerRadius - 1) / dangerRadius;
			airStrikeCheckIndices ??= Exts.MakeArray(columnCount * rowCount, i => i).Shuffle(owner.World.LocalRandom).ToArray();
		}

		Actor FindDefenselessTarget(Squad owner)
		{
			for (var checktime = 0; checktime <= MaxCheckTimesPerTick; checkedIndex++, checktime++)
			{
				if (checkedIndex >= airStrikeCheckIndices.Length)
					checkedIndex = 0;

				var pos = new MPos(airStrikeCheckIndices[checkedIndex] % columnCount * dangerRadius + dangerRadius / 2,
					airStrikeCheckIndices[checkedIndex] / columnCount * dangerRadius + dangerRadius / 2).ToCPos(map);

				if (NearToPosSafely(owner, map.CenterOfCell(pos), out var detectedEnemyTarget))
				{
					if (detectedEnemyTarget == null)
						continue;

					checkedIndex = owner.World.LocalRandom.Next(airStrikeCheckIndices.Length);
					return detectedEnemyTarget;
				}
			}

			return null;
		}

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner, owner.Units[0].Actor))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState());
				return;
			}

			if (!owner.IsTargetValid)
			{
				var e = owner.SquadManager.PopAirStrikeTarget();
				e ??= FindDefenselessTarget(owner);

				owner.TargetActor = e;
			}

			if (!owner.IsTargetValid)
			{
				Retreat(owner, flee: false, rearm: true, repair: true);
				return;
			}

			owner.FuzzyStateMachine.ChangeState(owner, new AirAttackState());
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class AirAttackState : AirStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var u = owner.Units.Random(owner.Random);
				var closestEnemy = owner.SquadManager.FindClosestEnemy(u.Actor);
				if (closestEnemy != null)
					owner.TargetActor = closestEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState());

					return;
				}
			}

			var leader = owner.Units.Select(u => u.Actor).ClosestToIgnoringPath(owner.TargetActor.CenterPosition);

			var unitsAroundPos = owner.World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(owner.SquadManager.Info.DangerScanRadius))
				.Where(a => owner.SquadManager.IsPreferredEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a)).ToList();

			// Check if get ambushed.
			if (CountAntiAirUnits(owner, unitsAroundPos) > owner.Units.Count)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState());

				return;
			}

			var cannotRetaliate = true;
			var resupplyingUnits = new List<Actor>();
			var backingoffUnits = new List<Actor>();
			var attackingUnits = new List<Actor>();
			foreach (var u in owner.Units)
			{
				if (IsAttackingAndTryAttack(u.Actor).TryAttacking)
				{
					cannotRetaliate = false;
					continue;
				}

				var ammoPools = u.Actor.TraitsImplementing<AmmoPool>().ToArray();
				if (!ReloadsAutomatically(ammoPools, u.Actor.TraitOrDefault<Rearmable>()))
				{
					if (IsRearming(u.Actor))
						continue;

					if (!HasAmmo(ammoPools))
					{
						resupplyingUnits.Add(u.Actor);
						continue;
					}
				}

				if (CanAttackTarget(u.Actor, owner.TargetActor))
				{
					cannotRetaliate = false;
					attackingUnits.Add(u.Actor);
				}
				else
				{
					if (!FullAmmo(ammoPools))
					{
						resupplyingUnits.Add(u.Actor);
						continue;
					}

					backingoffUnits.Add(u.Actor);
				}
			}

			if (cannotRetaliate)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeState());
				return;
			}

			owner.Bot.QueueOrder(new Order("ReturnToBase", null, false, groupedActors: resupplyingUnits.ToArray()));
			owner.Bot.QueueOrder(new Order("Attack", null, Target.FromActor(owner.TargetActor), false, groupedActors: attackingUnits.ToArray()));
			owner.Bot.QueueOrder(new Order("Move", null, Target.FromCell(owner.World, RandomBuildingLocation(owner)), false, groupedActors: backingoffUnits.ToArray()));
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class AirFleeState : AirStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			owner.TargetActor = null;

			if (!owner.IsValid)
				return;

			Retreat(owner, flee: true, rearm: true, repair: true);

			owner.FuzzyStateMachine.ChangeState(owner, new AirIdleState());
		}

		public void Deactivate(Squad owner) { }
	}
}
