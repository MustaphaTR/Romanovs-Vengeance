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

using System;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Plays a notification clip when the trait is enabled.")]
	public class NotificationAnnouncementInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[NotificationReference("Speech")]
		[Desc("Speech notification to play.")]
		public readonly string Notification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display.")]
		public readonly string TextNotification = null;

		[Desc("Player relationships who can hear this notification.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Ping radar on the actor's location to affected players along with the notification.")]
		public readonly bool PingRadar = false;

		[Desc("Play the notification to the owning player even if Stance.Ally is not included in ValidStances.")]
		public readonly bool PlayToOwner = true;

		[Desc("Disable the announcement after it has been triggered.")]
		public readonly bool OneShot = false;

		public override object Create(ActorInitializer init) { return new NotificationAnnouncement(init.Self, this); }
	}

	public class NotificationAnnouncement : ConditionalTrait<NotificationAnnouncementInfo>
	{
		bool triggered;
		readonly Lazy<RadarPings> radarPings;

		public NotificationAnnouncement(Actor self, NotificationAnnouncementInfo info)
			: base(info)
		{
			radarPings = Exts.Lazy(() => self.World.WorldActor.Trait<RadarPings>());
		}

		protected override void TraitEnabled(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Info.OneShot && triggered)
				return;

			triggered = true;
			var player = self.World.LocalPlayer ?? self.World.RenderPlayer;
			if (player == null)
				return;

			if (Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(player)))
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", Info.Notification, player.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(player, Info.TextNotification);

				if (Info.PingRadar)
					radarPings.Value?.Add(() => true, self.CenterPosition, Color.Red, 50);
			}
			else if (Info.PlayToOwner && self.Owner == player)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", Info.Notification, player.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(player, Info.TextNotification);

				if (Info.PingRadar)
					radarPings.Value?.Add(() => true, self.CenterPosition, Color.Red, 50);
			}
		}
	}
}
