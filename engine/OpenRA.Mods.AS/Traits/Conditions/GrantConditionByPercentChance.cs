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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Conditions
{
	[Desc("Grants a condition randonmly by a specified percent chance.")]
	public class GrantConditionByPercentChanceInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[Desc("Percent change to apply the condition.")]
		public readonly int Chance = 50;

		public override object Create(ActorInitializer init) { return new GrantConditionByPercentChance(this); }
	}

	public class GrantConditionByPercentChance : ConditionalTrait<GrantConditionByPercentChanceInfo>
	{
		int token = Actor.InvalidConditionToken;

		public GrantConditionByPercentChance(GrantConditionByPercentChanceInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			if (string.IsNullOrEmpty(Info.Condition) || token != Actor.InvalidConditionToken)
				return;

			if (self.World.SharedRandom.Next(100) >= Info.Chance)
				return;

			token = self.GrantCondition(Info.Condition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (token == Actor.InvalidConditionToken)
				return;

			token = self.RevokeCondition(token);
		}
	}
}
