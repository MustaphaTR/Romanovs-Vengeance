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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Allows to fire a weapon to a directly specified target position relative to the warhead explosion.")]
	public class FireFragmentWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Percentual chance the fragment is fired.")]
		public readonly int Chance = 100;

		[Desc("Target offsets relative to warhead explosion.")]
		public readonly WVec[] Offsets = { new(0, 0, 0) };

		[Desc("If set, Offset's Z value will be used as absolute height instead of explosion height.")]
		public readonly bool UseZOffsetAsAbsoluteHeight = false;

		[Desc("Should the weapons be fired around the intended target or at the explosion's epicenter.")]
		public readonly bool AroundTarget = false;

		[Desc("Rotate the fragment weapon based on the impact orientation.")]
		public readonly bool Rotate = false;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var world = firedBy.World;
			var map = world.Map;

			if (Chance < world.SharedRandom.Next(100))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var epicenter = AroundTarget && args.WeaponTarget.Type != TargetType.Invalid
				? args.WeaponTarget.CenterPosition
				: target.CenterPosition;

			foreach (var offset in Offsets)
			{
				var targetVector = offset;

				if (Rotate && args.ImpactOrientation != WRot.None)
					targetVector = targetVector.Rotate(args.ImpactOrientation);

				var fragmentTargetPosition = epicenter + targetVector;

				if (UseZOffsetAsAbsoluteHeight)
				{
					fragmentTargetPosition = new WPos(fragmentTargetPosition.X, fragmentTargetPosition.Y,
						world.Map.CenterOfCell(world.Map.CellContaining(fragmentTargetPosition)).Z + offset.Z);
				}

				var fragmentTarget = Target.FromPos(fragmentTargetPosition);
				var fragmentFacing = (fragmentTargetPosition - target.CenterPosition).Yaw;

				// Lambdas can't use 'in' variables, so capture a copy for later
				var centerPosition = target.CenterPosition;

				var projectileArgs = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = fragmentFacing,
					CurrentMuzzleFacing = () => fragmentFacing,

					DamageModifiers = args.DamageModifiers,

					InaccuracyModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray() : Array.Empty<int>(),

					RangeModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray() : Array.Empty<int>(),

					Source = target.CenterPosition,
					CurrentSource = () => centerPosition,
					SourceActor = firedBy,
					GuidedTarget = fragmentTarget,
					PassiveTarget = fragmentTargetPosition
				};

				if (projectileArgs.Weapon.Projectile != null)
				{
					var projectile = projectileArgs.Weapon.Projectile.Create(projectileArgs);
					if (projectile != null)
						firedBy.World.AddFrameEndTask(w => w.Add(projectile));

					if (projectileArgs.Weapon.Report != null && projectileArgs.Weapon.Report.Length > 0)
					{
						var pos = target.CenterPosition;
						if (projectileArgs.Weapon.AudibleThroughFog || (!firedBy.World.ShroudObscures(pos) && !firedBy.World.FogObscures(pos)))
							Game.Sound.Play(SoundType.World, projectileArgs.Weapon.Report, firedBy.World, pos, null, projectileArgs.Weapon.SoundVolume);
					}
				}
			}
		}
	}
}
