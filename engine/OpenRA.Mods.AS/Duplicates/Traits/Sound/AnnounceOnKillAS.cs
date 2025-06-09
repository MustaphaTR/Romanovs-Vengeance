#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Play the Kill voice of this actor when eliminating enemies.")]
	public class AnnounceOnKillASInfo : TraitInfo
	{
		[Desc("Minimum duration (in seconds) between sound events.")]
		public readonly int Interval = 5;

		[VoiceReference]
		[Desc("Voice to use when killing something.")]
		public readonly string Voice = "Kill";

		[Desc("Should the voice be played for the owner alone?")]
		public readonly bool OnlyToOwner = false;

		public override object Create(ActorInitializer init) { return new AnnounceOnKillAS(this); }
	}

	public class AnnounceOnKillAS : INotifyAppliedDamage
	{
		readonly AnnounceOnKillASInfo info;

		int lastAnnounce;

		public AnnounceOnKillAS(AnnounceOnKillASInfo info)
		{
			this.info = info;
			lastAnnounce = -info.Interval * 25;
		}

		void INotifyAppliedDamage.AppliedDamage(Actor self, Actor damaged, AttackInfo e)
		{
			// Don't notify suicides
			if (e.DamageState == DamageState.Dead && damaged != e.Attacker)
			{
				if (info.OnlyToOwner && self.Owner != self.World.RenderPlayer)
					return;

				if (self.World.WorldTick - lastAnnounce > info.Interval * 25)
					self.PlayVoice(info.Voice);

				lastAnnounce = self.World.WorldTick;
			}
		}
	}
}
