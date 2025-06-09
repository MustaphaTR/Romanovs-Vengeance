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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Sound
{
	[Desc("Plays a looping audio file at the actor position. Attach this to the `World` actor to cover the whole map.")]
	sealed class AmbientSoundInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string[] SoundFiles = null;

		[Desc("Initial delay (in ticks) before playing the sound for the first time.",
			"Two values indicate a random delay range.")]
		public readonly int[] Delay = [0];

		[Desc("Interval between playing the sound (in ticks).",
			"Two values indicate a random delay range.")]
		public readonly int[] Interval = [0];

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the sounds played at.")]
		public readonly float Volume = 1f;

		public override object Create(ActorInitializer init) { return new AmbientSound(init.Self, this); }
	}

	sealed class AmbientSound : ConditionalTrait<AmbientSoundInfo>, ITick, INotifyRemovedFromWorld
	{
		readonly bool loop;
		readonly HashSet<ISound> currentSounds = [];
		WPos cachedPosition;
		int delay;

		public AmbientSound(Actor self, AmbientSoundInfo info)
			: base(info)
		{
			delay = Util.RandomInRange(self.World.SharedRandom, info.Delay);
			loop = Info.Interval.Length == 0 || (Info.Interval.Length == 1 && Info.Interval[0] == 0);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			currentSounds.RemoveWhere(s => s == null || s.Complete);

			if (self.OccupiesSpace != null)
			{
				var pos = self.CenterPosition;
				if (pos != cachedPosition)
				{
					foreach (var s in currentSounds)
						s.SetPosition(pos);

					cachedPosition = pos;
				}
			}

			foreach (var s in currentSounds)
			{
				if (!Info.AudibleThroughFog)
					if (self.World.ShroudObscures(cachedPosition) || self.World.FogObscures(cachedPosition))
						s.Volume = 0f;
					else
						s.Volume = Info.Volume * Game.Settings.Sound.SoundVolume;
			}

			if (delay < 0)
				return;

			if (--delay < 0)
			{
				StartSound(self);
				if (!loop)
					delay = Util.RandomInRange(self.World.SharedRandom, Info.Interval);
			}
		}

		void StartSound(Actor self)
		{
			var sound = Info.SoundFiles.RandomOrDefault(Game.CosmeticRandom);

			ISound s;
			var shouldStart = Info.AudibleThroughFog || (!self.World.ShroudObscures(cachedPosition) && !self.World.FogObscures(cachedPosition));
			if (self.OccupiesSpace != null)
			{
				cachedPosition = self.CenterPosition;
				s = loop ? Game.Sound.PlayLooped(SoundType.World, sound, cachedPosition, shouldStart ? Info.Volume : 0f) :
					Game.Sound.Play(SoundType.World, sound, self.CenterPosition, shouldStart ? Info.Volume : 0f);
			}
			else
				s = loop ? Game.Sound.PlayLooped(SoundType.World, sound, shouldStart ? Info.Volume : 0f) :
					Game.Sound.Play(SoundType.World, sound, shouldStart ? Info.Volume : 0f);

			currentSounds.Add(s);
		}

		void StopSound()
		{
			foreach (var s in currentSounds)
				Game.Sound.StopSound(s);

			currentSounds.Clear();
		}

		protected override void TraitEnabled(Actor self) { delay = Util.RandomInRange(self.World.SharedRandom, Info.Delay); }
		protected override void TraitDisabled(Actor self) { StopSound(); }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { StopSound(); }
	}
}
