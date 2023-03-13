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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Plays a sound when the trait is enabled.")]
	public class SoundAnnouncementInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Sounds to play.")]
		public readonly string[] SoundFiles = null;

		[Desc("Disable the sound after it has been triggered.")]
		public readonly bool OneShot = false;

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the sounds played at.")]
		public readonly float Volume = 1f;

		public override object Create(ActorInitializer init) { return new SoundAnnouncement(this); }
	}

	public class SoundAnnouncement : ConditionalTrait<SoundAnnouncementInfo>
	{
		bool triggered;

		public SoundAnnouncement(SoundAnnouncementInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Info.OneShot && triggered)
				return;

			triggered = true;

			var sound = Info.SoundFiles.RandomOrDefault(Game.CosmeticRandom);
			var shouldStart = Info.AudibleThroughFog || (!self.World.ShroudObscures(self.CenterPosition) && !self.World.FogObscures(self.CenterPosition));
			if (self.OccupiesSpace != null)
				Game.Sound.Play(SoundType.World, sound, self.CenterPosition, shouldStart ? Info.Volume : 0f);
			else
				Game.Sound.Play(SoundType.World, sound, shouldStart ? Info.Volume : 0f);
		}
	}
}
