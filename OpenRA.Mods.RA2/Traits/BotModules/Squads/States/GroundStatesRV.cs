#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RV.Traits.BotModules.Squads
{
	abstract class GroundStateBaseRV : StateBaseRV
	{
		protected virtual bool ShouldFlee(SquadRV owner)
		{
			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzyRV.Default.CanAttack(owner.Units, enemies));
		}

		protected Actor FindClosestEnemy(SquadRV owner)
		{
			return owner.SquadManager.FindClosestEnemy(owner.Units.First().CenterPosition);
		}

		protected Actor GetRandomValuableTarget(SquadRV owner)
		{
			var manager = owner.SquadManager;
			var mustDestroyedEnemy = manager.World.ActorsHavingTrait<MustBeDestroyed>(t => t.Info.RequiredForShortGame)
					.Where(a => manager.IsPreferredEnemyUnit(a) && manager.IsNotHiddenUnit(a)).ToArray();

			if (!mustDestroyedEnemy.Any())
				return FindClosestEnemy(owner);

			return mustDestroyedEnemy.Random(owner.World.LocalRandom);
		}

		protected Actor ThreatScan(SquadRV owner, Actor teamLeader, WDist scanRadius)
		{
			var enemies = owner.World.FindActorsInCircle(teamLeader.CenterPosition, scanRadius)
					.Where(a => owner.SquadManager.IsPreferredEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a));
			return enemies.ClosestTo(teamLeader.CenterPosition);
		}
	}

	class GroundUnitsIdleStateRV : GroundStateBaseRV, IState
	{
		public void Activate(SquadRV owner) { }

		public void Tick(SquadRV owner)
		{
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = FindClosestEnemy(owner);
				if (closestEnemy == null)
					return;

				owner.TargetActor = closestEnemy;
			}

			var enemyUnits = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius))
				.Where(a => owner.SquadManager.IsPreferredEnemyUnit(a) && owner.SquadManager.IsNotHiddenUnit(a)).ToList();

			if (enemyUnits.Count == 0)
			{
				Retreat(owner, false, true, true);
				return;
			}

			if (AttackOrFleeFuzzyRV.Default.CanAttack(owner.Units, enemyUnits))
			{
				// We have gathered sufficient units. Attack the nearest enemy unit.
				// Inform human allies about AI's rush attack.
				owner.Bot.QueueOrder(new Order("PlaceBeacon", owner.SquadManager.Player.PlayerActor, Target.FromCell(owner.World, owner.TargetActor.Location), false)
				{ SuppressVisualFeedback = true });
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateRV(), true);
			}
			else
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateRV(), true);
		}

		public void Deactivate(SquadRV owner) { }
	}

	class GroundUnitsAttackMoveStateRV : GroundStateBaseRV, IState
	{
		public const int StuckInPathCheckTimes = 6;
		public const int MakeWayTick = 2;

		internal Actor PathGuider = null;

		// Give tolerance for AI grouping team at start
		internal int StuckInPath = StuckInPathCheckTimes;
		internal int TryMakeWay = MakeWayTick;
		internal WPos LastPos = new WPos(0, 0, 0);
		internal int LastPosIndex = 0;

		public void Activate(SquadRV owner) { }

		public void Tick(SquadRV owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			if (!owner.IsTargetValid)
			{
				var randomSuitableEnemy = GetRandomValuableTarget(owner);
				if (randomSuitableEnemy != null)
					owner.TargetActor = randomSuitableEnemy;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateRV(), true);
					return;
				}
			}

			// Initialize PathGuider. Optimaze pathfinding by using PathGuider.
			PathGuider = owner.Units.FirstOrDefault();
			if (PathGuider == null)
				return;

			// 1. Threat scan surroundings
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);

			var targetActor = ThreatScan(owner, PathGuider, attackScanRadius);
			if (targetActor != null)
			{
				owner.TargetActor = targetActor;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackStateRV(), true);
				return;
			}

			// 2. Force scattered for navigator if needed
			if (StuckInPath <= 0)
			{
				if (TryMakeWay > 0)
				{
					owner.Bot.QueueOrder(new Order("AttackMove", PathGuider, Target.FromCell(owner.World, owner.TargetActor.Location), false));

					foreach (var a in owner.Units)
					{
						if (a != PathGuider)
							owner.Bot.QueueOrder(new Order("Scatter", a, false));
					}

					TryMakeWay--;
				}
				else
				{
					// When going through is over, restore the check
					StuckInPath = StuckInPathCheckTimes + MakeWayTick;
					TryMakeWay = MakeWayTick;
				}

				return;
			}

			// 3. Check if the squad is stucked due to the map has very twisted path
			// or currently bridge and tunnel from TS mod
			if ((PathGuider.CenterPosition - LastPos).LengthSquared <= 4)
				StuckInPath--;
			else
				StuckInPath = StuckInPathCheckTimes;

			LastPos = PathGuider.CenterPosition;

			// 4. Since units have different movement speeds, they get separated while approaching the target.

			/* Let them regroup into tighter formation towards "PathGuider".
			 *
			 * "unitsArea" means the space the squad units will occupy (if 1 per Cell).
			 * PathGuider only stop when scope of "unitsAround" is not covered all units;
			 * units in "unitsHurryUp"  will catch up,
			 * which keep the team tight while not stucked.
			 *
			 * Imagining "unitsArea" takes up a a place shape like square, we need to draw a circle
			 * to cover the the enitire circle.
			 *
			 * When look around, PathGuider find units around to decide if it need to continue.
			 * and units that need hurry up will try catch up before guider waiting
			 *
			 * However in practice because of the poor PF, squad tend to PF to a eclipse.
			 * "lookAround" now has the radius of two times of the circle mentioned before.
			 */

			var groupArea = (long)WDist.FromCells(owner.Units.Count).Length * 1024;

			var unitsHurryUp = owner.Units.Where(a => (a.CenterPosition - PathGuider.CenterPosition).LengthSquared >= groupArea * 2).ToArray();
			var lookAround = owner.Units.Where(a => (a.CenterPosition - PathGuider.CenterPosition).LengthSquared <= groupArea * 5).ToArray();

			if (owner.Units.Count > lookAround.Length)
				owner.Bot.QueueOrder(new Order("Stop", PathGuider, false));
			else
				owner.Bot.QueueOrder(new Order("AttackMove", PathGuider, Target.FromCell(owner.World, owner.TargetActor.Location), false));

			foreach (var unit in unitsHurryUp)
				owner.Bot.QueueOrder(new Order("AttackMove", unit, Target.FromCell(owner.World, PathGuider.Location), false));
		}

		public void Deactivate(SquadRV owner) { }
	}

	class GroundUnitsAttackStateRV : GroundStateBaseRV, IState
	{
		internal Actor TeamLeader = null;

		public void Activate(SquadRV owner) { }

		public void Tick(SquadRV owner)
		{
			var cannotRetaliate = false;

			// Basic check
			if (!owner.IsValid)
				return;

			TeamLeader = owner.Units.FirstOrDefault();
			if (TeamLeader == null)
				return;

			// Rescan target to prevent being ambushed and die without fight
			// If there is no threat around, return to AttackMove state for formation
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);
			var targetActor = ThreatScan(owner, TeamLeader, attackScanRadius);

			if (targetActor == null)
			{
				owner.TargetActor = GetRandomValuableTarget(owner);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateRV(), true);
				return;
			}
			else
			{
				cannotRetaliate = true;
				owner.TargetActor = targetActor;

				foreach (var a in owner.Units)
				{
					if (!BusyAttack(a))
					{
						if (CanAttackTarget(a, targetActor))
						{
							owner.Bot.QueueOrder(new Order("Attack", a, Target.FromActor(owner.TargetActor), false));
							cannotRetaliate = false;
						}
						else
							owner.Bot.QueueOrder(new Order("AttackMove", a, Target.FromCell(owner.World, TeamLeader.Location), false));
					}
					else
						cannotRetaliate = false;
				}
			}

			if (ShouldFlee(owner) || cannotRetaliate)
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateRV(), true);
		}

		public void Deactivate(SquadRV owner) { }
	}

	class GroundUnitsFleeStateRV : GroundStateBaseRV, IState
	{
		public void Activate(SquadRV owner) { }

		public void Tick(SquadRV owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, true, true, true);

			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateRV(), true);
		}

		public void Deactivate(SquadRV owner) { owner.Units.Clear(); }
	}
}
