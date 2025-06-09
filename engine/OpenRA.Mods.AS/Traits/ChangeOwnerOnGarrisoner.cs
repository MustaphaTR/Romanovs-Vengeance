#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class ChangeOwnerOnGarrisonerInfo : TraitInfo, Requires<GarrisonableInfo>
	{
		[Desc("Speech notification played when the first actor enters this garrison.")]
		public readonly string EnterNotification = null;

		[Desc("Speech notification played when the last actor leaves this garrison.")]
		public readonly string ExitNotification = null;

		[Desc("List of sounds to be randomly played when the first actor enters this garrison.")]
		public readonly string[] EnterSounds = Array.Empty<string>();

		[Desc("List of sounds to be randomly played when the last actor exits this garrison.")]
		public readonly string[] ExitSounds = Array.Empty<string>();

		[Desc("Does the sound play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the EnterSounds and ExitSounds played at.")]
		public readonly float SoundVolume = 1;

		public override object Create(ActorInitializer init) { return new ChangeOwnerOnGarrisoner(init.Self, this); }
	}

	public class ChangeOwnerOnGarrisoner : INotifyGarrisonerEntered, INotifyGarrisonerExited, INotifyOwnerChanged
	{
		readonly ChangeOwnerOnGarrisonerInfo info;
		readonly Garrisonable garrison;

		Player originalOwner;
		bool garrisoning;

		public ChangeOwnerOnGarrisoner(Actor self, ChangeOwnerOnGarrisonerInfo info)
		{
			this.info = info;
			garrison = self.Trait<Garrisonable>();
			originalOwner = self.Owner;
		}

		void INotifyGarrisonerEntered.OnGarrisonerEntered(Actor self, Actor garrisoner)
		{
			var newOwner = garrisoner.Owner;
			if (self.Owner != originalOwner || self.Owner == newOwner || self.Owner.IsAlliedWith(garrisoner.Owner))
				return;

			garrisoning = true;
			self.ChangeOwner(newOwner);

			if (info.EnterSounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, info.EnterSounds, self.World, pos, null, info.SoundVolume);
			}

			Game.Sound.PlayNotification(self.World.Map.Rules, garrisoner.Owner, "Speech", info.EnterNotification, newOwner.Faction.InternalName);
			self.World.AddFrameEndTask(_ => garrisoning = false);
		}

		void INotifyGarrisonerExited.OnGarrisonerExited(Actor self, Actor garrisoner)
		{
			if (garrison.GarrisonerCount > 0)
				return;

			garrisoning = true;

			if (info.ExitSounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, info.ExitSounds, self.World, pos, null, info.SoundVolume);
			}

			Game.Sound.PlayNotification(self.World.Map.Rules, garrisoner.Owner, "Speech", info.ExitNotification, garrisoner.Owner.Faction.InternalName);
			self.ChangeOwner(originalOwner);
			self.World.AddFrameEndTask(_ => garrisoning = false);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!garrisoning)
				originalOwner = newOwner;
		}
	}
}
