#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Spawns a random actor with the `EligibleForRandomActorCrate` trait when collected.")]
	class GiveRandomActorCrateActionInfo : CrateActionInfo
	{
		[Desc("Factions that are allowed to trigger this action.")]
		public readonly HashSet<string> ValidFactions = new();

		[Desc("Override the owner of the newly spawned unit: e.g. Creeps or Neutral")]
		public readonly string Owner = null;

		[Desc("Valid `EligibleForRandomActorCrate` types this crate can pick from.")]
		public readonly HashSet<string> Type = new() { "crateunit" };

		public override object Create(ActorInitializer init) { return new GiveRandomActorCrateAction(init.Self, this); }
	}

	class GiveRandomActorCrateAction : CrateAction
	{
		readonly Actor self;
		readonly GiveRandomActorCrateActionInfo info;

		readonly IEnumerable<ActorInfo> eligibleActors;

		IEnumerable<ActorInfo> validActors;

		public GiveRandomActorCrateAction(Actor self, GiveRandomActorCrateActionInfo info)
			: base(self, info)
		{
			this.self = self;
			this.info = info;

			eligibleActors = self.World.Map.Rules.Actors.Values.Where(a => a.HasTraitInfo<EligibleForRandomActorCrateInfo>()
				&& a.TraitInfos<EligibleForRandomActorCrateInfo>().Any(c => info.Type.Contains(c.Type)));
		}

		public bool CanGiveTo(Actor collector)
		{
			if (collector.Owner.NonCombatant)
				return false;

			if (info.ValidFactions.Count <= 0 && !info.ValidFactions.Contains(collector.Owner.Faction.InternalName))
				return false;

			var cells = collector.World.Map.FindTilesInCircle(self.Location, 2);

			validActors = eligibleActors.Where(a => ValidActor(a, cells));

			return validActors.Any();
		}

		bool ValidActor(ActorInfo a, IEnumerable<CPos> cells)
		{
			foreach (var c in cells)
			{
				var mi = a.TraitInfoOrDefault<MobileInfo>();
				if (mi != null && mi.CanEnterCell(self.World, self, c))
					return true;
			}

			return false;
		}

		public override int GetSelectionShares(Actor collector)
		{
			if (!CanGiveTo(collector))
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			var unit = validActors.Random(self.World.SharedRandom);

			var cells = collector.World.Map.FindTilesInCircle(self.Location, 2);

			foreach (var c in cells)
			{
				var mi = unit.TraitInfoOrDefault<MobileInfo>();
				if (mi != null && mi.CanEnterCell(self.World, self, c))
				{
					var cell = c;
					var td = new TypeDictionary
					{
						new LocationInit(cell),
						new OwnerInit(info.Owner ?? collector.Owner.InternalName)
					};

					collector.World.AddFrameEndTask(w => w.CreateActor(unit.Name, td));

					base.Activate(collector);

					return;
				}
			}
		}
	}
}
