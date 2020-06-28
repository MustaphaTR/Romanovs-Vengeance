#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Grants a condition if owner is combatant.")]
	public class GrantConditionOnCombatantOwnerInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnCombatantOwner(init.Self, this); }
	}

	public class GrantConditionOnCombatantOwner : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnCombatantOwnerInfo info;

		int token = Actor.InvalidConditionToken;

		public GrantConditionOnCombatantOwner(Actor self, GrantConditionOnCombatantOwnerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (!self.Owner.NonCombatant)
				GrantCondition(self, info.Condition);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			token = self.GrantCondition(cond);
		}

		void RevokeCondition(Actor self)
		{
			token = self.RevokeCondition(token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!newOwner.NonCombatant && token == Actor.InvalidConditionToken)
				GrantCondition(self, info.Condition);
			else if (newOwner.NonCombatant && token != Actor.InvalidConditionToken)
				RevokeCondition(self);
		}
	}
}
