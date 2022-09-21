#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class CaptureSoundInfo : TraitInfo
	{
		[Desc("Sound to play when actor is captured.")]
		public readonly string Sound = null;

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the sounds played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new CaptureSound(this); }
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
			var pos = self.CenterPosition;
			if (info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
				Game.Sound.Play(SoundType.World, info.Sound, pos, info.SoundVolume);
		}
	}
}
