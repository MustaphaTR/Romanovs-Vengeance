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
	public class GrantConditionOnCombatantOwnerInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new GrantConditionOnCombatantOwner(init.Self, this); }
	}

	public class GrantConditionOnCombatantOwner : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantConditionOnCombatantOwnerInfo info;
		ConditionManager manager;

		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionOnCombatantOwner(Actor self, GrantConditionOnCombatantOwnerInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();

			if (!self.Owner.NonCombatant)
				GrantCondition(self, info.Condition);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			token = manager.GrantCondition(self, cond);
		}

		void RevokeCondition(Actor self)
		{
			if (manager == null)
				return;

			token = manager.RevokeCondition(self, token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!newOwner.NonCombatant && token == ConditionManager.InvalidConditionToken)
				GrantCondition(self, info.Condition);
			else if (newOwner.NonCombatant && token != ConditionManager.InvalidConditionToken)
				RevokeCondition(self);
		}
	}
}
