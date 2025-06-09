#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class ChronoResourceTeleport : Activity
	{
		readonly CPos destination;
		readonly ChronoResourceDeliveryInfo info;
		readonly CPos harvestedField;
		readonly Actor hostActor;
		readonly DockHost host;
		readonly bool forceEnter;

		public ChronoResourceTeleport(CPos destination, ChronoResourceDeliveryInfo info, CPos harvestedField, Actor hostActor, DockHost host, bool forceEnter)
		{
			this.destination = destination;
			this.info = info;
			this.harvestedField = harvestedField;
			this.hostActor = hostActor;
			this.host = host;
			this.forceEnter = forceEnter;
		}

		public override bool Tick(Actor self)
		{
			var sourcepos = self.CenterPosition;

			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;

			foreach (var ost in self.TraitsImplementing<IOnSuccessfulTeleportRA2>())
				ost.OnSuccessfulTeleport(info.TeleportType, sourcepos, self.CenterPosition);

			var facing = self.TraitOrDefault<IFacing>();
			if (facing != null)
				facing.Facing = host.Info.DockAngle;

			if (hostActor == null)
				self.QueueActivity(new FindAndDeliverResources(self, harvestedField));
			else
				self.QueueActivity(new MoveToDock(self, hostActor, host, forceEnter));

			return true;
		}
	}
}
