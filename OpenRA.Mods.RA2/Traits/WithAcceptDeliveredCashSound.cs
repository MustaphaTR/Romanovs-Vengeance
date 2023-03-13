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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Plays a sound when it accepts a cash delivery unit.")]
	public class WithAcceptDeliveredCashSoundInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Sound to play when delivery is done.")]
		public readonly string Sound = null;

		[Desc("Does the sound play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the EnterSound and ExitSound played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new WithAcceptDeliveredCashSound(init.Self, this); }
	}

	public class WithAcceptDeliveredCashSound : ConditionalTrait<WithAcceptDeliveredCashSoundInfo>, INotifyCashTransfer
	{
		public WithAcceptDeliveredCashSound(Actor self, WithAcceptDeliveredCashSoundInfo info)
			: base(info) { }

		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor)
		{
			if (IsTraitDisabled)
				return;

			if (!string.IsNullOrEmpty(Info.Sound))
			{
				var pos = self.CenterPosition;
				if (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, Info.Sound, pos, Info.SoundVolume);
			}
		}

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor) { }
	}
}
