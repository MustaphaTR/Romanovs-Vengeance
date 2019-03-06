#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can spawn actors.")]
	public class AirstrikeMasterInfo : BaseSpawnerMasterInfo
	{
		[Desc("Spawn is a missile that dies and not return.")]
		public readonly bool SpawnIsMissile = false;

		[Desc("Spawn rearm delay, in ticks")]
		public readonly int RearmTicks = 150;

		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[Desc("Pip color for the spawn count.")]
		public readonly PipType PipType = PipType.Yellow;

		[Desc("Insta-repair spawners when they return?")]
		public readonly bool InstaRepair = true;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();
		[Desc("The sound will be played when mark a target")]
		public readonly string MarkSound = "Attack";
		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		public override object Create(ActorInitializer init) { return new AirstrikeMaster(init, this); }
	}

	public class AirstrikeMaster : BaseSpawnerMaster, IPips, ITick, INotifyAttack, INotifyBecomingIdle
	{
		private WPos startEdge;
		private WPos finishEdge;
		private WVec spawnOffset;
		private int attackFacing;
		class AirstrikeSlaveEntry : BaseSpawnerSlaveEntry
		{
			public int RearmTicks = 0;
			public bool IsLaunched = false;
			public new AirstrikeSlave SpawnerSlave;
		}

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();

		public new AirstrikeMasterInfo Info { get; private set; }

		AirstrikeSlaveEntry[] slaveEntries;
		ConditionManager conditionManager;

		Stack<int> loadedTokens = new Stack<int>();

		int respawnTicks = 0;

		public AirstrikeMaster(ActorInitializer init, AirstrikeMasterInfo info) : base(init, info)
		{
			Info = info;
		}

		public override void Replenish(Actor self, BaseSpawnerSlaveEntry entry)
		{
			if (entry.IsValid)
				throw new InvalidOperationException("Replenish must not be run on a valid entry!");

			string attacker = entry.ActorName;

			Game.Sound.Play(SoundType.World, Info.MarkSound);

			self.World.AddFrameEndTask(w =>
			{
				for (var i = -Info.SquadSize / 2; i <= Info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (Info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates

					var slave = w.CreateActor(attacker, new TypeDictionary()
					{
						new CenterPositionInit(startEdge + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(attackFacing)
					});

					// Initialize slave entry
					InitializeSlaveEntry(slave, entry);
					entry.SpawnerSlave.LinkMaster(entry.Actor, self, this);
				}
			});
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			conditionManager = self.Trait<ConditionManager>();
		}

		public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			slaveEntries = new AirstrikeSlaveEntry[info.Actors.Length]; // For this class to use

			for (int i = 0; i < slaveEntries.Length; i++)
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

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled)
				return;

			if (a.Info.Name != Info.SpawnerArmamentName)
				return;

			// Issue retarget order for already launched ones
			foreach (var slave in slaveEntries)
				if (slave.IsLaunched && slave.IsValid)
					slave.SpawnerSlave.Attack(slave.Actor, target);

			var se = GetLaunchable();
			if (se == null)
				return;

			se.IsLaunched = true; // mark as launched

			// Launching condition is timed, so not saving the token.
			if (Info.LaunchingCondition != null)
				conditionManager.GrantCondition(self, Info.LaunchingCondition/*, Info.LaunchingTicks*/);

			SpawnIntoWorld(self, se.Actor, self.CenterPosition);

			se.SpawnerSlave.SetSpawnInfo(finishEdge, spawnOffset);

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// The actor might had been trying to do something before entering the carrier.
				// Cancel whatever it was trying to do.
				se.SpawnerSlave.Stop(se.Actor);
				se.Actor.PlayVoice(Info.MarkSound);
				se.SpawnerSlave.Attack(se.Actor, target);
			});
		}

		public override void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			World w = self.World;

			WPos target = centerPosition;

			for (var i = -Info.SquadSize / 2; i <= Info.SquadSize / 2; i++)
			{
				int attackFacing = 256 * self.World.SharedRandom.Next(Info.QuantizedFacings) / Info.QuantizedFacings;

				var altitude = self.World.Map.Rules.Actors[slave.Info.Name].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
				var attackRotation = WRot.FromFacing(attackFacing);
				var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
				target = target + new WVec(0, 0, altitude);
				var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + Info.Cordon).Length * delta / 1024;
				var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + Info.Cordon).Length * delta / 1024;

				var so = Info.SquadOffset;
				var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
				var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(attackRotation);

				this.attackFacing = attackFacing;
				this.spawnOffset = spawnOffset;
				this.startEdge = startEdge;
				this.finishEdge = finishEdge;

				w.AddFrameEndTask(_ =>
				{
					if (!slave.IsInWorld)
						w.Add(slave);

					var attack = slave.Trait<AttackAircraft>();
					attack.AttackTarget(Target.FromPos(target + targetOffset), false, true);
				});
			}
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			Recall(self);
		}

		void Recall(Actor self)
		{
			// Tell launched slaves to come back and enter me.
			foreach (var se in slaveEntries)
				if (se.IsLaunched && se.IsValid)
					se.SpawnerSlave.EnterSpawner(se.Actor);
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		AirstrikeSlaveEntry GetLaunchable()
		{
			foreach (var se in slaveEntries)
				if (se.RearmTicks <= 0 && !se.IsLaunched && se.IsValid)
					return se;

			return null;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			int inside = 0;
			foreach (var se in slaveEntries)
				if (se.IsValid && !se.IsLaunched)
					inside++;

			for (var i = 0; i < Info.Actors.Length; i++)
			{
				if (i < inside)
					yield return Info.PipType;
				else
					yield return PipType.Transparent;
			}
		}

		public void PickupSlave(Actor self, Actor a)
		{
			AirstrikeSlaveEntry slaveEntry = null;
			foreach (var se in slaveEntries)
				if (se.Actor == a)
				{
					slaveEntry = se;
					break;
				}

			if (slaveEntry == null)
				throw new InvalidOperationException("An actor that isn't my slave entered me?");

			slaveEntry.IsLaunched = false;

			// setup rearm
			slaveEntry.RearmTicks = Info.RearmTicks;

			string spawnContainCondition;
			if (conditionManager != null && Info.SpawnContainConditions.TryGetValue(a.Info.Name, out spawnContainCondition))
				spawnContainTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
		}

		public void Tick(Actor self)
		{
			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, slaveEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(slaveEntries) != null)
						respawnTicks = Info.RespawnTicks;
				}
			}

			// Rearm
			foreach (var se in slaveEntries)
			{
				if (se.RearmTicks > 0)
					se.RearmTicks--;
			}
		}
	}
}
