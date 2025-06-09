#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Triggers a weapon explosion on all appropriate actors on the map.",
		"Note that this warhead is a HUGE HACK. Use it on your own risk!")]
	public class GlobalExplodeWeaponWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
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
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var allowedActors = firedBy.World.Actors.Where(a => a.IsInWorld && !a.IsDead && IsValidAgainst(a, firedBy));

			foreach (var actor in allowedActors)
			{
				weapon.Impact(Target.FromActor(actor), args);

				if (weapon.Report != null && weapon.Report.Length > 0)
				{
					var pos = actor.CenterPosition;
					if (weapon.AudibleThroughFog || (!firedBy.World.ShroudObscures(pos) && !firedBy.World.FogObscures(pos)))
						Game.Sound.Play(SoundType.World, weapon.Report.Random(firedBy.World.SharedRandom), pos, weapon.SoundVolume);
				}
			}
		}
	}
}
