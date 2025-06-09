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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When returning to a refinery to deliver resources, this actor will teleport if possible.")]
	public class ChronoResourceDeliveryInfo : ConditionalTraitInfo, Requires<HarvesterInfo>
	{
		[Desc("This trait only trigger teleport effect of this teleport type.")]
		public readonly string TeleportType = "RA2ChronoPower";

		[Desc("The number of ticks between each check to see if we can teleport to the refinery.")]
		public readonly int CheckTeleportDelay = 10;

		public override object Create(ActorInitializer init) { return new ChronoResourceDelivery(this); }
	}

	public class ChronoResourceDelivery : ConditionalTrait<ChronoResourceDeliveryInfo>, INotifyHarvestAction, INotifyDockClientMoving, ITick
	{
		CPos? destination = null;
		Actor hostActor = null;
		IDockHost host = null;
		CPos harvestedField;
		int ticksTillCheck = 0;
		bool forceEnter;

		public ChronoResourceDelivery(ChronoResourceDeliveryInfo info)
			: base(info) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || destination == null)
				return;

			if (ticksTillCheck <= 0)
			{
				ticksTillCheck = Info.CheckTeleportDelay;

				TeleportIfPossible(self);
			}
			else
				ticksTillCheck--;
		}

		void INotifyDockClientMoving.MovingToDock(Actor self, Actor hostActor, IDockHost host, bool forceEnter)
		{
			var deliverypos = self.World.Map.CellContaining(host.DockPosition);

			if (destination != null && destination.Value != deliverypos)
				ticksTillCheck = 0;

			harvestedField = self.World.Map.CellContaining(self.CenterPosition);

			this.forceEnter = forceEnter;
			this.hostActor = hostActor;
			this.host = host;
			destination = deliverypos;
		}

		void INotifyDockClientMoving.MovementCancelled(Actor self) { Reset(); }

		void INotifyHarvestAction.MovingToResources(Actor self, CPos targetCell) { Reset(); }

		void INotifyHarvestAction.MovementCancelled(Actor self) { Reset(); }

		void INotifyHarvestAction.Harvested(Actor self, string resourceType) { }

		void TeleportIfPossible(Actor self)
		{
			// We're already here; no need to interfere.
			if (self.Location == destination.Value)
			{
				Reset();
				return;
			}

			var pos = self.Trait<IPositionable>();
			if (pos.CanEnterCell(destination.Value))
			{
				self.QueueActivity(false, new ChronoResourceTeleport(destination.Value, Info, harvestedField, hostActor, host as DockHost, forceEnter));
				Reset();
			}
		}

		void Reset()
		{
			ticksTillCheck = 0;
			destination = null;
		}
	}
}
