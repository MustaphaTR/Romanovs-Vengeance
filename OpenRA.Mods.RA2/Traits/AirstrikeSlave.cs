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

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class AirstrikeSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new WDist(5 * 1024);

		[Desc("We consider this is close enought to the spawner and enter it, instead of trying to reach 0 distance." +
			"This allows the spawned unit to enter the spawner while the spawner is moving.")]
		public readonly WDist CloseEnoughDistance = new WDist(128);

		public override object Create(ActorInitializer init) { return new AirstrikeSlave(init, this); }
	}

	public class AirstrikeSlave : BaseSpawnerSlave, INotifyBecomingIdle
	{
		public AirstrikeSlaveInfo Info { get; private set; }
		private WPos finishEdge;
		private WVec spawnOffset;
		readonly AmmoPool[] ammoPools;

		AirstrikeMaster spawnerMaster;

		public AirstrikeSlave(ActorInitializer init, AirstrikeSlaveInfo info)
			: base(init, info)
		{
			Info = info;
			ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray();
		}

		public void SetSpawnInfo(WPos finishEdge, WVec spawnOffset)
		{
			this.finishEdge = finishEdge;
			this.spawnOffset = spawnOffset;
		}

		public override void Attack(Actor self, Target target)
		{
			base.Attack(self, target);
		}

		public void EnterSpawner(Actor self)
		{
			// Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
			if (Master == null || Master.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is EnterAirstrikeMaster)
				return;

			// Cancel whatever else self was doing and return
			self.CancelActivity();

			self.QueueActivity(new Fly(self, Target.FromPos(finishEdge + spawnOffset)));
			self.QueueActivity(new EnterAirstrikeMaster(self, Master, spawnerMaster));
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as AirstrikeMaster;
		}

		bool NeedToReload(Actor self)
		{
			// The unit may not have ammo but will have unlimited ammunitions.
			if (ammoPools.Length == 0)
				return false;

			return ammoPools.All(x => !x.HasAmmo);
			/* AutoReloads seems to be removed and i dunno how exactly to implement this check now.
			 * Doesn't seem like we actually need it for RA2.
			 * return ammoPools.All(x => !x.AutoReloads && !x.HasAmmo());
			 */
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			EnterSpawner(self);
		}
	}
}
