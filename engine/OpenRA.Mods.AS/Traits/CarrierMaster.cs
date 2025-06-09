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
	[Desc("This actor can spawn actors.")]
	public class CarrierMasterInfo : BaseSpawnerMasterInfo
	{
		public readonly string Name = "primary";

		[Desc("Spawn is a missile that dies and not return.")]
		public readonly bool SpawnIsMissile = false;

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

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new CarrierMaster(init, this); }
	}

	public class CarrierMaster : BaseSpawnerMaster, ITick, IResolveOrder, INotifyAttack
	{
		class CarrierSlaveEntry : BaseSpawnerSlaveEntry
		{
			public int RearmTicks = 0;
			public new CarrierSlave SpawnerSlave;
		}

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new();
		public readonly CarrierMasterInfo CarrierMasterInfo;

		readonly Stack<int> loadedTokens = new();
		int respawnTicks = 0;

		int launchCondition = Actor.InvalidConditionToken;
		int launchConditionTicks;

		public CarrierMaster(ActorInitializer init, CarrierMasterInfo info)
			: base(init, info)
		{
			CarrierMasterInfo = info;
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
			var slaveEntries = new CarrierSlaveEntry[info.Actors.Length]; // For this class to use

			for (var i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new CarrierSlaveEntry();

			return slaveEntries; // For the base class to use
		}

		public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			base.InitializeSlaveEntry(slave, entry);

			var carrierSlaveEntry = entry as CarrierSlaveEntry;
			carrierSlaveEntry.RearmTicks = 0;
			carrierSlaveEntry.IsLaunched = false;
			carrierSlaveEntry.SpawnerSlave = slave.Trait<CarrierSlave>();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Stop")
				Recall();
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

			var carrierSlaveEntry = GetLaunchable();
			if (carrierSlaveEntry == null)
				return;

			carrierSlaveEntry.IsLaunched = true; // mark as launched

			if (CarrierMasterInfo.LaunchingCondition != null)
			{
				if (launchCondition == Actor.InvalidConditionToken)
					launchCondition = self.GrantCondition(CarrierMasterInfo.LaunchingCondition);

				launchConditionTicks = CarrierMasterInfo.LaunchingTicks;
			}

			SpawnIntoWorld(self, carrierSlaveEntry.Actor, self.CenterPosition + carrierSlaveEntry.Offset.Rotate(self.Orientation));

			if (spawnContainTokens.TryGetValue(a.Info.Name, out var spawnContainToken) && spawnContainToken.Count > 0)
				self.RevokeCondition(spawnContainToken.Pop());

			if (loadedTokens.Count > 0)
				self.RevokeCondition(loadedTokens.Pop());

			// Lambdas can't use 'in' variables, so capture a copy for later
			var delayedTarget = target;

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				// The actor might had been trying to do something before entering the carrier.
				// Cancel whatever it was trying to do.
				carrierSlaveEntry.SpawnerSlave.Stop(carrierSlaveEntry.Actor);

				carrierSlaveEntry.SpawnerSlave.Attack(carrierSlaveEntry.Actor, delayedTarget);
			});
		}

		void Recall()
		{
			// Tell launched slaves to come back and enter me.
			foreach (var slaveEntry in SlaveEntries)
				if (slaveEntry.IsLaunched && slaveEntry.IsValid)
				{
					var carrierSlaveEntry = slaveEntry as CarrierSlaveEntry;
					carrierSlaveEntry.SpawnerSlave.EnterSpawner(slaveEntry.Actor);
				}
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		CarrierSlaveEntry GetLaunchable()
		{
			foreach (var slaveEntry in SlaveEntries)
			{
				var carrierSlaveEntry = slaveEntry as CarrierSlaveEntry;
				if (carrierSlaveEntry.RearmTicks <= 0 && !slaveEntry.IsLaunched && slaveEntry.IsValid)
					return carrierSlaveEntry;
			}

			return null;
		}

		public void PickupSlave(Actor self, Actor a)
		{
			CarrierSlaveEntry slaveEntry = null;
			foreach (var carrierSlaveEntry in SlaveEntries)
				if (carrierSlaveEntry.Actor == a)
				{
					slaveEntry = carrierSlaveEntry as CarrierSlaveEntry;
					break;
				}

			if (slaveEntry == null)
				throw new InvalidOperationException("An actor that isn't my slave entered me?");

			slaveEntry.IsLaunched = false;

			// setup rearm
			slaveEntry.RearmTicks = Util.ApplyPercentageModifiers(
				CarrierMasterInfo.RearmTicks, reloadModifiers.Select(rm => rm.GetReloadModifier(CarrierMasterInfo.Name)));

			if (CarrierMasterInfo.SpawnContainConditions.TryGetValue(a.Info.Name, out var spawnContainCondition))
				spawnContainTokens.GetOrAdd(a.Info.Name).Push(self.GrantCondition(spawnContainCondition));

			if (!string.IsNullOrEmpty(CarrierMasterInfo.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(CarrierMasterInfo.LoadedCondition));
		}

		public override void Replenish(Actor self, BaseSpawnerSlaveEntry entry)
		{
			base.Replenish(self, entry);

			if (CarrierMasterInfo.SpawnContainConditions.TryGetValue(entry.Actor.Info.Name, out var spawnContainCondition))
				spawnContainTokens.GetOrAdd(entry.Actor.Info.Name).Push(self.GrantCondition(spawnContainCondition));

			if (!string.IsNullOrEmpty(CarrierMasterInfo.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(CarrierMasterInfo.LoadedCondition));
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
						respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier(CarrierMasterInfo.Name)));
				}
			}

			// Rearm
			foreach (var slaveEntry in SlaveEntries)
			{
				var carrierSlaveEntry = slaveEntry as CarrierSlaveEntry;
				if (carrierSlaveEntry.RearmTicks > 0)
					carrierSlaveEntry.RearmTicks--;
			}
		}

		protected override void TraitPaused(Actor self)
		{
			Recall();
		}
	}
}
