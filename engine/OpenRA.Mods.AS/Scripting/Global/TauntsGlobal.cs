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

using OpenRA.Scripting;

namespace OpenRA.Mods.AS.Scripting
{
	[ScriptGlobal("Taunts")]
	public class TauntsGlobal : ScriptGlobal
	{
		readonly World world;

		public TauntsGlobal(ScriptContext context)
			: base(context)
		{
			world = context.World;
		}

		[Desc("Play a taunt listed in taunts.yaml")]
		public void PlayTauntNotification(Player player, string notification)
		{
			Game.Sound.PlayNotification(world.Map.Rules, world.LocalPlayer, "Taunts", notification, player?.Faction.InternalName);
		}
	}
}
