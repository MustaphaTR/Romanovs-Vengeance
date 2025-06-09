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
	[Desc("Attach this to the player actor.")]
	public class TauntsInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the taunts checkbox in the lobby.")]
		public readonly string CheckboxLabel = "Taunts";

		[Desc("Tooltip description for the taunts checkbox in the lobby.")]
		public readonly string CheckboxDescription = "Enables /taunt command to play taunts to other players";

		[Desc("Default value of the taunts checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the taunts checkbox state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the taunts checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the taunts checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, "taunts", CheckboxLabel,
				CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new Taunts(this); }
	}

	public class Taunts : IResolveOrder, INotifyCreated
	{
		readonly TauntsInfo info;
		public bool Enabled { get; private set; }

		public Taunts(TauntsInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			Enabled = self.World.LobbyInfo.GlobalSettings.OptionOrDefault("taunts", info.CheckboxEnabled);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!Enabled)
				return;

			switch (order.OrderString)
			{
				case "Taunt":
				{
					if (self.World.LocalPlayer != null)
					{
						var rules = self.World.Map.Rules;
						if (rules.Notifications["taunts"].NotificationsPools.Value.ContainsKey(order.TargetString))
							Game.Sound.PlayNotification(rules, self.World.LocalPlayer, "Taunts", order.TargetString, self.Owner.Faction.InternalName);
						else
							TextNotificationsManager.Debug("{0} is not a valid taunt.", order.TargetString);
					}

					break;
				}
			}
		}
	}
}
