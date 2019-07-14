#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("When returning to a refinery to deliver resources, this actor will teleport if possible.")]
	public class ChronoResourceDeliveryInfo : ConditionalTraitInfo, Requires<HarvesterInfo>
	{
		[Desc("The number of ticks between each check to see if we can teleport to the refinery.")]
		public readonly int CheckTeleportDelay = 10;

		[Desc("Image used for the teleport effects. Defaults to the actor's type.")]
		public readonly string Image = null;

		[SequenceReference("Image")]
		[Desc("Sequence used for the effect played where the harvester jumped from.")]
		public readonly string WarpInSequence = null;

		[SequenceReference("Image")]
		[Desc("Sequence used for the effect played where the harvester jumped to.")]
		public readonly string WarpOutSequence = null;

		[PaletteReference]
		[Desc("Palette to render the warp in/out sprites in.")]
		public readonly string Palette = "effect";

		[Desc("Sound played where the harvester jumped from.")]
		public readonly string WarpInSound = null;

		[Desc("Sound where the harvester jumped to.")]
		public readonly string WarpOutSound = null;

		[Desc("Does the sound play under shroud or fog.")]
		public readonly bool RequireVisibilyForSound = true;

		[Desc("Volume the WarpInSound and WarpOutSound played at.")]
		public readonly float SoundVolume = 1;

		public override object Create(ActorInitializer init) { return new ChronoResourceDelivery(init.Self, this); }
	}

	public class ChronoResourceDelivery : ConditionalTrait<ChronoResourceDeliveryInfo>, INotifyHarvesterAction, ITick
	{
		CPos? destination = null;
		CPos harvestedField;
		int ticksTillCheck = 0;

		public ChronoResourceDelivery(Actor self, ChronoResourceDeliveryInfo info)
			: base(info) { }

		public void Tick(Actor self)
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

		public void MovingToResources(Actor self, CPos targetCell, Activity next)
		{
			Reset();
		}

		public void MovingToRefinery(Actor self, Actor refineryActor, Activity next)
		{
			var iao = refineryActor.Trait<IAcceptResources>();
			var targetCell = refineryActor.Location + iao.DeliveryOffset;
			if (destination != null && destination.Value != targetCell)
				ticksTillCheck = 0;

			harvestedField = self.World.Map.CellContaining(self.CenterPosition);

			destination = targetCell;
		}

		public void MovementCancelled(Actor self)
		{
			Reset();
		}

		public void Harvested(Actor self, ResourceType resource) { }
		public void Docked() { }
		public void Undocked() { }

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
                self.QueueActivity(false, new ChronoResourceTeleport(destination.Value, Info, harvestedField));
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
