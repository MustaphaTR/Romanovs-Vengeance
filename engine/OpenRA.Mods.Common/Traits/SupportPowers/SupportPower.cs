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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class SupportPowerInfo : PausableConditionalTraitInfo
	{
		[Desc("Measured in ticks.")]
		public readonly int ChargeInterval = 0;

		public readonly string IconImage = "icon";

		// [SequenceReference(nameof(IconImage))]
		[Desc("Icon sprite displayed in the support power palette.")]
		public readonly Dictionary<int, string> Icons = new();

		[PaletteReference]
		[Desc("Palette used for the icon.")]
		public readonly string IconPalette = "chrome";

		[FluentReference(optional: true)]
		public readonly Dictionary<int, string> Names = new();

		[Desc("An optional list of generic names (i.e. \"Ability\" or \"Superpower\")" +
			"to be shown to chosen players.")]
		[FluentReference(dictionaryReference: LintDictionaryReference.Values)]
		public readonly Dictionary<int, string> GenericNames = new();

		[Desc("Player stances that the generic names should be shown to.")]
		public readonly PlayerRelationship GenericVisibility = PlayerRelationship.None;

		[FluentReference(dictionaryReference: LintDictionaryReference.Values)]
		public readonly Dictionary<int, string> Descriptions = new();

		[Desc("Allow multiple instances of the same support power.")]
		public readonly bool AllowMultiple = false;

		[Desc("Allow this to be used only once.")]
		public readonly bool OneShot = false;
		public readonly int Cost = 0;

		[CursorReference]
		[Desc("Cursor to display for using this support power.")]
		public readonly string Cursor = "ability";

		[CursorReference]
		[Desc("Cursor when unable to activate on this position. ")]
		public readonly string BlockedCursor = "generic-blocked";

		[Desc("If set to true, the support power will be fully charged when it becomes available. " +
			"Normal rules apply for subsequent charges.")]
		public readonly bool StartFullyCharged = false;

		[Desc("If set to true, the support power will be fully charged when the first time player obtain it. " +
			"Overrided by `StartFullyCharged`." +
			"Note: it depends on `OrderName` and the first name in `Names` to indentify different support power." +
			"Normal rules apply for subsequent charges.")]
		public readonly bool StartFullyChargedForTheFirstTime = false;

		public readonly Dictionary<int, string[]> Prerequisites = [];

		public readonly string DetectedSound = null;

		[NotificationReference("Speech")]
		public readonly string DetectedSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string DetectedTextNotification = null;

		public readonly string BeginChargeSound = null;

		[NotificationReference("Speech")]
		public readonly string BeginChargeSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string BeginChargeTextNotification = null;

		public readonly string EndChargeSound = null;

		[NotificationReference("Speech")]
		public readonly string EndChargeSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string EndChargeTextNotification = null;

		public readonly string SelectTargetSound = null;

		[NotificationReference("Speech")]
		public readonly string SelectTargetSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string SelectTargetTextNotification = null;

		public readonly string InsufficientPowerSound = null;

		[NotificationReference("Speech")]
		public readonly string InsufficientPowerSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string InsufficientPowerTextNotification = null;

		public readonly string LaunchSound = null;

		[NotificationReference("Speech")]
		public readonly string LaunchSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string LaunchTextNotification = null;

		public readonly string IncomingSound = null;

		[NotificationReference("Speech")]
		public readonly string IncomingSpeechNotification = null;

		[FluentReference(optional: true)]
		public readonly string IncomingTextNotification = null;

		[Desc("Defines to which players the timer is shown.")]
		public readonly PlayerRelationship DisplayTimerRelationships = PlayerRelationship.None;

		[Desc("Beacons are only supported on the Airstrike, Paratroopers, and Nuke powers")]
		public readonly bool DisplayBeacon = false;

		public readonly bool BeaconPaletteIsPlayerPalette = true;

		[PaletteReference(nameof(BeaconPaletteIsPlayerPalette))]
		public readonly string BeaconPalette = "player";

		public readonly string BeaconImage = "beacon";

		// [SequenceReference(nameof(BeaconImage))]
		public readonly Dictionary<int, string> BeaconPosters = new();

		[PaletteReference]
		public readonly string BeaconPosterPalette = "chrome";

		[SequenceReference(nameof(BeaconImage))]
		public readonly string ClockSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string BeaconSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string ArrowSequence = null;

		[SequenceReference(nameof(BeaconImage))]
		public readonly string CircleSequence = null;

		[Desc("Delay after launch, measured in ticks.")]
		public readonly int BeaconDelay = 0;

		public readonly bool DisplayRadarPing = false;

		[Desc("Measured in ticks.")]
		public readonly int RadarPingDuration = 125;

		public readonly string OrderName;

		[Desc("Sort order for the support power palette. Smaller numbers are presented earlier.")]
		public readonly int SupportPowerPaletteOrder = 9999;

		public string NameForPlayerStance(PlayerRelationship relationship, int level)
		{
			if (relationship == PlayerRelationship.None || !GenericVisibility.HasRelationship(relationship))
				return FluentProvider.GetMessage(Names.First(gn => gn.Key == level).Value);

			var genericName = GenericNames.First(gn => gn.Key == level).Value;
			return string.IsNullOrEmpty(genericName) ? "" : FluentProvider.GetMessage(genericName);
		}

		protected SupportPowerInfo() { OrderName = GetType().Name + "Order"; }
	}

	public abstract class SupportPower : PausableConditionalTrait<SupportPowerInfo>, INotifyOwnerChanged
	{
		public readonly Actor Self;
		readonly SupportPowerInfo info;
		protected RadarPing ping;

		DeveloperMode developerMode;
		TechTree techTree;

		protected SupportPower(Actor self, SupportPowerInfo info)
			: base(info)
		{
			Self = self;
			this.info = info;

			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;

			techTree = playerActor.Trait<TechTree>();
			developerMode = playerActor.Trait<DeveloperMode>();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();
			developerMode = newOwner.PlayerActor.Trait<DeveloperMode>();
		}

		public int GetLevel()
		{
			var availables = Info.Prerequisites.Where(p => techTree.HasPrerequisites(p.Value));
			var level = availables.Any() ? availables.Max(p => p.Key) : 0;

			return developerMode.AllTech ? Info.Prerequisites.Max(p => p.Key) : level;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			var player = self.World.LocalPlayer;
			if (player != null && player != self.Owner)
			{
				Game.Sound.Play(SoundType.UI, Info.DetectedSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, player, "Speech", info.DetectedSpeechNotification, player.Faction.InternalName);
				TextNotificationsManager.AddTransientLine(player, info.DetectedTextNotification);
			}
		}

		public virtual SupportPowerInstance CreateInstance(string key, SupportPowerManager manager)
		{
			return new SupportPowerInstance(key, info, manager);
		}

		public virtual void Charging(Actor self, string key)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, self.Owner, Info.BeginChargeSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.BeginChargeSpeechNotification, self.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(self.Owner, Info.BeginChargeTextNotification);
		}

		public virtual void Charged(Actor self, string key)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, self.Owner, Info.EndChargeSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.EndChargeSpeechNotification, self.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(self.Owner, Info.EndChargeTextNotification);

			foreach (var notify in self.TraitsImplementing<INotifySupportPower>())
				notify.Charged(self);
		}

		public virtual void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectGenericPowerTarget(order, manager, info, MouseButton.Left);
		}

		public virtual void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			if (Info.DisplayRadarPing && manager.RadarPings != null)
			{
				ping = manager.RadarPings.Value.Add(
					() => order.Player.IsAlliedWith(self.World.RenderPlayer),
					order.Target.CenterPosition,
					order.Player.Color,
					Info.RadarPingDuration);
			}

			foreach (var notify in self.TraitsImplementing<INotifySupportPower>())
				notify.Activated(self);
		}

		public virtual void PlayLaunchSounds()
		{
			var localPlayer = Self.World.LocalPlayer;
			if (localPlayer == null || localPlayer.Spectating)
				return;

			var isAllied = Self.Owner.IsAlliedWith(localPlayer);
			Game.Sound.Play(SoundType.UI, isAllied ? Info.LaunchSound : Info.IncomingSound);

			var speech = isAllied ? Info.LaunchSpeechNotification : Info.IncomingSpeechNotification;
			Game.Sound.PlayNotification(Self.World.Map.Rules, localPlayer, "Speech", speech, localPlayer.Faction.InternalName);

			var text = isAllied ? Info.LaunchTextNotification : Info.IncomingTextNotification;
			TextNotificationsManager.AddTransientLine(localPlayer, text);
		}

		public IEnumerable<CPos> CellsMatching(CPos location, char[] footprint, CVec dimensions)
		{
			var index = 0;
			var x = location.X - (dimensions.X - 1) / 2;
			var y = location.Y - (dimensions.Y - 1) / 2;
			for (var j = 0; j < dimensions.Y; j++)
				for (var i = 0; i < dimensions.X; i++)
					if (footprint[index++] == 'x')
						yield return new CPos(x + i, y + j);
		}
	}
}
