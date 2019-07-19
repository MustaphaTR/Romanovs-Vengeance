#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Desc("Gives a condition to the actor after its owner loses the game.")]
	public class GrantConditionOnOwnerLostInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new GrantConditionOnOwnerLost(this); }
	}

	public class GrantConditionOnOwnerLost : INotifyCreated, INotifyOwnerLost
	{
		GrantConditionOnOwnerLostInfo info;
		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionOnOwnerLost(GrantConditionOnOwnerLostInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.TraitOrDefault<ConditionManager>();
		}

		void INotifyOwnerLost.OnOwnerLost(Actor self)
		{
			GrantCondition(self, info.Condition);
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			if (token != ConditionManager.InvalidConditionToken)
				return;

			token = manager.GrantCondition(self, cond);
		}
	}
}
