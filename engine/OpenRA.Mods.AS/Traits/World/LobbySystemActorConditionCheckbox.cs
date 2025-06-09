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
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enables a condition on the world or player actors if the checkbox is enabled.")]
	public class LobbySystemActorConditionCheckboxInfo : TraitInfo, ILobbyOptions
	{
		[FieldLoader.Require]
		[Desc("Internal id for this checkbox.")]
		public readonly string ID = null;

		[FieldLoader.Require]
		[FluentReference]
		[Desc("Display name for this checkbox.")]
		public readonly string Label = null;

		[FluentReference]
		[Desc("Description name for this checkbox.")]
		public readonly string Description = null;

		[Desc("Default value of the checkbox in the lobby.")]
		public readonly bool Enabled = false;

		[Desc("Prevent the checkbox from being changed from its default value.")]
		public readonly bool Locked = false;

		[Desc("Display the checkbox in the lobby.")]
		public readonly bool Visible = true;

		[Desc("Display order for the checkbox in the lobby.")]
		public readonly int DisplayOrder = 0;

		[Desc("System actors to grant condition to. Only supports: World, Player")]
		public readonly SystemActors Actors = SystemActors.World;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant when this checkbox is enabled.")]
		public readonly string Condition = "";

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, ID, Label, Description,
				Visible, DisplayOrder, Enabled, Locked);
		}

		public override object Create(ActorInitializer init) { return new LobbySystemActorConditionCheckbox(this); }
	}

	public class LobbySystemActorConditionCheckbox : INotifyCreated, ITick
	{
		readonly LobbySystemActorConditionCheckboxInfo info;
		bool grantToPlayer;

		public LobbySystemActorConditionCheckbox(LobbySystemActorConditionCheckboxInfo info)
		{
			this.info = info;
			grantToPlayer = info.Actors.HasFlag(SystemActors.Player);
		}

		void INotifyCreated.Created(Actor self)
		{
			var enabled = self.World.LobbyInfo.GlobalSettings.OptionOrDefault(info.ID, info.Enabled);

			if (info.Actors.HasFlag(SystemActors.World) && enabled)
				self.GrantCondition(info.Condition);

			grantToPlayer &= enabled;
		}

		void ITick.Tick(Actor self)
		{
			// World actor is created before Player actors, so this doesn't work in Created.
			if (grantToPlayer)
			{
				foreach (var player in self.World.Players)
					player.PlayerActor.GrantCondition(info.Condition);

				grantToPlayer = false;
			}
		}
	}
}
