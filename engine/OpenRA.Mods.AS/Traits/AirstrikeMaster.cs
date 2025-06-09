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
	[Desc("This actor can send in other actors to deliver an airstrike.")]
	public class AirstrikeMasterInfo : BaseSpawnerMasterInfo
	{
		public readonly string Name = "primary";

		[Desc("Just send the spawnee and forget it.")]
		public readonly bool SendAndForget = false;

		[Desc("Spawn rearm delay, in ticks")]
		public readonly int RearmTicks = 150;

		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[Desc("Instantly repair spawners when they return?")]
		public readonly bool InstantRepair = true;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new();

		[Desc("The sound will be played when mark a target")]
		public readonly string MarkSound = "";

		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new(5120);

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new AirstrikeMaster(init, this); }
	}

	public class AirstrikeMaster : BaseSpawnerMaster, ITick, INotifyAttack
	{
		class AirstrikeSlaveEntry : BaseSpawnerSlaveEntry
		{
			public int RearmTicks = 0;
			public new AirstrikeSlave SpawnerSlave;
		}

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new();
		public readonly AirstrikeMasterInfo AirstrikeMasterInfo;

		readonly Stack<int> loadedTokens = new();

		WPos finishEdge;
		WVec spawnOffset;
		WPos targetPos;

		int launchCondition = Actor.InvalidConditionToken;
		int launchConditionTicks;

		int respawnTicks = 0;

		public AirstrikeMaster(ActorInitializer init, AirstrikeMasterInfo info)
			: base(init, info)
		{
			AirstrikeMasterInfo = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			// Spawn initial load.
			var burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
			for (var i = 0; i < burst; i++)
				Replenish(self, SlaveEntries);
		}

		public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			var slaveEntries = new AirstrikeSlaveEntry[info.Actors.Length]; // For this class to use

			for (var i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new AirstrikeSlaveEntry();

			return slaveEntries; // For the base class to use
		}

		public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			var se = entry as AirstrikeSlaveEntry;
			base.InitializeSlaveEntry(slave, se);

			se.RearmTicks = 0;
			se.IsLaunched = false;
			se.SpawnerSlave = slave.Trait<AirstrikeSlave>();
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			// HACK: If Armament hits instantly and kills the target, the target will become invalid
			if (target.Type == TargetType.Invalid || IsTraitDisabled || IsTraitPaused || (Info.ArmamentNames.Count > 0 && !Info.ArmamentNames.Contains(a.Info.Name)))
				return;

			// Issue retarget order for already launched ones
			foreach (var slave in SlaveEntries)
				if (slave.IsLaunched && slave.IsValid)
					slave.SpawnerSlave.Attack(slave.Actor, target);

			var se = GetLaunchable();
			if (se == null)
				return;

			se.IsLaunched = true; // mark as launched

			if (AirstrikeMasterInfo.LaunchingCondition != null)
			{
				if (launchCondition == Actor.InvalidConditionToken)
					launchCondition = self.GrantCondition(AirstrikeMasterInfo.LaunchingCondition);

				launchConditionTicks = AirstrikeMasterInfo.LaunchingTicks;
			}

			SpawnIntoWorld(self, se.Actor, self.CenterPosition + se.Offset.Rotate(self.Orientation));

			se.SpawnerSlave.SetSpawnInfo(finishEdge, spawnOffset, targetPos);

			// Lambdas can't use 'in' variables, so capture a copy for later
			var delayedTarget = target;

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// The actor might had been trying to do something before entering the carrier.
				// Cancel whatever it was trying to do.
				se.SpawnerSlave.Stop(se.Actor);
				if (!string.IsNullOrEmpty(AirstrikeMasterInfo.MarkSound))
					se.Actor.PlayVoice(AirstrikeMasterInfo.MarkSound);
				se.SpawnerSlave.Attack(se.Actor, delayedTarget);
			});

			if (AirstrikeMasterInfo.SendAndForget)
				se.Actor = null;
		}

		public override void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			var w = self.World;
			var target = centerPosition;
			for (var i = -AirstrikeMasterInfo.SquadSize / 2; i <= AirstrikeMasterInfo.SquadSize / 2; i++)
			{
				var attackFacing = 256 * self.World.SharedRandom.Next(AirstrikeMasterInfo.QuantizedFacings) / AirstrikeMasterInfo.QuantizedFacings;

				var altitude = self.World.Map.Rules.Actors[slave.Info.Name].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
				var attackRotation = WRot.FromFacing(attackFacing);
				var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
				target += new WVec(0, 0, altitude);
				var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + AirstrikeMasterInfo.Cordon).Length * delta / 1024;
				var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + AirstrikeMasterInfo.Cordon).Length * delta / 1024;

				var so = AirstrikeMasterInfo.SquadOffset;
				var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
				var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(attackRotation);

				this.spawnOffset = spawnOffset;
				this.finishEdge = finishEdge;
				targetPos = target;

				w.AddFrameEndTask(_ =>
				{
					if (!slave.IsInWorld)
						w.Add(slave);

					var attack = slave.Trait<AttackAircraft>();
					attack.AttackTarget(Target.FromPos(target + targetOffset), AttackSource.Default, false, true);
				});
			}
		}

		void Recall()
		{
			// Tell launched slaves to come back and enter me.
			foreach (var se in SlaveEntries)
			{
				var childSlave = se as AirstrikeSlaveEntry;
				if (se.IsLaunched && se.IsValid)
					childSlave.SpawnerSlave.LeaveMap(se.Actor);
			}
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier(AirstrikeMasterInfo.Name)));
		}

		AirstrikeSlaveEntry GetLaunchable()
		{
			foreach (var se in SlaveEntries)
			{
				var childSlave = se as AirstrikeSlaveEntry;
				if (childSlave.RearmTicks <= 0 && !childSlave.IsLaunched && se.IsValid)
					return childSlave;
			}

			return null;
		}

		public void PickupSlave(Actor self, Actor a)
		{
			AirstrikeSlaveEntry slaveEntry = null;
			foreach (var se in SlaveEntries)
				if (se.Actor == a)
				{
					slaveEntry = se as AirstrikeSlaveEntry;
					break;
				}

			if (slaveEntry == null)
				throw new InvalidOperationException("An actor that isn't my slave entered me?");

			slaveEntry.IsLaunched = false;

			// setup rearm
			slaveEntry.RearmTicks = Util.ApplyPercentageModifiers(
				AirstrikeMasterInfo.RearmTicks, reloadModifiers.Select(rm => rm.GetReloadModifier(AirstrikeMasterInfo.Name)));

			if (AirstrikeMasterInfo.SpawnContainConditions.TryGetValue(a.Info.Name, out var spawnContainCondition))
				spawnContainTokens.GetOrAdd(a.Info.Name).Push(self.GrantCondition(spawnContainCondition));

			if (!string.IsNullOrEmpty(AirstrikeMasterInfo.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(AirstrikeMasterInfo.LoadedCondition));
		}

		void ITick.Tick(Actor self)
		{
			if (launchCondition != Actor.InvalidConditionToken && --launchConditionTicks < 0)
				launchCondition = self.RevokeCondition(launchCondition);

			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, SlaveEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(SlaveEntries) != null)
						respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier(AirstrikeMasterInfo.Name)));
				}
			}

			// Rearm
			foreach (var se in SlaveEntries)
			{
				var slaveEntry = se as AirstrikeSlaveEntry;
				if (slaveEntry.RearmTicks > 0)
					slaveEntry.RearmTicks--;
			}
		}

		protected override void TraitPaused(Actor self)
		{
			Recall();
		}
	}
}
