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

using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Gives a condition to the actor after its owner loses the game.")]
	public class GrantConditionOnOwnerLostInfo : TraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantConditionOnOwnerLost(this); }
	}

	public class GrantConditionOnOwnerLost : INotifyOwnerLost
	{
		readonly GrantConditionOnOwnerLostInfo info;

		int token = Actor.InvalidConditionToken;

		public GrantConditionOnOwnerLost(GrantConditionOnOwnerLostInfo info)
		{
			this.info = info;
		}

		void INotifyOwnerLost.OnOwnerLost(Actor self)
		{
			GrantCondition(self, info.Condition);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			if (token != Actor.InvalidConditionToken)
				return;

			token = self.GrantCondition(cond);
		}
	}
}
