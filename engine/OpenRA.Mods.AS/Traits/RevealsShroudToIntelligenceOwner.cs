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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class RevealsShroudToIntelligenceOwnerInfo : RevealsShroudInfo
	{
		[FieldLoader.Require]
		[Desc("Types of intelligence this trait requires.")]
		public readonly HashSet<string> Types = new();

		public override object Create(ActorInitializer init) { return new RevealsShroudToIntelligenceOwner(this); }
	}

	public class RevealsShroudToIntelligenceOwner : RevealsShroud, INotifyAddedToWorld, INotifyMoving, INotifyCenterPositionChanged, ITick
	{
		public readonly RevealsShroudToIntelligenceOwnerInfo RSTIOInfo;
		public List<Player> IntelOwners = new();

		readonly Shroud.SourceType rstiotype;

		public RevealsShroudToIntelligenceOwner(RevealsShroudToIntelligenceOwnerInfo info)
			: base(info)
		{
			RSTIOInfo = info;
			rstiotype = info.RevealGeneratedShroud ? Shroud.SourceType.Visibility
				: Shroud.SourceType.PassiveVisibility;
		}

		protected override void AddCellsToPlayerShroud(Actor self, Player p, PPos[] uv)
		{
			p.Shroud.AddSource(this, rstiotype, uv);
		}

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld)
				return;

			if (self.Owner.NonCombatant)
				return;

			if (IntelOwners.Count == 0)
				return;

			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			var projectedLocation = self.World.Map.CellContaining(projectedPos);
			var pos = self.CenterPosition;

			var dirty = Info.MoveRecalculationThreshold.Length > 0 && (pos - cachedPos).LengthSquared > Info.MoveRecalculationThreshold.LengthSquared;
			if (!dirty && cachedLocation == projectedLocation)
				return;

			cachedLocation = projectedLocation;
			cachedPos = pos;

			UpdateIntelligenceShroudCells(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			if (self.Owner.NonCombatant)
				return;

			if (IntelOwners.Count == 0)
				return;

			var traitDisabled = IsTraitDisabled;
			var range = Range;

			if (cachedRange == range && traitDisabled == cachedTraitDisabled)
				return;

			cachedRange = range;
			cachedTraitDisabled = traitDisabled;

			UpdateIntelligenceShroudCells(self);
		}

		void UpdateIntelligenceShroudCells(Actor self)
		{
			var cells = ProjectedCells(self);
			foreach (var p in self.World.Players)
			{
				RemoveCellsFromPlayerShroud(self, p);
				if (IntelOwners.Contains(p))
					AddCellsToPlayerShroud(self, p, cells);
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (!self.IsInWorld)
				return;

			if (self.Owner.NonCombatant)
				return;

			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			cachedLocation = self.World.Map.CellContaining(projectedPos);
			cachedPos = centerPosition;
			cachedTraitDisabled = IsTraitDisabled;
			var cells = ProjectedCells(self);

			foreach (var p in self.World.Players)
			{
				var hasIntel = self.World.ActorsWithTrait<GivesIntelligence>()
					.Any(t => t.Actor.Owner == p && t.Trait.Info.Types.Overlaps(RSTIOInfo.Types) && !t.Trait.IsTraitDisabled);

				if (hasIntel)
				{
					RemoveCellsFromPlayerShroud(self, p);
					AddCellsToPlayerShroud(self, p, cells);

					if (!IntelOwners.Contains(p))
						IntelOwners.Add(p);
				}
			}
		}

		void INotifyMoving.MovementTypeChanged(Actor self, MovementType type)
		{
			if (self.Owner.NonCombatant)
				return;

			if (IntelOwners.Count == 0)
				return;

			// Recalculate the visiblity at our final stop position
			if (type == MovementType.None && self.IsInWorld)
			{
				var centerPosition = self.CenterPosition;
				var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
				var projectedLocation = self.World.Map.CellContaining(projectedPos);
				var pos = self.CenterPosition;

				cachedLocation = projectedLocation;
				cachedPos = pos;

				UpdateIntelligenceShroudCells(self);
			}
		}

		public void AddCellsToIntelligenceOwnerShroud(Actor self, Player p, PPos[] uv)
		{
			AddCellsToPlayerShroud(self, p, uv);
		}

		public void RemoveCellsFromIntelligenceOwnerShroud(Actor self, Player p)
		{
			RemoveCellsFromPlayerShroud(self, p);
		}

		public PPos[] GetIntelligenceProjectedCells(Actor self)
		{
			return ProjectedCells(self);
		}
	}
}
