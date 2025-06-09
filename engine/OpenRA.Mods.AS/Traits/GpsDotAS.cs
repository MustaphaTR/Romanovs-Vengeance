#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Show an indicator revealing the actor underneath the fog when a GpsASProvider is activated.")]
	class GpsDotASInfo : ConditionalTraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite used for this actor.")]
		public readonly string Sequence = "idle";

		[PaletteReference(true)]
		public readonly string IndicatorPalettePrefix = "player";

		public readonly bool VisibleInShroud = true;

		public readonly WDist Range = WDist.Zero;

		public override object Create(ActorInitializer init) { return new GpsDotAS(this); }
	}

	class GpsDotAS : ConditionalTrait<GpsDotASInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		GpsDotEffectAS effect;

		public GpsDotAS(GpsDotASInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			effect = new GpsDotEffectAS(self, this);

			base.Created(self);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}
	}
}
