#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Show an indicator revealing the actor underneath the fog when a RangedGPSProvider is activated.")]
	class RangedGpsDotInfo : ConditionalTraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite used for this actor.")]
		public readonly string Sequence = "idle";

		[PaletteReference(true)]
		public readonly string IndicatorPalettePrefix = "player";

		public readonly bool VisibleInShroud = true;

		public override object Create(ActorInitializer init) { return new RangedGpsDot(this); }
	}

	class RangedGpsDot : ConditionalTrait<RangedGpsDotInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		RangedGpsDotEffect effect;
		public readonly List<Actor> Providers = new();

		public RangedGpsDot(RangedGpsDotInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			effect = new RangedGpsDotEffect(self, this);

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
