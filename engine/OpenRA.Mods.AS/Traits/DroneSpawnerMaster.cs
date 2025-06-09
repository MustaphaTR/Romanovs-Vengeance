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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can spawn actors. Disable this trait to disable drone control, Pause this trait to stop drone spawning")]
	public class DroneSpawnerMasterInfo : BaseSpawnerMasterInfo
	{
		[Desc("Can the slaves be controlled independently?")]
		public readonly bool SlavesHaveFreeWill = false;

		[Desc("Place slave will gather to. Only recommended to used on building master")] // TODO: Test it on ground unit on map edges
		public readonly CVec[] GatherCell = Array.Empty<CVec>();

		[Desc("When idle and not moving, master check slaves and gathers them in this many tick. Set it properly can save performance")]
		public readonly int IdleCheckTick = 103;

		[Desc("After master attack, slaves will stop and follow master in this many tick. Mainly used for long-range attack also use drone")]
		public readonly int FollowAfterAttackDelay = 0;

		[Desc("Spawn initial load all at once?")]
		public readonly bool ShouldSpawnInitialLoad = true;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Actors == null || Actors.Length == 0)
				throw new YamlException($"Actors is null or empty for DroneSpawner for actor type {ai.Name}!");

			if (InitialActorCount > Actors.Length || InitialActorCount < -1)
				throw new YamlException("DroneSpawner can't have more InitialActorCount than the actors defined!");

			if (GatherCell.Length > Actors.Length)
				throw new YamlException($"Length of GatherOffsetCell can't be larger than the actors defined! (Actor type = {ai.Name})");
		}

		public override object Create(ActorInitializer init) { return new DroneSpawnerMaster(init, this); }
	}

	public class DroneSpawnerMaster : BaseSpawnerMaster, INotifyOwnerChanged, ITick,
		IResolveOrder, INotifyAttack
	{
		class DroneSpawnerSlaveEntry : BaseSpawnerSlaveEntry
		{
			public new DroneSpawnerSlave SpawnerSlave;
			public CVec GatherOffsetCell = CVec.Zero;
		}

		public new DroneSpawnerMasterInfo Info { get; }

		DroneSpawnerSlaveEntry[] slaveEntries;
		int spawnReplaceTicks;
		int followTick;

		ActivityType preState;

		WPos preLoc;

		int remainingIdleCheckTick;
		bool isAircraft;
		bool hasSpawnInitialLoad;

		public DroneSpawnerMaster(ActorInitializer init, DroneSpawnerMasterInfo info)
			: base(init, info)
		{
			Info = info;
			preLoc = WPos.Zero;
			followTick = 0;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			remainingIdleCheckTick = Info.IdleCheckTick;

			for (var i = 0; i < Info.GatherCell.Length; i++)
				slaveEntries[i].GatherOffsetCell = Info.GatherCell[i];

			isAircraft = self.Info.HasTraitInfo<AircraftInfo>();

			if (Info.ShouldSpawnInitialLoad)
				hasSpawnInitialLoad = false;
			else
				hasSpawnInitialLoad = true;
		}

		public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			slaveEntries = new DroneSpawnerSlaveEntry[info.Actors.Length]; // For this class to use

			for (var i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new DroneSpawnerSlaveEntry();

			return slaveEntries; // For the base class to use
		}

		public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			var se = entry as DroneSpawnerSlaveEntry;
			base.InitializeSlaveEntry(slave, se);

			se.SpawnerSlave = slave.Trait<DroneSpawnerSlave>();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (Info.SlavesHaveFreeWill)
				return;

			switch (order.OrderString)
			{
				case "Stop":
					StopSlaves();
					break;
				default:
					break;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			// Drone Master only pause attack when trait is Disabled
			// HACK: If Armament hits instantly and kills the target, the target will become invalid
			if (target.Type == TargetType.Invalid
				|| (Info.ArmamentNames.Count > 0 && !Info.ArmamentNames.Contains(a.Info.Name))
				|| Info.SlavesHaveFreeWill
				|| IsTraitDisabled)
				return;

			AssignTargetsToSlaves(self, target);
			followTick = Info.FollowAfterAttackDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			if (!hasSpawnInitialLoad)
			{
				// Spawn initial load.
				var burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
				for (var i = 0; i < burst; i++)
					Replenish(self, SlaveEntries);

				// The base class creates the slaves but doesn't move them into world.
				// Let's do it here.
				SpawnReplenishedSlaves(self);
				spawnReplaceTicks = -1;
				hasSpawnInitialLoad = true;
			}

			// Time to respawn something.
			if (!IsTraitPaused)
			{
				if (spawnReplaceTicks < 0)
				{
					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(slaveEntries) != null)
						spawnReplaceTicks = Info.RespawnTicks;
				}
				else if (spawnReplaceTicks == 0)
				{
					Replenish(self, slaveEntries);
					SpawnReplenishedSlaves(self);
					spawnReplaceTicks--;
				}
				else
					spawnReplaceTicks--;
			}

			if (!Info.SlavesHaveFreeWill)
				AssignSlaveActivity(self);

			if (followTick > 0)
				followTick--;
		}

		void SpawnReplenishedSlaves(Actor self)
		{
			foreach (var se in slaveEntries)
				if (se.IsValid && !se.Actor.IsInWorld)
					SpawnIntoWorld(self, se.Actor, self.CenterPosition + se.Offset.Rotate(self.Orientation));
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			if (spawnReplaceTicks <= 0)
				spawnReplaceTicks = Info.RespawnTicks;
		}

		void AssignTargetsToSlaves(Actor self, Target target)
		{
			foreach (var se in slaveEntries)
			{
				if (!se.IsValid)
					continue;
				if (se.SpawnerSlave.Info.AttackCallBackDistance.LengthSquared > (self.CenterPosition - target.CenterPosition).HorizontalLengthSquared)
					se.SpawnerSlave.Attack(se.Actor, target);
				else if (preLoc != self.CenterPosition)
				{
					MoveSlaves(self);
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
			}
		}

		void MoveSlaves(Actor self)
		{
			foreach (var se in slaveEntries)
			{
				if (!se.IsValid || !se.Actor.IsInWorld)
					continue;

				if (!se.SpawnerSlave.IsMoving(self.Location + se.GatherOffsetCell))
				{
					se.SpawnerSlave.Stop(se.Actor);
					se.SpawnerSlave.Move(se.Actor, self.Location + se.GatherOffsetCell);
				}
			}
		}

		void AssignSlaveActivity(Actor self)
		{
			if (followTick > 0)
				return;

			var effectiveActivity = self.CurrentActivity;
			if (!self.IsIdle)
			{
				while (effectiveActivity.ChildActivity != null)
					effectiveActivity = effectiveActivity.ChildActivity;
			}

			// 1. Drone may get away from master due to auto-targeting.
			if (effectiveActivity == null || effectiveActivity.ActivityType == ActivityType.Ability || effectiveActivity.ActivityType == ActivityType.Undefined)
			{
				if (remainingIdleCheckTick < 0)
				{
					MoveSlaves(self);
					remainingIdleCheckTick = Info.IdleCheckTick;
				}

				// 1.1 There is situation like teleport will just change actor's position without activity
				else if (preLoc != self.CenterPosition)
				{
					MoveSlaves(self);
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
				else
					remainingIdleCheckTick--;
			}

			// 2. Stop the drone attacking when move for special case of fire at an ally.
			// Only move slaves when position change
			// Note: because aircraft always Fly, so drone may get away from master due to auto-targeting
			// when actor moves.
			else if (effectiveActivity.ActivityType == ActivityType.Move)
			{
				if (preState == ActivityType.Attack)
				{
					StopSlaves();
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
				else if (preLoc != self.CenterPosition)
				{
					MoveSlaves(self);
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
				else if (remainingIdleCheckTick < 0 && isAircraft)
				{
					MoveSlaves(self);
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
				else if (isAircraft)
					remainingIdleCheckTick--;
			}

			// Actually, new code here or old code in MobSpawnerMaster is not working
			// The only working code is in INotifyAttack. It is due to Activity of attack
			// do not achieve `GetTargets(actor)`
			// 3. Stop the slaves move when prepare to attack
			else if (effectiveActivity.ActivityType == ActivityType.Attack)
			{
				if (preState == ActivityType.Move)
				{
					StopSlaves();
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
				else if (preState == ActivityType.Undefined || preState == ActivityType.Ability)
				{
					StopSlaves();
					remainingIdleCheckTick = Info.IdleCheckTick;
				}
			}

			preState = effectiveActivity == null ? ActivityType.Undefined : effectiveActivity.ActivityType;
			preLoc = self.CenterPosition;
		}

		/* Debug
		, ITickRender
		void ITickRender.TickRender(Graphics.WorldRenderer wr, Actor self)
		{
			var font = Game.Renderer.Fonts["Bold"];
			foreach (var kv in Info.GatherOffsetCell)
			{
				var i = new FloatingText(self.World.Map.CenterOfCell(kv + self.Location), Color.Gold, "1", 1);
				self.World.Add(i);
			}
		}
		*/
	}
}
