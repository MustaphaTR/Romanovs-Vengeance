#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits.Sound
{
	public class CaptureSoundInfo : ITraitInfo
    {
        [Desc("Sound to play when actor is captured.")]
        public readonly string Sound = null;

        public object Create(ActorInitializer init) { return new CaptureSound(this); }
	}

	public class CaptureSound : INotifyCapture
	{
		readonly CaptureSoundInfo info;
		public CaptureSound(CaptureSoundInfo info)
		{
			this.info = info;
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
        {
            Game.Sound.Play(SoundType.World, info.Sound, self.CenterPosition);
        }
	}
}
