#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public class WithCargoSoundsInfo : ConditionalTraitInfo, Requires<CargoInfo>
	{
		[Desc("Speech notification played when the first actor enters this garrison.")]
		public readonly string EnterNotification = null;

		[Desc("Speech notification played when the last actor leaves this garrison.")]
		public readonly string ExitNotification = null;

		[Desc("Sound played when the first actor enters this garrison.")]
		public readonly string EnterSound = null;

		[Desc("Sound played when the last actor exits this garrison.")]
		public readonly string ExitSound = null;

		[Desc("Does the sound play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the EnterSound and ExitSound played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new WithCargoSounds(init.Self, this); }
	}

	public class WithCargoSounds : ConditionalTrait<WithCargoSoundsInfo>, INotifyPassengerEntered, INotifyPassengerExited
	{
		readonly Cargo cargo;

		public WithCargoSounds(Actor self, WithCargoSoundsInfo info)
            : base(info)
		{
			cargo = self.Trait<Cargo>();
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			var pos = self.CenterPosition;
			if (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
				Game.Sound.Play(SoundType.World, Info.EnterSound, self.CenterPosition, Info.SoundVolume);

			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", Info.EnterNotification, passenger.Owner.Faction.InternalName);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			var pos = self.CenterPosition;
			if (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
				Game.Sound.Play(SoundType.World, Info.ExitSound, self.CenterPosition, Info.SoundVolume);

			Game.Sound.PlayNotification(self.World.Map.Rules, passenger.Owner, "Speech", Info.ExitNotification, passenger.Owner.Faction.InternalName);
		}
	}
}
