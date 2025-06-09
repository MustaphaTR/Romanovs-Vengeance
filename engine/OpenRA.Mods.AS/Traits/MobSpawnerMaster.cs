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
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
 * Needs base engine modification.
 * In AttackOmni.cs, SetTarget() made public.
 * In Mobile.cs, OccupySpace (true by default) added. Aircraft trait for a dummy unit wasn't the greatest idea as they fly over anything.
 * Move.cs, uses my PR which isn't in bleed yet. (PR to make Move use parent child activity)
 *
 * The difference between Spawner (carrier logic) and this is that
 * carriers have units going in and out of the master actor for reload activities,
 * while MobSpawner doesn't, thus MobSpawner has much simpler code.
 */

/*
 * The code is very similar to Spawner.cs.
 * Sometimes it is neater to have a duplicate than to have wrong inheirtances.
 */

namespace OpenRA.Mods.AS.Traits
{
	// What to do when master is killed or mind controlled
	public enum MobMemberDisposal
	{
		DoNothing,
		KillSlaves,
		GiveSlavesToAttacker
	}

	[Desc("This actor can spawn actors.")]
	public class MobSpawnerMasterInfo : BaseSpawnerMasterInfo
	{
		public readonly string Name = "primary";

		[Desc("Spawn at a member, not the nexus?")]
		public readonly bool ExitByBudding = true;

		[Desc("Can the slaves be controlled independently?")]
		public readonly bool SlavesHaveFreeWill = false;

		[Desc("This is a dummy spawner like in C&C Generals and use virtual position and health.")]
		public readonly bool AggregateHealth = true;

		public readonly int AggregateHealthUpdateDelay = 17; // Just a visual parameter, Doesn't affect the game.

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Actors == null || Actors.Length == 0)
				throw new YamlException($"Actors is null or empty for MobSpawner for actor type {ai.Name}!");

			if (InitialActorCount > Actors.Length || InitialActorCount < -1)
				throw new YamlException("MobSpawner can't have more InitialActorCount than the actors defined!");

			if (InitialActorCount == 0 && AggregateHealth)
				throw new YamlException("You can't have InitialActorCount == 0 and AggregateHealth");
		}

		public override object Create(ActorInitializer init) { return new MobSpawnerMaster(init, this); }
	}

	public class MobSpawnerMaster : BaseSpawnerMaster, INotifyOwnerChanged, ITick,
		IResolveOrder, INotifyAttack
	{
		class MobSpawnerSlaveEntry : BaseSpawnerSlaveEntry
		{
			public new MobSpawnerSlave SpawnerSlave;
			public Health Health;
		}

		public new MobSpawnerMasterInfo Info { get; }

		MobSpawnerSlaveEntry[] slaveEntries;

		bool hasSpawnedInitialLoad = false;
		int spawnReplaceTicks = 0;

		IPositionable position;
		Aircraft aircraft;
		Health health;

		public MobSpawnerMaster(ActorInitializer init, MobSpawnerMasterInfo info)
			: base(init, info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			position = self.TraitOrDefault<IPositionable>();
			health = self.Trait<Health>();
			aircraft = self.TraitOrDefault<Aircraft>();

			base.Created(self);

			if (!IsTraitDisabled)
			{
				// Spawn initial load.
				var burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
				for (var i = 0; i < burst; i++)
					Replenish(self, SlaveEntries);

				// The base class creates the slaves but doesn't move them into world.
				// Let's do it here.
				SpawnReplenishedSlaves(self);

				hasSpawnedInitialLoad = true;
			}
		}

		public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			slaveEntries = new MobSpawnerSlaveEntry[info.Actors.Length]; // For this class to use

			for (var i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new MobSpawnerSlaveEntry();

			return slaveEntries; // For the base class to use
		}

		public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			var se = entry as MobSpawnerSlaveEntry;
			base.InitializeSlaveEntry(slave, se);

			se.SpawnerSlave = slave.Trait<MobSpawnerSlave>();
			se.Health = slave.Trait<Health>();
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
			// Mob Master only pause attack when trait is Disabled
			// HACK: If Armament hits instantly and kills the target, the target will become invalid
			if (target.Type == TargetType.Invalid
				|| (Info.ArmamentNames.Count > 0 && !Info.ArmamentNames.Contains(a.Info.Name))
				|| Info.SlavesHaveFreeWill
				|| IsTraitDisabled)
				return;

			AssignTargetsToSlaves(target);
		}

		void ITick.Tick(Actor self)
		{
			if (spawnReplaceTicks > 0 && !IsTraitDisabled)
			{
				spawnReplaceTicks--;

				// Time to respawn someting.
				if (spawnReplaceTicks <= 0)
				{
					Replenish(self, slaveEntries);

					SpawnReplenishedSlaves(self);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(slaveEntries) != null)
						spawnReplaceTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier(Info.Name)));
				}
			}

			// I'm a virtual mob spawning nexus.
			if (Info.AggregateHealth)
			{
				SetNexusPosition(self);
				SetNexusHealth(self);
			}

			if (!Info.SlavesHaveFreeWill)
				AssignSlaveActivity(self);
		}

		void SpawnReplenishedSlaves(Actor self)
		{
			var centerPosition = WPos.Zero;
			if (!hasSpawnedInitialLoad || !Info.ExitByBudding)
			{
				// Spawning from a solid actor...
				centerPosition = self.CenterPosition;
			}
			else
			{
				// Spawning from a virtual nexus: exit by an existing member.
				var se = Array.Find(slaveEntries, s => s.IsValid && s.Actor.IsInWorld);
				if (se != null)
					centerPosition = se.Actor.CenterPosition;
			}

			// WPos.Zero implies this mob spawner master is dead or something.
			if (centerPosition == WPos.Zero)
				return;

			foreach (var se in slaveEntries)
				if (se.IsValid && !se.Actor.IsInWorld)
					SpawnIntoWorld(self, se.Actor, centerPosition + se.Offset.Rotate(self.Orientation));
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			// No need to update mobs entry because Actor.IsDead marking is done automatically by the engine.
			// However, we need to check if all are dead when AggregateHealth.
			if (Info.AggregateHealth && slaveEntries.All(m => !m.IsValid))
				self.Dispose();

			if (spawnReplaceTicks <= 0)
				spawnReplaceTicks = Info.RespawnTicks;
		}

		void AssignTargetsToSlaves(Target target)
		{
			foreach (var se in slaveEntries)
			{
				if (!se.IsValid)
					continue;

				se.SpawnerSlave.Attack(se.Actor, target);
			}
		}

		void MoveSlaves(Actor self)
		{
			var targets = self.CurrentActivity.GetTargets(self);
			if (!targets.Any())
				return;

			var location = self.World.Map.CellContaining(targets.First().CenterPosition);

			foreach (var se in slaveEntries)
			{
				if (!se.IsValid || !se.Actor.IsInWorld)
					continue;

				if (se.Actor.Location == location)
					continue;

				if (!se.SpawnerSlave.IsMoving())
				{
					se.SpawnerSlave.Stop(se.Actor);
					se.SpawnerSlave.Move(se.Actor, location);
				}
			}
		}

		CPos lastAttackMoveLocation;
		void AttackMoveSlaves(Actor self)
		{
			var targets = self.CurrentActivity.GetTargets(self);
			if (!targets.Any())
				return;

			var location = self.World.Map.CellContaining(targets.First().CenterPosition);

			if (lastAttackMoveLocation == location)
				return;

			lastAttackMoveLocation = location;

			foreach (var se in slaveEntries)
			{
				if (!se.IsValid || !se.Actor.IsInWorld)
					continue;

				se.SpawnerSlave.AttackMove(se.Actor, location);
			}
		}

		void SetNexusPosition(Actor self)
		{
			int x = 0, y = 0, cnt = 0;
			foreach (var se in slaveEntries)
			{
				if (!se.IsValid || !se.Actor.IsInWorld)
					continue;

				var pos = se.Actor.CenterPosition;
				x += pos.X;
				y += pos.Y;
				cnt++;
			}

			if (cnt == 0)
				return;

			var newPos = new WPos(x / cnt, y / cnt, aircraft != null ? aircraft.Info.CruiseAltitude.Length : 0);
			if (aircraft == null)
				position.SetPosition(self, newPos); // breaks arrival detection of the aircraft if we set position.

			position.SetCenterPosition(self, newPos);
		}

		int aggregateHealthUpdateTicks = 0;

		void SetNexusHealth(Actor self)
		{
			if (!Info.AggregateHealth)
				return;

			if (aggregateHealthUpdateTicks > 0)
			{
				aggregateHealthUpdateTicks--;
				return;
			}

			aggregateHealthUpdateTicks = Info.AggregateHealthUpdateDelay;

			// Time to aggregate health.
			var maxHealth = 0;
			var h = 0;

			foreach (var se in slaveEntries)
			{
				maxHealth += se.Health.MaxHP;

				if (!se.IsValid)
					continue;

				h += se.Health.HP;
			}

			// Apply the aggregate health.
			h = h * health.MaxHP / maxHealth;

			if (h > 0)
			{
				// Only do these when h > 0.
				// Nexus kill when wiped out is handled else where.
				// We can't set health. Inflict damage instead.
				health.InflictDamage(self, self, new Damage(-health.MaxHP), true); // fully heal
				health.InflictDamage(self, self, new Damage(health.MaxHP - h), true); // remove some health
			}
		}

		void AssignSlaveActivity(Actor self)
		{
			if (self.CurrentActivity is Move || self.CurrentActivity is Fly)
				MoveSlaves(self);
			else if (self.CurrentActivity is AttackMoveActivity)
				AttackMoveSlaves(self);
			else if (self.CurrentActivity is AttackOmni.SetTarget)
				AssignTargetsToSlaves(self.CurrentActivity.GetTargets(self).First());
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!Info.EnabledByDefault && !hasSpawnedInitialLoad)
			{
				var burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
				for (var i = 0; i < burst; i++)
					Replenish(self, SlaveEntries);

				SpawnReplenishedSlaves(self);
				hasSpawnedInitialLoad = true;
			}
		}
	}
}
