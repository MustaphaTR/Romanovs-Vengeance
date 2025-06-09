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

using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants horde types for the HordeBonus traits to work.")]
	public class GrantHordeBonusInfo : TraitInfo
	{
		public readonly string HordeType = "horde";

		public override object Create(ActorInitializer init) { return new GrantHordeBonus(this); }
	}

	public class GrantHordeBonus
	{
		public readonly GrantHordeBonusInfo Info;

		public GrantHordeBonus(GrantHordeBonusInfo info)
		{
			Info = info;
		}
	}
}
