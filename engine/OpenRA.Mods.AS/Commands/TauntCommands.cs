#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Commands;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Commands
{
	[Desc("Allows the player to play taunts via the chatbox. Attach this to the world actor.")]
	public class TauntCommandsInfo : TraitInfo<TauntCommands> { }

	public class TauntCommands : IChatCommand, IWorldLoaded
	{
		World world;
		Taunts taunts;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			void Register(string name, string helpText)
			{
				console.RegisterCommand(name, this);
				help.RegisterHelp(name, helpText);
			}

			if (world.LocalPlayer != null)
			{
				taunts = world.LocalPlayer.PlayerActor.TraitOrDefault<Taunts>();
				if (taunts != null)
					Register("taunt", "plays a taunt");
			}
		}

		public void InvokeCommand(string name, string arg)
		{
			switch (name)
			{
				case "taunt":
					if (!taunts.Enabled)
					{
						TextNotificationsManager.Debug("Taunts are disabled.");
						return;
					}

					if (world.LocalPlayer != null)
						world.IssueOrder(new Order("Taunt", world.LocalPlayer.PlayerActor, false) { TargetString = arg });

					break;
			}
		}
	}
}
