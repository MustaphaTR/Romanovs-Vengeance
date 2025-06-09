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
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition when owner of the actor is a specified player.")]
	public class GrantConditionOnInternalOwnerInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Map player names to grant the condition to.")]
		public readonly HashSet<string> InternalOwners = new();

		public override object Create(ActorInitializer init) { return new GrantConditionOnInternalOwner(init.Self, this); }
	}

	public class GrantConditionOnInternalOwner : INotifyOwnerChanged
	{
		readonly GrantConditionOnInternalOwnerInfo info;

		int token = Actor.InvalidConditionToken;

		public GrantConditionOnInternalOwner(Actor self, GrantConditionOnInternalOwnerInfo info)
		{
			this.info = info;

			if (info.InternalOwners.Contains(self.Owner.InternalName))
				GrantCondition(self, info.Condition);
			else
				RevokeCondition(self);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (token != Actor.InvalidConditionToken)
				return;

			token = self.GrantCondition(cond);
		}

		void RevokeCondition(Actor self)
		{
			if (token == Actor.InvalidConditionToken)
				return;

			token = self.RevokeCondition(token);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (info.InternalOwners.Contains(self.Owner.InternalName))
				GrantCondition(self, info.Condition);
			else
				RevokeCondition(self);
		}
	}
}
