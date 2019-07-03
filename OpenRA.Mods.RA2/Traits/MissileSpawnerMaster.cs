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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

/*
 * Works without base engine modification?
 */

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can spawn missile actors.")]
	public class MissileSpawnerMasterInfo : BaseSpawnerMasterInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("Pip color for the spawn count.")]
		public readonly PipType PipType = PipType.Green;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new MissileSpawnerMaster(init, this); }
	}

	public class MissileSpawnerMaster : BaseSpawnerMaster, IPips, ITick, INotifyAttack
	{
		public new MissileSpawnerMasterInfo Info { get; private set; }

		ConditionManager conditionManager;
		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();
		Stack<int> loadedTokens = new Stack<int>();
		IFirepowerModifier[] firepowerModifiers;

		int respawnTicks = 0;

		public MissileSpawnerMaster(ActorInitializer init, MissileSpawnerMasterInfo info)
			: base(init, info)
		{
			Info = info;

			firepowerModifiers = init.Self.TraitsImplementing<IFirepowerModifier>().ToArray();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			conditionManager = self.Trait<ConditionManager>();

			if (conditionManager != null)
			{
				foreach (var entry in SlaveEntries)
				{
					string spawnContainCondition;
					if (Info.SpawnContainConditions.TryGetValue(entry.Actor.Info.Name, out spawnContainCondition))
						spawnContainTokens.GetOrAdd(entry.Actor.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

					if (!string.IsNullOrEmpty(Info.LoadedCondition))
						loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
				}
			}
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
			foreach (var slave in SlaveEntries)
				if (slave.IsValid)
					slave.SpawnerSlave.Attack(slave.Actor, target);

			var se = GetLaunchable();
			if (se == null)
				return;

			// Launching condition is timed, so not saving the token.
			if (Info.LaunchingCondition != null)
				conditionManager.GrantCondition(self, Info.LaunchingCondition);

			// Program the trajectory.
			var sbm = se.Actor.Trait<ShootableBallisticMissile>();
			sbm.Target = Target.FromPos(target.CenterPosition);
			sbm.FirepowerModifiers = Util.ApplyPercentageModifiers(100, firepowerModifiers.Select(fm => fm.GetFirepowerModifier()));

			SpawnIntoWorld(self, se.Actor, self.CenterPosition);

			Stack<int> spawnContainToken;
			if (spawnContainTokens.TryGetValue(a.Info.Name, out spawnContainToken) && spawnContainToken.Any())
				conditionManager.RevokeCondition(self, spawnContainToken.Pop());

			if (loadedTokens.Any())
				conditionManager.RevokeCondition(self, loadedTokens.Pop());

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				se.Actor.QueueActivity(new ShootableBallisticMissileFly(se.Actor, sbm.Target, sbm));

				// invalidate the slave entry so that slave will regen.
				se.Actor = null;
			});

			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		BaseSpawnerSlaveEntry GetLaunchable()
		{
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					return se;

			return null;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			int inside = 0;
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					inside++;

			for (var i = 0; i < Info.Actors.Length; i++)
			{
				if (i < inside)
					yield return Info.PipType;
				else
					yield return PipType.Transparent;
			}
		}

		public override void Replenish(Actor self, BaseSpawnerSlaveEntry entry)
		{
			base.Replenish(self, entry);

			string spawnContainCondition;
			if (conditionManager != null)
			{
				if (Info.SpawnContainConditions.TryGetValue(entry.Actor.Info.Name, out spawnContainCondition))
					spawnContainTokens.GetOrAdd(entry.Actor.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

				if (!string.IsNullOrEmpty(Info.LoadedCondition))
					loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));
			}
		}

		public void Tick(Actor self)
		{
			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, SlaveEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(SlaveEntries) != null)
						respawnTicks = Info.RespawnTicks;
				}
			}
		}
	}
}
