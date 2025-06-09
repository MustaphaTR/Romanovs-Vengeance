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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("A creative warhead in MW, made by CombinE88")]
	public class BackFireShrapnelWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			var sourcepos = target.CenterPosition;
			if (!IsValidImpact(sourcepos, firedBy))
				return;

			var shrapnelTarget = Target.FromActor(firedBy);
			if (shrapnelTarget.Type != TargetType.Invalid)
			{
				var facing = (shrapnelTarget.CenterPosition - sourcepos).Yaw;
				var pargs = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = facing,
					CurrentMuzzleFacing = () => facing,

					DamageModifiers = args.DamageModifiers,

					InaccuracyModifiers = Array.Empty<int>(),

					RangeModifiers = Array.Empty<int>(),

					Source = sourcepos,
					CurrentSource = () => sourcepos,
					SourceActor = firedBy,
					GuidedTarget = shrapnelTarget,
					PassiveTarget = shrapnelTarget.CenterPosition,
				};

				if (pargs.Weapon.Projectile != null)
				{
					var projectile = pargs.Weapon.Projectile.Create(pargs);
					if (projectile != null)
						firedBy.World.AddFrameEndTask(w => w.Add(projectile));

					if (pargs.Weapon.Report != null && pargs.Weapon.Report.Length > 0)
					{
						if (pargs.Weapon.AudibleThroughFog || (!firedBy.World.ShroudObscures(sourcepos) && !firedBy.World.FogObscures(sourcepos)))
							Game.Sound.Play(SoundType.World, pargs.Weapon.Report, firedBy.World, sourcepos, null, pargs.Weapon.SoundVolume);
					}
				}
			}
		}
	}
}
