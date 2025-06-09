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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Plays an audio notification and shows a radar ping when a building is attacked.",
		"Attach this to the player actor.")]
	public class BaseAttackNotifierInfo : TraitInfo
	{
		[Desc("Minimum duration (in milliseconds) between notification events.")]
		public readonly int NotifyInterval = 30000;

		[Desc("Ping radar on the damaged actor's location.")]
		public readonly bool PingRadar = true;

		public readonly Color RadarPingColor = Color.Red;

		[Desc("Length of time (in ticks) to display a location ping in the minimap.")]
		public readonly int RadarPingDuration = 250;

		[NotificationReference("Speech")]
		[Desc("Speech notification type to play.")]
		public readonly string Notification = "BaseAttack";

		[FluentReference(optional: true)]
		[Desc("Text notification to display.")]
		public readonly string TextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play to allies when under attack.",
			"Won't play a notification to allies if this is null.")]
		public readonly string AllyNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display to allies when under attack.")]
		public readonly string AllyTextNotification = null;

		[Desc("Trigger the notification for non-buildings only.")]
		public readonly bool RevertUnitTypes = false;

		public override object Create(ActorInitializer init) { return new BaseAttackNotifier(init.Self, this); }
	}

	public class BaseAttackNotifier : INotifyDamage
	{
		readonly RadarPings radarPings;
		readonly BaseAttackNotifierInfo info;

		long lastAttackTime;

		public BaseAttackNotifier(Actor self, BaseAttackNotifierInfo info)
		{
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			this.info = info;
			lastAttackTime = -info.NotifyInterval;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			var localPlayer = self.World.LocalPlayer;

			if (localPlayer == null || localPlayer.Spectating)
				return;

			if (e.Attacker == null)
				return;

			if (e.Attacker.Owner == self.Owner)
				return;

			if (e.Attacker == self.World.WorldActor)
				return;

			if ((!info.RevertUnitTypes && !self.Info.HasTraitInfo<BuildingInfo>())
				|| (info.RevertUnitTypes && self.Info.HasTraitInfo<BuildingInfo>()))
				return;

			if (e.Attacker.Owner.IsAlliedWith(self.Owner) && e.Damage.Value <= 0)
				return;

			if (Game.RunTime > lastAttackTime + info.NotifyInterval)
			{
				var rules = self.World.Map.Rules;

				if (self.Owner == localPlayer)
				{
					if (!string.IsNullOrEmpty(info.Notification))
						Game.Sound.PlayNotification(rules, self.Owner, "Speech", info.Notification, self.Owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(self.Owner, info.TextNotification);
				}
				else if (localPlayer.IsAlliedWith(self.Owner) && localPlayer != e.Attacker.Owner)
				{
					if (!string.IsNullOrEmpty(info.AllyNotification))
						Game.Sound.PlayNotification(rules, localPlayer, "Speech", info.AllyNotification, localPlayer.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(localPlayer, info.AllyTextNotification);
				}

				if (info.PingRadar)
					radarPings?.Add(() => self.Owner.IsAlliedWith(self.World.RenderPlayer), self.CenterPosition, info.RadarPingColor, info.RadarPingDuration);

				lastAttackTime = Game.RunTime;
			}
		}
	}
}
