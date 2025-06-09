#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Shows an overlay over the cameo on the ActorIconWidget.")]
	public class WithStatIconOverlayInfo : ConditionalTraitInfo
	{
		[Desc("Image used for this overlay. Defaults to the actor's type.")]
		public readonly string Image = null;

		[FieldLoader.Require]
		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Sequence name to use")]
		public readonly string Sequence;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = "chrome";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithStatIconOverlay(init.Self, this); }
	}

	public class WithStatIconOverlay : ConditionalTrait<WithStatIconOverlayInfo>
	{
		public readonly Sprite Sprite;

		public WithStatIconOverlay(Actor self, WithStatIconOverlayInfo info)
			: base(info)
		{
			var image = info.Image ?? self.Info.Name;
			var anim = new Animation(self.World, image);
			anim.Play(info.Sequence);
			Sprite = anim.Image;
		}

		public float2 GetOffset(int2 iconSize, float iconScale = 1f)
		{
			var x = (Sprite.Size.X * iconScale - iconSize.X) / 2;
			var y = (Sprite.Size.Y * iconScale - iconSize.Y) / 2;
			return new float2(x, y);
		}
	}
}
