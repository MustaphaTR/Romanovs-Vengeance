#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.AS.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Can be slaved to a spawner.")]
	public class AirstrikeSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new(5 * 1024);

		[Desc("We consider this is close enought to the spawner and enter it, instead of trying to reach 0 distance." +
			"This allows the spawned unit to enter the spawner while the spawner is moving.")]
		public readonly WDist CloseEnoughDistance = new(128);

		public override object Create(ActorInitializer init) { return new AirstrikeSlave(init, this); }
	}

	public class AirstrikeSlave : BaseSpawnerSlave, INotifyIdle
	{
		// readonly AmmoPool[] ammoPools;
		public readonly AirstrikeSlaveInfo Info;

		// WPos targetPos;
		WPos finishEdge;
		WVec spawnOffset;

		AirstrikeMaster spawnerMaster;

		public AirstrikeSlave(ActorInitializer init, AirstrikeSlaveInfo info)
			: base(info)
		{
			Info = info;
			/* ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray(); */
		}

		public void SetSpawnInfo(WPos finishEdge, WVec spawnOffset, WPos targetPos)
		{
			// this.targetPos = targetPos;
			this.finishEdge = finishEdge;
			this.spawnOffset = spawnOffset;
		}

		public void LeaveMap(Actor self)
		{
			// Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
			if (Master == null || Master.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is ReturnAirstrikeMaster)
				return;

			// Cancel whatever else self was doing and return.
			self.QueueActivity(false, new ReturnAirstrikeMaster(Master, spawnerMaster, finishEdge + spawnOffset));
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as AirstrikeMaster;
		}

		/* bool NeedToReload()
		{
			// The unit may not have ammo but will have unlimited ammunitions.
			if (ammoPools.Length == 0)
				return false;

			return ammoPools.All(x => !x.HasAmmo);
		} */

		void INotifyIdle.TickIdle(Actor self)
		{
			LeaveMap(self);
		}
	}
}
