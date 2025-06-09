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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Spawn buildings upon explosion.")]
	public class SpawnBuildingWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The cell range to try placing the buildings within.")]
		public readonly int Range = 10;

		[Desc("Actors to spawn.")]
		public readonly string[] Buildings = Array.Empty<string>();

		[Desc("Should this building link to the actor who create them?")]
		public readonly bool LinkToParent = false;

		public readonly bool SkipMakeAnims = false;

		[Desc("Owner of the spawned actor. Allowed keywords:" +
			"'Attacker' and 'InternalName'.")]
		public readonly ASOwnerType OwnerType = ASOwnerType.Attacker;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		[Desc("Defines the image of an optional animation played at the spawning location.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Defines the sequence of an optional animation played at the spawning location.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Defines the palette of an optional animation played at the spawning location.")]
		public readonly string Palette = "effect";

		[Desc("List of sounds that can be played at the spawning location.")]
		public readonly string[] Sounds = Array.Empty<string>();

		public readonly bool UsePlayerPalette = false;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
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
				var actorInfo = firedBy.World.Map.Rules.Actors[b];
				var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();

				var td = new TypeDictionary();
				if (OwnerType == ASOwnerType.Attacker)
					td.Add(new OwnerInit(firedBy.Owner));
				else
					td.Add(new OwnerInit(Array.Find(firedBy.World.Players, p => p.InternalName == InternalOwner)));

				if (LinkToParent)
					td.Add(new ParentActorInit(firedBy));

				while (cell.MoveNext())
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

						break;
					}
				}
			}
		}
	}
}
