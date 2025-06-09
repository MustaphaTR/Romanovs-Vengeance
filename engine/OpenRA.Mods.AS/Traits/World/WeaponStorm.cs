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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Create a map-wide weapon storm.")]
	class WeaponStormInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Do not modify graphics that use any palette in this list.")]
		public readonly HashSet<string> ExcludePalettes = new() { "cursor", "chrome", "colorpicker", "fog", "shroud", "alpha" };

		[Desc("Do not modify graphics that start with these letters.")]
		public readonly HashSet<string> ExcludePalettePrefixes = new();

		public readonly float Red = 1f;
		public readonly float Green = 1f;
		public readonly float Blue = 1f;
		public readonly float Ambient = 1f;

		[Desc("How many weapons should be fired per 1000 map cells (on average).")]
		public readonly int[] Density = { 1 };

		public readonly WDist Altitude = WDist.Zero;

		[Desc("Should this storm be associated with an enemy (the Owner player)?")]
		public readonly bool Enemy = true;

		public readonly string Owner = "Creeps";

		public WeaponInfo WeaponInfo { get; private set; }

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weaponInfo))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weaponInfo;
		}

		public override object Create(ActorInitializer init) { return new WeaponStorm(this); }
	}

	class WeaponStorm : ConditionalTrait<WeaponStormInfo>, IPaletteModifier, ISync, ITick, IWorldLoaded
	{
		readonly WeaponStormInfo info;

		readonly uint ar, ag, ab;

		World world;
		int mapsize;

		public WeaponStorm(WeaponStormInfo info)
			: base(info)
		{
			this.info = info;

			// Calculate ambient color multipliers as integers for speed. To handle fractional ambiance, we'll increase
			// the magnitude of the result by 8 bits.
			ar = (uint)((1 << 8) * info.Ambient * info.Red);
			ag = (uint)((1 << 8) * info.Ambient * info.Green);
			ab = (uint)((1 << 8) * info.Ambient * info.Blue);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var density = info.Density.Length == 2
				? world.SharedRandom.Next(info.Density[0], info.Density[1])
				: info.Density[0];

			var weapons = mapsize * density / 1000;
			var firer = info.Enemy ? Array.Find(world.Players, x => x.PlayerName == info.Owner).PlayerActor : world.WorldActor;

			for (var i = 0; i < weapons; i++)
			{
				var tpos = world.Map.CenterOfCell(world.Map.ChooseRandomCell(world.SharedRandom))
					+ new WVec(WDist.Zero, WDist.Zero, info.Altitude);

				var args = new WarheadArgs
				{
					Weapon = info.WeaponInfo,
					Source = tpos,
					SourceActor = firer,
					WeaponTarget = Target.FromPos(tpos)
				};

				info.WeaponInfo.Impact(Target.FromPos(tpos), args);
			}
		}

		public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
		{
			if (IsTraitDisabled)
				return;

			foreach (var kvp in palettes)
			{
				if (info.ExcludePalettes.Contains(kvp.Key))
					continue;

				if (info.ExcludePalettePrefixes.Any(kvp.Key.StartsWith))
					continue;

				var palette = kvp.Value;

				for (var x = 0; x < Palette.Size; x++)
				{
					/* Here is the reference code for the operation we are performing.
					var from = palette.GetColor(x);
					var r = (int)(from.R * Ambient * Red).Clamp(0, 255);
					var g = (int)(from.G * Ambient * Green).Clamp(0, 255);
					var b = (int)(from.B * Ambient * Blue).Clamp(0, 255);
					palette.SetColor(x, Color.FromArgb(from.A, r, g, b));
					*/

					// PERF: Use integer arithmetic to avoid costly conversions to and from floating point values.
					var from = palette[x];

					// 1: Extract each color component and shift it to the lower bits, then multiply with ambiance.
					// 2: Because the ambiance was increased by 8 bits, our result has been shifted 8 bits up.
					// If the multiply overflowed we clamp the value, otherwise we mask out the fractional bits.
					// 3: Finally, we shift the color component back to its correct place. We're already 8 bits higher
					// than expected due to the multiply, so we don't have to shift as far to get back.
					var r1 = ((from & 0x00FF0000) >> 16) * ar;
					var r2 = r1 >= 0x0000FF00 ? 0x0000FF00 : r1 & 0x0000FF00;
					var r3 = r2 << 8;

					var g1 = ((from & 0x0000FF00) >> 8) * ag;
					var g2 = g1 >= 0x0000FF00 ? 0x0000FF00 : g1 & 0x0000FF00;
					var g3 = g2 << 0;

					var b1 = ((from & 0x000000FF) >> 0) * ab;
					var b2 = b1 >= 0x0000FF00 ? 0x0000FF00 : b1 & 0x0000FF00;
					var b3 = b2 >> 8;

					// Combine all the adjusted components back together.
					var a = from & 0xFF000000;
					palette[x] = a | r3 | g3 | b3;
				}
			}
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			mapsize = world.Map.MapSize.Width * world.Map.MapSize.Height;
		}
	}
}
