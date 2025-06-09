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
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Can be teleported via Chronoshift power.")]
	public class RA2ChronoshiftableInfo : ConditionalTraitInfo
	{
		[Desc("This trait only support this type of Chronoshift power, ",
			"and only trigger teleport effect of this teleport type")]
		public readonly string TeleportType = "RA2ChronoPower";

		[Desc("Types of damage that this trait causes when teleported to following terrain while unit cannot stand on it.")]
		public readonly Dictionary<HashSet<string>, BitSet<DamageType>> TerrainsAndDeathTypes = new();

		[Desc("Max distance when destination is unavaliable for allies")]
		public readonly int MaxSearchCellDistance = 5;

		public override object Create(ActorInitializer init) { return new RA2Chronoshiftable(this); }
	}

	public class RA2Chronoshiftable : ConditionalTrait<RA2ChronoshiftableInfo>
	{
		public RA2Chronoshiftable(RA2ChronoshiftableInfo info)
			: base(info) { }

		public virtual bool ChronoPowerTeleport(Actor self, CPos targetLocation, List<CPos> teleportCells, Actor chronoProvider)
		{
			if (IsTraitDisabled)
				return false;

			self.QueueActivity(
				false, new RA2Teleport(
					chronoProvider, Info.TeleportType, targetLocation, teleportCells, Info.MaxSearchCellDistance, -1, true, null, Info.TerrainsAndDeathTypes));

			return true;
		}
	}
}
