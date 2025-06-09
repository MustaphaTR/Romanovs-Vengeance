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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Modifies the reload time of weapons fired by this actor as the weapon firing.")]
	public class GatlingReloadDelayMultiplierInfo : PausableConditionalTraitInfo
	{
		[Desc("Maximum Percentage modifier to apply.")]
		public readonly int MaxModifier = 100;

		[Desc("Minimum Percentage modifier to apply.")]
		public readonly int MinModifier = 25;

		[Desc("How many time trigger the Cool Down when not attack.")]
		public readonly int CoolDownDelay = 20;

		[Desc("The change on reload modifier when not attack.")]
		public readonly int CoolDownChange = 1;

		[Desc("The change on reload modifier when attack.")]
		public readonly int HeatUpChange = -1;

		[Desc("Should an instance be revoked if the actor changes target?")]
		public readonly bool RevokeOnNewTarget = false;

		[Desc("Weapon types to applies to. Leave empty to apply to all weapons.")]
		public readonly HashSet<string> Types = new();

		public override object Create(ActorInitializer init) { return new GatlingReloadDelayMultiplier(this); }
	}

	public class GatlingReloadDelayMultiplier : PausableConditionalTrait<GatlingReloadDelayMultiplierInfo>, IReloadModifier, INotifyAttack, ITick
	{
		int currentModifier;
		int cooldown;

		// Only tracked when RevokeOnNewTarget is true.
		Target lastTarget = Target.Invalid;

		public GatlingReloadDelayMultiplier(GatlingReloadDelayMultiplierInfo info)
			: base(info)
		{
			currentModifier = info.MaxModifier;
		}

		static bool TargetChanged(in Target lastTarget, in Target target)
		{
			// Invalidate reveal changing the target.
			if (lastTarget.Type == TargetType.FrozenActor && target.Type == TargetType.Actor && lastTarget.FrozenActor.Actor == target.Actor)
				return false;

			if (lastTarget.Type == TargetType.Actor && target.Type == TargetType.FrozenActor && target.FrozenActor.Actor == lastTarget.Actor)
				return false;

			if (lastTarget.Type != target.Type)
				return true;

			// Invalidate attacking different targets with shared target types.
			if (lastTarget.Type == TargetType.Actor && target.Type == TargetType.Actor && lastTarget.Actor != target.Actor)
				return true;

			if (lastTarget.Type == TargetType.FrozenActor && target.Type == TargetType.FrozenActor && lastTarget.FrozenActor != target.FrozenActor)
				return true;

			if (lastTarget.Type == TargetType.Terrain && target.Type == TargetType.Terrain && lastTarget.CenterPosition != target.CenterPosition)
				return true;

			return false;
		}

		void ITick.Tick(Actor self)
		{
			if (cooldown <= 0)
				currentModifier += Info.CoolDownChange;
			else
			{
				currentModifier += Info.HeatUpChange;
				cooldown--;
			}

			if (currentModifier > Info.MaxModifier) currentModifier = Info.MaxModifier;
			else if (currentModifier < Info.MinModifier) currentModifier = Info.MinModifier;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused || (Info.Types.Count > 0 && !Info.Types.Contains(a.Info.Name)))
				return;

			if (Info.RevokeOnNewTarget)
			{
				if (TargetChanged(lastTarget, target))
					currentModifier = Info.MaxModifier;

				lastTarget = target;
			}

			cooldown = Info.CoolDownDelay;
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		int IReloadModifier.GetReloadModifier(string armamentName)
		{
			return !IsTraitDisabled && (Info.Types.Count == 0 || (!string.IsNullOrEmpty(armamentName) && Info.Types.Contains(armamentName))) ? currentModifier : 100;
		}
	}
}
