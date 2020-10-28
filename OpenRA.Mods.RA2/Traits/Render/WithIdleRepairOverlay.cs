#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	// TODO: Refactor this trait into WithResupplyOverlay
	[Desc("Displays an overlay when the building is being repaired by the player.")]
	public class WithIdleRepairOverlayInfo : PausableConditionalTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference("Image")]
		[Desc("Sequence name to use")]
		public readonly string IdleSequence = "idle-overlay";

		[SequenceReference("Image")]
		[Desc("Sequence to use upon repair beginning.")]
		public readonly string StartSequence = null;

		[SequenceReference]
		[Desc("Sequence name to play once during repair intervals or repeatedly if a start sequence is set.")]
		public readonly string Sequence = "active";

		[SequenceReference("Image")]
		[Desc("Sequence to use after repairing has finished.")]
		public readonly string EndSequence = null;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		public readonly bool IsDecoration = false;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithIdleRepairOverlay(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			if (Palette != null)
				p = init.WorldRenderer.Palette(Palette);

			Func<WAngle> facing;
			var dynamicfacingInit = init.GetOrDefault<DynamicFacingInit>();
			if (dynamicfacingInit != null)
				facing = dynamicfacingInit.Value;
			else
			{
				var f = init.GetValue<FacingInit, WAngle>(WAngle.Zero);
				facing = () => f;
			}

			var anim = new Animation(init.World, image, facing);
			anim.IsDecoration = IsDecoration;
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), IdleSequence));

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			Func<WRot> orientation = () => body.QuantizeOrientation(WRot.FromYaw(facing()), facings);
			Func<WVec> offset = () => body.LocalToWorld(Offset.Rotate(orientation()));
			Func<int> zOffset = () =>
			{
				var tmpOffset = offset();
				return tmpOffset.Y + tmpOffset.Z + 1;
			};

			yield return new SpriteActorPreview(anim, offset, zOffset, p, rs.Scale);
		}
	}

	public class WithIdleRepairOverlay : PausableConditionalTrait<WithIdleRepairOverlayInfo>, INotifyDamageStateChanged, INotifyResupply
	{
		readonly Animation overlay;
		bool idling;
		bool repairing;

		public WithIdleRepairOverlay(Actor self, WithIdleRepairOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.IdleSequence));
			idling = true;

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		void INotifyResupply.BeforeResupply(Actor self, Actor target, ResupplyType types)
		{
			repairing = types.HasFlag(ResupplyType.Repair);
			if (!repairing)
				return;

			if (Info.StartSequence != null)
			{
				idling = false;
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.StartSequence),
					() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.Sequence)));
			}
		}

		void INotifyResupply.ResupplyTick(Actor self, Actor target, ResupplyType types)
		{
			var wasRepairing = repairing;
			repairing = types.HasFlag(ResupplyType.Repair);

			if (repairing && Info.StartSequence == null && idling)
			{
				idling = false;
				overlay.PlayThen(Info.Sequence, () =>
				{
					overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.IdleSequence));
					idling = true;
				});
			}

			if (!repairing && wasRepairing && Info.EndSequence != null)
			{
				idling = false;
				overlay.PlayThen(Info.EndSequence, () =>
				{
					overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.IdleSequence));
					idling = true;
				});
			}
		}
	}
}
