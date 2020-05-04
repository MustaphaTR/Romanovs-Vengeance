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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Warheads
{
	[Desc("Warhead used to simulate the Red Alert 2 CellSpread damage delivery model.")]
	public class LegacySpreadWarhead : DamageWarhead
	{
		[Desc("Damage will be applied to actors in this area. A value of zero means only targeted actor will be damaged.")]
		public readonly WDist Spread = WDist.Zero;

		[Desc("The minimum percentage delivered at the edge of the spread.")]
		public readonly int PercentAtMax = 0;

		[Desc("In vanilia RA2, each cell of a structure were affected independently. Ares offered this control instead.")]
		public readonly int MaxAffect = int.MaxValue;

		public override void DoImpact(WPos pos, Actor firedBy, WarheadArgs args)
		{
			if (Spread == WDist.Zero)
				return;

			var debugVis = firedBy.World.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
				firedBy.World.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, new[] { WDist.Zero, Spread }, DebugOverlayColor);

			foreach (var victim in firedBy.World.FindActorsOnCircle(pos, Spread))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				var closestActiveShape = victim.TraitsImplementing<HitShape>()
						.Where(Exts.IsTraitEnabled)
						.Select(s => Pair.New(s, s.DistanceFromEdge(victim, pos)))
						.MinByOrDefault(s => s.Second);

				// Cannot be damaged without an active HitShape or if HitShape is outside Spread
				if (closestActiveShape.First == null || closestActiveShape.Second > Spread)
					continue;

				var building = victim.TraitOrDefault<Building>();

				var adjustedDamageModifiers = args.DamageModifiers.Append(DamageVersus(victim, closestActiveShape.First, args));

				if (MaxAffect > 0 && building != null)
				{
					var affectedcells = building.OccupiedCells().Select(x => (pos - firedBy.World.Map.CenterOfCell(x.First)).Length)
						.Where(x => x > Spread.Length).OrderBy(x => x).Take(MaxAffect);

					var delivereddamage = 0;

					foreach (var c in affectedcells)
						delivereddamage += Util.ApplyPercentageModifiers(Damage, adjustedDamageModifiers.Append(int2.Lerp(PercentAtMax, 100, c, Spread.Length)));

					victim.InflictDamage(firedBy, new Damage(delivereddamage, DamageTypes));
				}
				else
				{
					var damage = Util.ApplyPercentageModifiers(Damage,
						adjustedDamageModifiers.Append(int2.Lerp(PercentAtMax, 100, closestActiveShape.Second.Length, Spread.Length)));
					victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
				}
			}
		}
	}
}
