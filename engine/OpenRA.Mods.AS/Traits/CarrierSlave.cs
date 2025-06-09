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
	public class CarrierSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Move this close to the spawner, before entering it.")]
		public readonly WDist LandingDistance = new(5 * 1024);

		public override object Create(ActorInitializer init) { return new CarrierSlave(init, this); }
	}

	public class CarrierSlave : BaseSpawnerSlave, INotifyIdle
	{
		// readonly AmmoPool[] ammoPools;
		public readonly CarrierSlaveInfo Info;

		CarrierMaster spawnerMaster;

		public CarrierSlave(ActorInitializer init, CarrierSlaveInfo info)
			: base(info)
		{
			Info = info;
			/* ammoPools = init.Self.TraitsImplementing<AmmoPool>().ToArray(); */
		}

		public void EnterSpawner(Actor self)
		{
			// Hopefully, self will be disposed shortly afterwards by SpawnerSlaveDisposal policy.
			if (Master == null || Master.IsDead)
				return;

			// Proceed with enter, if already at it.
			if (self.CurrentActivity is EnterCarrierMaster)
				return;

			// Cancel whatever else self was doing and return.
			self.QueueActivity(false, new EnterCarrierMaster(self, Master, spawnerMaster));
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as CarrierMaster;
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
			EnterSpawner(self);
		}

		public override void Stop(Actor self)
		{
			base.Stop(self);
			EnterSpawner(self);
		}
	}
}
