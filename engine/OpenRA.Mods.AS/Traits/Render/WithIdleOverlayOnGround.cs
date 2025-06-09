#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Render
{
	[Desc("Plays an idle overlay on the ground position under the actor (regardless of it's actual height).")]
	public class WithIdleOverlayOnGroundInfo : WithIdleOverlayInfo
	{
		public override object Create(ActorInitializer init) { return new WithIdleOverlayOnGround(init.Self, this); }

		public new IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			Func<WAngle> facing;
			var dynamicfacingInit = init.GetOrDefault<DynamicFacingInit>(this);
			if (dynamicfacingInit != null)
				facing = dynamicfacingInit.Value;
			else
			{
				var f = init.GetValue<FacingInit, WAngle>(this, WAngle.Zero);
				facing = () => f;
			}

			var anim = new Animation(init.World, image, facing);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			WRot Orientation() => body.QuantizeOrientation(WRot.FromYaw(facing()), facings);
			WVec Offset() => body.LocalToWorld(this.Offset.Rotate(Orientation()));
			int ZOffset()
			{
				var tmpOffset = Offset();
				return tmpOffset.Y + tmpOffset.Z + 1;
			}

			yield return new SpriteActorPreview(anim, Offset, ZOffset, p);
		}
	}

	public class WithIdleOverlayOnGround : PausableConditionalTrait<WithIdleOverlayOnGroundInfo>, INotifyDamageStateChanged
	{
		readonly Animation overlay;

		public WithIdleOverlayOnGround(Actor self, WithIdleOverlayOnGroundInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			var image = info.Image ?? rs.GetImage(self);
			overlay = new Animation(self.World, image, () => IsTraitPaused)
			{
				IsDecoration = info.IsDecoration
			};
			if (info.StartSequence != null)
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.StartSequence),
					() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence)));
			else
				overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence));

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation)))
					- new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(self.CenterPosition)),
				() => IsTraitDisabled,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}
