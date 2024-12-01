﻿#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.AS.Traits;

/*
Works without base engine modification.
However, Mods.Common\Activities\Air\Land.cs is modified to support the air units to land "mid air!"
See landHeight private variable to track the changes.
*/

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This unit is \"slaved\" to a missile spawner old master.")]
	public class MissileSpawnerOldSlaveInfo : BaseSpawnerSlaveInfo
	{
		public override object Create(ActorInitializer init) { return new MissileSpawnerOldSlave(this); }
	}

	public class MissileSpawnerOldSlave : BaseSpawnerSlave
	{
		public MissileSpawnerOldSlave(MissileSpawnerOldSlaveInfo info)
			: base(info) { }
	}
}
