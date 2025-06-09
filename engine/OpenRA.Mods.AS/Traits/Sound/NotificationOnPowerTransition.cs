#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Sound
{
	[Desc("Play notifications when the player enters or exits low power.")]
	public class NotificationOnPowerTransitionInfo : TraitInfo
	{
		[NotificationReference("Speech")]
		public readonly string EnterLowPowerNotification = null;

		[NotificationReference("Speech")]
		public readonly string ExitLowPowerNotification = null;

		public override object Create(ActorInitializer init) { return new NotificationOnPowerTransition(this); }
	}

	public class NotificationOnPowerTransition : INotifyPowerLevelChanged, INotifyCreated
	{
		readonly NotificationOnPowerTransitionInfo info;
		PowerManager playerPower;
		bool wasLowPower;

		public NotificationOnPowerTransition(NotificationOnPowerTransitionInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			playerPower = playerActor.Trait<PowerManager>();
		}

		void INotifyPowerLevelChanged.PowerLevelChanged(Actor self)
		{
			var lowPower = playerPower.PowerState != PowerState.Normal;

			if (lowPower != wasLowPower)
			{
				if (lowPower)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.EnterLowPowerNotification, self.Owner.Faction.InternalName);
				else
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ExitLowPowerNotification, self.Owner.Faction.InternalName);
			}

			wasLowPower = lowPower;
		}
	}
}
