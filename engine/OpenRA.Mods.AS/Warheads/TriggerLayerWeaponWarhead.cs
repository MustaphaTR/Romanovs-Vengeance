#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Works like Inferno Canon like in CnC Generals:, used by TA.")]
	public sealed class TriggerLayerWeaponWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Range between falloff steps in cells.")]
		public readonly WDist Spread = new(1024);

		[Desc("Level percentage at each range step.")]
		public readonly int[] Falloff = { 100, 75, 50 };

		[Desc("The name of the layer we want to increase the level of.")]
		public readonly string LayerName = "";

		[Desc("Ranges at which each Falloff step is defined (in cells). Overrides Spread.")]
		public WDist[] Range = null;

		[Desc("Level this weapon puts on the ground. Accumulates over previously trigger area.")]
		public int Level = 200;

		[Desc("It saturates at this level, by this weapon.")]
		public int MaxLevel = 600;

		[Desc("Allow triggering effects when the impacted cell has the value in [TriggerAtLevelMax, TriggerAtLevelMin]")]
		public bool AllowTriggerLevel = true;

		[Desc("Allows a triggering effect: cells (affected by Falloff and Range) set to a specific level defined by TriggerSetLevel")]
		public bool AllowSetLevelWhenTrigger = true;

		[Desc("Allows a triggering effect: impacted cell explode a weapon")]
		public bool AllowTriggerWeaponWhenTrigger = true;

		[Desc("Impacted cell has the value in [TriggerAtLevelMax, TriggerAtLevelMin] to trigger effect. Requires \"AllowTriggerLevel = true\".")]
		public int TriggerAtLevelMax = int.MaxValue;

		[Desc("Impacted cell has the value in [TriggerAtLevelMax, TriggerAtLevelMin] to trigger effect.  Requires \"AllowTriggerLevel = true\".")]
		public int TriggerAtLevelMin = int.MinValue;

		[Desc("Cells (affected by Falloff and Range) set to this level when trigger. Requires \"AllowTriggerLevel = true\" and \"AllowSetLevelWhenTrigger = true\"")]
		public int TriggerSetLevel = 0;

		[WeaponReference]
		[Desc("Impacted cell explode a weapon when trigger. Has to be defined in weapons.yaml as well.")]
		public readonly string TriggerWeapon = null;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range == null)
				Range = Exts.MakeArray(Falloff.Length, i => i * Spread);
			else
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");
			}

			if (AllowTriggerLevel && AllowTriggerWeaponWhenTrigger && !rules.Weapons.TryGetValue(TriggerWeapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{TriggerWeapon.ToLowerInvariant()}'");
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			var world = firedBy.World;

			if (world.LocalPlayer != null)
			{
				var devMode = world.LocalPlayer.PlayerActor.TraitOrDefault<DebugVisualizations>();
				if (devMode != null && devMode.CombatGeometry)
				{
					var rng = Exts.MakeArray(Range.Length, i => WDist.FromCells(Range[i].Length));
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(target.CenterPosition, rng, Primitives.Color.Gold);
				}
			}

			var targetTile = world.Map.CellContaining(target.CenterPosition);
			var raLayer = world.WorldActor.TraitsImplementing<WeaponTriggerCells>()
				.First(l => l.Info.Name == LayerName);

			var triggeredSetLevel = false;
			if (AllowTriggerLevel &&
				raLayer.GetLevel(targetTile) >= TriggerAtLevelMin &&
				raLayer.GetLevel(targetTile) <= TriggerAtLevelMax)
			{
				if (AllowTriggerWeaponWhenTrigger)
					weapon.Impact(Target.FromPos(target.CenterPosition), firedBy);

				var affectedCells = world.Map.FindTilesInCircle(targetTile, (int)Math.Ceiling((decimal)Range[^1].Length / 1024));
				if (AllowSetLevelWhenTrigger)
				{
					triggeredSetLevel = true;
					foreach (var cell in affectedCells)
						raLayer.SetLevel(cell, TriggerSetLevel);
				}
			}

			if (!triggeredSetLevel && Level != 0)
			{
				var affectedCells = world.Map.FindTilesInCircle(targetTile, (int)Math.Ceiling((decimal)Range[^1].Length / 1024));
				foreach (var cell in affectedCells)
				{
					var mul = GetIntensityFalloff((target.CenterPosition - world.Map.CenterOfCell(cell)).Length);
					raLayer.IncreaseLevel(cell, Level * mul / 100, MaxLevel);
				}
			}
		}

		int GetIntensityFalloff(int distance)
		{
			var inner = Range[0].Length;
			for (var i = 1; i < Range.Length; i++)
			{
				var outer = Range[i].Length;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
