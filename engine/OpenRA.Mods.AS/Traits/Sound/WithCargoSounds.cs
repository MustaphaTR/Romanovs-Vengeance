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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class WithCargoSoundsInfo : ConditionalTraitInfo
	{
		[Desc("Speech notification played when an actor enters this cargo.")]
		public readonly string EnterNotification = null;

		[Desc("Speech notification played when an actor leaves this cargo.")]
		public readonly string ExitNotification = null;

		[Desc("List of sounds to be randomly played when an actor enters this cargo.")]
		public readonly string[] EnterSounds = Array.Empty<string>();

		[Desc("List of sounds to be randomly played when an actor exits this cargo.")]
		public readonly string[] ExitSounds = Array.Empty<string>();

		[Desc("Does the sound play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the EnterSounds and ExitSounds played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new WithCargoSounds(init.Self, this); }
	}

	public class WithCargoSounds : ConditionalTrait<WithCargoSoundsInfo>, INotifyPassengerEntered, INotifyPassengerExited
	{
		public WithCargoSounds(Actor self, WithCargoSoundsInfo info)
			: base(info) { }

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			if (Info.EnterSounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, Info.EnterSounds, self.World, pos, null, Info.SoundVolume);
			}

			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", Info.EnterNotification, passenger.Owner.Faction.InternalName);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			if (Info.ExitSounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, Info.ExitSounds, self.World, pos, null, Info.SoundVolume);
			}

			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", Info.ExitNotification, passenger.Owner.Faction.InternalName);
		}
	}
}
