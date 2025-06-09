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
using OpenRA.Mods.Common.Commands;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class DevOffsetOverlayManagerInfo : TraitInfo
	{
		[Desc("The font used to draw cell vectors. Should match the value as-is in the Fonts section of the mod manifest (do not convert to lowercase).")]
		public readonly string Font = "TinyBold";

		public override object Create(ActorInitializer init) { return new DevOffsetOverlayManager(init.Self); }
	}

	public class DevOffsetOverlayManager : IWorldLoaded, IChatCommand
	{
		const string CommandName = "dev-offset";
		const string CommandHelp = "Commands the DevOffsetOverlay trait. See the trait documentation for controls.";

		readonly Actor self;

		public DevOffsetOverlayManager(Actor self)
		{
			this.self = self;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = self.TraitOrDefault<ChatCommands>();
			var help = self.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandHelp);
		}

		void IChatCommand.InvokeCommand(string command, string arg)
		{
			if (command != CommandName)
				return;

			foreach (var actor in self.World.Selection.Actors)
			{
				if (actor.IsDead)
					continue;

				var devOffset = actor.TraitOrDefault<DevOffsetOverlay>();
				if (devOffset == null)
					continue;

				devOffset.ParseCommand(actor, arg);
			}
		}
	}
}
