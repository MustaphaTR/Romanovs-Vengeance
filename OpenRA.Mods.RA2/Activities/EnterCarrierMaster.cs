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
using OpenRA.Mods.RA2.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class EnterCarrierMaster : Enter
	{
		readonly Actor master; // remember the spawner.
		readonly CarrierMaster spawnerMaster;

		public EnterCarrierMaster(Actor self, Target master, CarrierMaster spawnerMaster, EnterBehaviour enterBehaviour)
			: base(self, master, Color.Green)
		{
			this.master = master.Actor;
			this.spawnerMaster = spawnerMaster;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			return true; // Slaves are always welcome.
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			// Master got killed :(
			if (master.IsDead)
				return;

			Cancel(self); // Stop slaves from exiting.

			// Load this thingy.
			// Issue attack move to the rally point.
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || master.IsDead)
					return;

				spawnerMaster.PickupSlave(master, self);
				w.Remove(self);

				// Insta repair.
				if (spawnerMaster.Info.InstaRepair)
				{
					var health = self.Trait<Health>();
					self.InflictDamage(self, new Damage(-health.MaxHP));
				}

				// Insta re-arm. (Delayed launching is handled at spawner.)
				var ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
				if (ammoPools != null)
					foreach (var pool in ammoPools)
						while (!pool.FullAmmo())
							pool.GiveAmmo(self, 1);
			});
		}
	}
}
