#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Allow the actor to use it's health percentage",
		"as a firepower multiplier.")]
	class HealthPercentageFirepowerMultiplierInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[Desc("Weapon types to applies to. Leave empty to apply to all weapons.")]
		public readonly HashSet<string> Types = new();

		public override object Create(ActorInitializer init)
		{
			return new HealthPercentageFirepowerMultiplier(init.Self, this);
		}
	}

	class HealthPercentageFirepowerMultiplier : ConditionalTrait<HealthPercentageFirepowerMultiplierInfo>, IFirepowerModifier
	{
		readonly Health health;

		public HealthPercentageFirepowerMultiplier(Actor self, HealthPercentageFirepowerMultiplierInfo info)
			: base(info)
		{
			health = self.Trait<Health>();
		}

		int IFirepowerModifier.GetFirepowerModifier(string armamentName)
		{
			return !IsTraitDisabled
				&& (Info.Types.Count == 0 || (!string.IsNullOrEmpty(armamentName) && Info.Types.Contains(armamentName))) ? 100 * health.HP / health.MaxHP : 100;
		}
	}
}
