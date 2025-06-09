#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class DelayedWeaponTrigger
	{
		readonly WarheadArgs args;

		public readonly BitSet<DamageType> DeathTypes;

		public readonly int TriggerTime;

		public int RemainingTime { get; private set; }

		public Actor AttachedBy { get; }

		readonly WeaponInfo weaponInfo;

		public bool IsValid { get; private set; }

		public DelayedWeaponTrigger(AttachDelayedWeaponWarhead warhead, WarheadArgs args)
		{
			this.args = args;
			TriggerTime = warhead.TriggerTime;
			RemainingTime = TriggerTime;
			DeathTypes = warhead.DeathTypes;
			weaponInfo = warhead.WeaponInfo;
			AttachedBy = args.SourceActor;
			IsValid = true;
		}

		public void Tick(Actor attachable)
		{
			if (attachable.IsDead || !attachable.IsInWorld || !IsValid || TriggerTime < 0)
				return;

			if (--RemainingTime < 0)
				Activate(attachable);
		}

		public void Activate(Actor attachable)
		{
			IsValid = false;
			var target = Target.FromPos(attachable.CenterPosition);
			attachable.World.AddFrameEndTask(w => weaponInfo.Impact(target, args));
		}

		public void Deactivate()
		{
			IsValid = false;
		}
	}
}
