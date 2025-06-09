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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When enabled, pre-emptively triggers delayed weapons on the actor.")]
	public class TriggersDelayedWeaponInfo : ConditionalTraitInfo, Requires<DelayedWeaponAttachableInfo>
	{
		[FieldLoader.Require]
		[Desc("Type of DelayedWeapons that can be triggered by this trait.")]
		public readonly string Type = "bomb";

		public override object Create(ActorInitializer init) { return new TriggersDelayedWeapon(init.Self, this); }
	}

	public class TriggersDelayedWeapon : ConditionalTrait<TriggersDelayedWeaponInfo>
	{
		readonly DelayedWeaponAttachable[] attachables = Array.Empty<DelayedWeaponAttachable>();

		public TriggersDelayedWeapon(Actor self, TriggersDelayedWeaponInfo info)
			: base(info)
		{
			attachables = self.TraitsImplementing<DelayedWeaponAttachable>().Where(dwa => dwa.Info.Type == info.Type).ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var attachable in attachables)
				foreach (var trigger in attachable.Container)
					if (trigger.IsValid)
						trigger.Activate(self);
		}
	}
}
