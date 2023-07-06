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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Warheads
{
	[Desc("Spawn buildings, or if it can't fires a weapon, upon explosion.")]
	public class SpawnBuildingOrWeaponWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The cell range to try placing the buildings within.")]
		public readonly int Range = 10;

		[Desc("Actors to spawn.")]
		public readonly string[] Buildings = { };

		public readonly bool SkipMakeAnims = false;

		[Desc("Map player to give the actors to. Defaults to the firer.")]
		public readonly string Owner = null;

		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Defines the image of an optional animation played at the spawning location.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image))]
		[Desc("Defines the sequence of an optional animation played at the spawning location.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Defines the palette of an optional animation played at the spawning location.")]
		public readonly string Palette = "effect";

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] Sounds = new string[0];

		public readonly bool UsePlayerPalette = false;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");

			foreach (var b in Buildings)
			{
				var actorInfo = rules.Actors[b];
				var buildingInfo = actorInfo.TraitInfoOrDefault<BuildingInfo>();

				if (buildingInfo == null)
					throw new YamlException($"SpawnBuildingWarhead cannot be used to spawn nonbuilding actor '{b}'");
			}
		}

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var map = firedBy.World.Map;
			var targetCell = map.CellContaining(target.CenterPosition);

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var targetCells = map.FindTilesInCircle(targetCell, Range);
			var cell = targetCells.GetEnumerator();
			var alreadyusedcells = new HashSet<CPos>();

			foreach (var b in Buildings)
			{
				var placed = false;
				var actorInfo = firedBy.World.Map.Rules.Actors[b];
				var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

				var td = new TypeDictionary();
				if (Owner == null)
					td.Add(new OwnerInit(firedBy.Owner));
				else
					td.Add(new OwnerInit(firedBy.World.Players.First(p => p.InternalName == Owner)));

				while (cell.MoveNext() && !placed)
				{
					if (!buildingInfo.Tiles(cell.Current).Any(c => alreadyusedcells.Contains(c)) &&
						firedBy.World.CanPlaceBuilding(cell.Current, actorInfo, buildingInfo, null))
					{
						td.Add(new LocationInit(cell.Current));

						if (SkipMakeAnims)
							td.Add(new SkipMakeAnimsInit());

						alreadyusedcells.Concat(buildingInfo.Tiles(cell.Current));

						firedBy.World.AddFrameEndTask(w =>
							{
								var building = w.CreateActor(b, td);

								var palette = Palette;
								if (UsePlayerPalette)
									palette += building.Owner.InternalName;

								if (Image != null)
									w.Add(new SpriteEffect(building.CenterPosition, w, Image, Sequence, palette));

								var sound = Sounds.RandomOrDefault(firedBy.World.LocalRandom);
								if (sound != null)
									Game.Sound.Play(SoundType.World, sound, building.CenterPosition);
							});

						placed = true;
					}
				}

				if (!placed)
					weapon.Impact(target, args);
			}
		}
	}
}
