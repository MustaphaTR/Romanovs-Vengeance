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
	abstract class GroundStateBase : StateBase { }

	sealed class GroundUnitsIdleState : GroundStateBase, IState
	{
		Actor leader;

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			if (owner.SquadManager.UnitCannotBeOrdered(leader))
				leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor).Actor;

			if (!owner.IsTargetValid)
			{
				var closestEnemy = owner.SquadManager.FindClosestEnemy(leader);
				if (closestEnemy == null)
					return;

				owner.TargetActor = closestEnemy;
			}

			var enemyUnits = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius))
				.Where(owner.SquadManager.IsPreferredEnemyUnit).ToList();

			if (enemyUnits.Count == 0)
			{
				Retreat(owner, flee: false, rearm: true, repair: true);
				return;
			}

			if (AttackOrFleeFuzzy.Default.CanAttack(owner.Units.ConvertAll(u => u.Actor), enemyUnits))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState());
			else
				Retreat(owner, flee: true, rearm: true, repair: true);
		}

		public void Deactivate(Squad owner) { }
	}

	// This version AI forcus on solving pathfinding problem for AI
	// 1. use a leader to guide the entire squad to target, solve stuck on twisted road and saving performance on pathfinding
	// 2. have two methods to solve entire squad stuck. First, try make way for leader. Second, kick stuck units
	sealed class GroundUnitsAttackMoveState : GroundStateBase, IState
	{
		const int MaxMakeWayPossibility = 4;
		const int MaxSquadStuckPossibility = 6;
		const int MakeWayTicks = 3;
		const int KickStuckTicks = 4;

		// Give tolerance for AI grouping team at start
		int shouldMakeWayPossibility = -(MaxMakeWayPossibility * 6);
		int shouldKickStuckPossibility = -(MaxSquadStuckPossibility * 6);
		int makeWay = 0;
		int kickStuck = 0;

		UnitWposWrapper leader = new(null);

		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			// Initialize leader. Optimize pathfinding by using a leader with specific locomotor.
			if (owner.SquadManager.UnitCannotBeOrdered(leader.Actor))
				leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor);

			if (!owner.IsTargetValid || !CheckReachability(leader.Actor, owner.TargetActor))
			{
				var targetActor = owner.SquadManager.FindClosestEnemy(leader.Actor);
				if (targetActor != null)
					owner.TargetActor = targetActor;
				else
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState());
					return;
				}
			}

			// Switch to "GroundUnitsAttackState" if we encounter enemy units.
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);

			var enemyActor = owner.SquadManager.FindClosestEnemy(leader.Actor, attackScanRadius);
			if (enemyActor != null)
			{
				owner.TargetActor = enemyActor;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState());

				return;
			}

			// Since units have different movement speeds, they get separated while approaching the target.
			// Let them regroup into tighter formation towards "leader".
			//
			// "occupiedArea" means the space the squad units will occupy (if 1 per Cell).
			// leader only stop when scope of "lookAround" is not covered all units;
			// units in "unitsHurryUp"  will catch up, which keep the team tight while not stuck.
			//
			// Imagining "occupiedArea" takes up a a place shape like square,
			// we need to draw a circle to cover the the enitire circle.
			var occupiedArea = (long)WDist.FromCells(owner.Units.Count).Length * 1024;

			// Kick stuck units: Kick stuck units that is blocked
			if (kickStuck > 0)
			{
				var stopUnits = new List<Actor>();
				var otherUnits = new List<Actor>();

				// Check if it is the leader stuck
				if (leader.Actor.CenterPosition == leader.WPos && !IsAttackingAndTryAttack(leader.Actor).IsFiring)
				{
					stopUnits.Add(leader.Actor);
					owner.Units.Remove(leader);
					AIUtils.BotDebug("AI ({0}): Kick leader from squad.", owner.Bot.Player.ClientIndex);
				}

				// Check if it is the units stuck
				else
				{
					for (var i = 0; i < owner.Units.Count; i++)
					{
						var u = owner.Units[i];

						if (u.Actor == leader.Actor)
							continue;

						// If unit that is not in valid distance from leader nor firing at enemy,
						// we will check if it can reach the leader, or stuck due to unknow reason
						if ((u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared >= 5 * occupiedArea
							&& (u.Actor.CenterPosition == u.WPos
							|| !AIUtils.PathExist(u.Actor, leader.Actor.Location, leader.Actor)))
						{
							stopUnits.Add(u.Actor);
							owner.Units.RemoveAt(i);
							i--;
						}
						else
						{
							u.WPos = u.Actor.CenterPosition;
							otherUnits.Add(u.Actor);
						}
					}

					if (stopUnits.Count > 0)
						AIUtils.BotDebug("AI ({0}): Kick ({1}) from squad.", owner.Bot.Player.ClientIndex, stopUnits.Count);
				}

				if (owner.Units.Count == 0)
					return;

				if (kickStuck > 1)
				{
					leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor);
					leader.WPos = leader.Actor.CenterPosition;
					owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, Target.FromCell(owner.World, owner.TargetActor.Location), false));
					owner.Bot.QueueOrder(new Order("Stop", null, false, groupedActors: stopUnits.ToArray()));
					owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Actor.Location), false, groupedActors: otherUnits.ToArray()));
					kickStuck--;
				}
				else if (kickStuck == 1)
				{
					shouldMakeWayPossibility = 0;
					shouldKickStuckPossibility = 0;
					leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor);

					// The end of "kickStuck": stop the leader for position record next tick
					owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
					kickStuck = 0;
				}

				return;
			}

			// Make way for leader: Make sure the guide unit has not been blocked by the rest of the squad.
			if (makeWay > 0)
			{
				if (makeWay > 1)
				{
					var others = owner.Units.Where(u => u.Actor != leader.Actor).Select(u => u.Actor);
					owner.Bot.QueueOrder(new Order("Scatter", null, false, groupedActors: others.ToArray()));
					owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, Target.FromCell(owner.World, owner.TargetActor.Location), false));
					makeWay--;
				}
				else if (makeWay == 1)
				{
					shouldMakeWayPossibility = 0;
					shouldKickStuckPossibility = MaxSquadStuckPossibility / 2;

					// The end of "makeWay": stop the leader for position record next tick
					// set "makeWay" to -1 to inform that squad just make way for leader
					owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
					makeWay = -1;
				}

				return;
			}

			// "leaderStopCheck" to see if leader move.
			// "leaderWaitCheck" to see if leader should wait squad members that left behind.
			var leaderStopCheck = leader.Actor.CenterPosition == leader.WPos;
			var leaderWaitCheck = owner.Units.Any(u => (u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared > occupiedArea * 5);

			// To find out the stuck problem of the squad and deal with it.
			// 1. If leader cannot move and leader should wait, there may be squad members stuck.
			// 2. If leader cannot move but leader should go, leader is stuck.
			// -- Try make way for leader
			// -- If make way cannot solve this problem, we kick stuck unit
			// 3. If leader can move and leader should go, we consider this squad has no problem on stuck.
			if (leaderStopCheck && leaderWaitCheck)
				shouldKickStuckPossibility++;
			else if (leaderStopCheck && !leaderWaitCheck)
			{
				if (makeWay != -1)
					shouldMakeWayPossibility++;
				else
					shouldKickStuckPossibility++;
			}
			else if (!leaderStopCheck && !leaderWaitCheck)
			{
				shouldMakeWayPossibility = 0;
				shouldKickStuckPossibility = 0;
			}

			// Check if we need to make way for leader or kick stuck units
			if (shouldMakeWayPossibility >= MaxMakeWayPossibility)
			{
				AIUtils.BotDebug("AI ({0}): Make way for squad leader.", owner.Bot.Player.ClientIndex);
				makeWay = MakeWayTicks;
			}
			else if (shouldKickStuckPossibility >= MaxSquadStuckPossibility)
			{
				AIUtils.BotDebug("AI ({0}): Kick stuck units from squad.", owner.Bot.Player.ClientIndex);
				kickStuck = KickStuckTicks;
			}

			// Record current position of the squad leader
			leader.WPos = leader.Actor.CenterPosition;

			// Leader will wait squad members that left behind, unless
			// next tick is kick stuck unit (we need leader move in advance).
			if (leaderWaitCheck && kickStuck <= 0)
				owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
			else
				owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, Target.FromCell(owner.World, owner.TargetActor.Location), false));

			var unitsHurryUp = owner.Units.Where(u => (u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared >= occupiedArea * 2)
				.Select(u => u.Actor).ToArray();
			owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Actor.Location), false, groupedActors: unitsHurryUp));
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class GroundUnitsAttackState : GroundStateBase, IState
	{
		// Use it to find if entire squad cannot reach the attack position
		int tryAttackTick;

		Actor leader;
		int tryAttack = 0;

		public void Activate(Squad owner)
		{
			tryAttackTick = owner.SquadManager.Info.AttackScanRadius;
		}

		public void Tick(Squad owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			if (owner.SquadManager.UnitCannotBeOrdered(leader))
				leader = owner.Units[0].Actor;

			owner.SquadManager.SetAirStrikeTarget(owner.TargetActor);
			var isDefaultLeader = true;

			// Rescan target to prevent being ambushed and die without fight
			// If there is no threat around, return to AttackMove state for formation
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);
			var closestEnemy = owner.SquadManager.FindClosestEnemy(leader, attackScanRadius);

			// Becuase MoveWithinRange can cause huge lag when stuck
			// we only allow free attack behaivour within TryAttackTick
			// then the squad will gather to a certain leader
			if (closestEnemy == null)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(leader);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveState());
				return;
			}
			else if (owner.TargetActor != closestEnemy)
			{
				// Refresh tryAttack when target switched
				tryAttack = 0;
				owner.TargetActor = closestEnemy;
			}

			var cannotRetaliate = true;
			var followingUnits = new List<Actor>();
			var attackingUnits = new List<Actor>();

			foreach (var u in owner.Units)
			{
				var (isFiring, tryAttacking) = IsAttackingAndTryAttack(u.Actor);

				if ((tryAttacking || isFiring) &&
					(u.Actor.CenterPosition - owner.TargetActor.CenterPosition).HorizontalLengthSquared <
					(leader.CenterPosition - owner.TargetActor.CenterPosition).HorizontalLengthSquared)
				{
					isDefaultLeader = false;
					leader = u.Actor;
				}

				if (isFiring && tryAttack != 0)
				{
					// Make there is at least one follow and attack target, AFTER first trying on attack
					if (isDefaultLeader)
					{
						leader = u.Actor;
						isDefaultLeader = false;
					}

					cannotRetaliate = false;
				}
				else if (CanAttackTarget(u.Actor, owner.TargetActor))
				{
					if (tryAttack > tryAttackTick && tryAttacking)
					{
						// Make there is at least one follow and attack target even when approach max tryAttackTick
						if (isDefaultLeader)
						{
							leader = u.Actor;
							isDefaultLeader = false;
							attackingUnits.Add(u.Actor);
							continue;
						}

						followingUnits.Add(u.Actor);
						continue;
					}

					attackingUnits.Add(u.Actor);
					cannotRetaliate = false;
				}
				else
					followingUnits.Add(u.Actor);
			}

			// Because ShouldFlee(owner) cannot retreat units while they cannot even fight
			// a unit that they cannot target. Therefore, use `cannotRetaliate` here to solve this bug.
			if (ShouldFleeSimple(owner) || cannotRetaliate)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeState());

				return;
			}

			tryAttack++;

			owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Location), false, groupedActors: followingUnits.ToArray()));
			owner.Bot.QueueOrder(new Order("Attack", null, Target.FromActor(owner.TargetActor), false, groupedActors: attackingUnits.ToArray()));
		}

		public void Deactivate(Squad owner) { }
	}

	sealed class GroundUnitsFleeState : GroundStateBase, IState
	{
		public void Activate(Squad owner) { }

		public void Tick(Squad owner)
		{
			if (!owner.IsValid)
				return;

			Retreat(owner, flee: true, rearm: true, repair: true);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleState());
		}

		public void Deactivate(Squad owner) { owner.SquadManager.DismissSquad(owner); }
	}
}
