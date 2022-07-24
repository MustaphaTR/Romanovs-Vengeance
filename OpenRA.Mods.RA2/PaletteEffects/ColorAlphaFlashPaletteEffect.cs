#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TA.Traits
{
	[Desc("The cloak palette effect used by TA.")]
	public class ColorAlphaFlashPaletteEffectInfo : TraitInfo
	{
		[FieldLoader.Require]
		[PaletteReference(true)]
		[Desc("The name of the player palette to base off.")]
		public readonly string AffectedPalette = null;

		[Desc("The name of the player palette to base off.")]
		public readonly bool IsAffectedPalettePlayerColor = false;

		[Desc("Alpha multipliers of the colors.")]
		public readonly float[] Alpha =	{ 0.3f, 0.6f, 0.9f };

		[Desc("Start Index to apply the effect.")]
		public readonly int StartIndex = 0;

		[Desc("End Index to apply the effect.")]
		public readonly int EndIndex = 32;

		public override object Create(ActorInitializer init) { return new ColorAlphaFlashPaletteEffect(this); }
	}

	public class ColorAlphaFlashPaletteEffect : ILoadsPlayerPalettes, IPaletteModifier, ITick
	{
		int t = 0;
		readonly ColorAlphaFlashPaletteEffectInfo info;
		readonly HashSet<string> palettes;

		public ColorAlphaFlashPaletteEffect(ColorAlphaFlashPaletteEffectInfo info)
		{
			this.info = info;
			palettes = new HashSet<string>();

			if (!info.IsAffectedPalettePlayerColor)
				palettes.Add(info.AffectedPalette);
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color playerColor, bool replaceExisting)
		{
			if (!info.IsAffectedPalettePlayerColor)
				return;

			palettes.Add(info.AffectedPalette + playerName);
		}

		void IPaletteModifier.AdjustPalette(IReadOnlyDictionary<string, MutablePalette> b)
		{
			foreach (var ap in palettes)
			{
				var p = b[ap];

				for (var j = 0; j < info.Alpha.Length; j++)
				{
					var k = (t + j) % 255 + 1;
					for (var l = k; l < 256; l += 32)
					{
						var color = p.GetColor(l);
						color = Color.FromArgb((int)(info.Alpha[j] * color.A), color.R, color.G, color.B);
						p.SetColor(l, color);
					}
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			t += 1;
			if (t >= info.EndIndex) t = info.StartIndex;
		}
	}
}
