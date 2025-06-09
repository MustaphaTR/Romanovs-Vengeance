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
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Blinks the actor and captor when it is being captured.")]
	sealed class CapturableProgressBlinkInfo : ConditionalTraitInfo, Requires<CapturableInfo>
	{
		[Desc("Number of ticks to wait between repeating blinks.")]
		public readonly int Interval = 50;

		[Desc("Sound to play at the same time the actor blinks.")]
		public readonly string Sound = null;

		[Desc("Do the sound play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the Sound played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new CapturableProgressBlink(this); }
	}

	sealed class CapturableProgressBlink : ConditionalTrait<CapturableProgressBlinkInfo>, ITick, ICaptureProgressWatcher
	{
		readonly List<Player> captorOwners = [];
		readonly HashSet<Actor> captors = [];
		int tick = 0;

		public CapturableProgressBlink(CapturableProgressBlinkInfo info)
			: base(info) { }

		void ICaptureProgressWatcher.Update(Actor self, Actor captor, Actor target, int current, int total)
		{
			if (self != target)
				return;

			if (total == 0)
			{
				captors.Remove(captor);
				if (!captors.Any(c => c.Owner == captor.Owner))
					captorOwners.Remove(captor.Owner);
			}
			else if (captors.Add(captor) && !captorOwners.Contains(captor.Owner))
				captorOwners.Add(captor.Owner);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (captorOwners.Count == 0)
			{
				tick = 0;
				return;
			}

			// Separate the blinks from different players by 4 ticks
			if (tick / 4 < captorOwners.Count && tick % 4 == 0)
			{
				var captorOwner = captorOwners[tick / 4];
				self.World.Add(new FlashTarget(self, captorOwner.Color));
				foreach (var captor in captors)
					if (captor.Owner == captorOwner)
						self.World.Add(new FlashTarget(captor, captorOwner.Color));

				var pos = self.CenterPosition;
				if (Info.Sound != null && (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos))))
					Game.Sound.Play(SoundType.World, Info.Sound, pos, Info.SoundVolume);
			}

			if (++tick >= Info.Interval)
				tick = 0;
		}
	}
}
