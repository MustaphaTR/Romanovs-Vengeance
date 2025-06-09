#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a random condition from a predefined list to the actor when created." +
		"Rerandomized when the actor changes ownership.")]
	public class GrantRandomConditionOnOwnerChangeInfo : TraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("List of conditions to grant from.")]
		public readonly string[] Conditions = null;

		public override object Create(ActorInitializer init) { return new GrantRandomConditionOnOwnerChange(this); }
	}

	public class GrantRandomConditionOnOwnerChange : INotifyCreated, INotifyOwnerChanged
	{
		readonly GrantRandomConditionOnOwnerChangeInfo info;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantRandomConditionOnOwnerChange(GrantRandomConditionOnOwnerChangeInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			if (info.Conditions.Length <= 0)
				return;

			var condition = info.Conditions.Random(self.World.SharedRandom);
			conditionToken = self.GrantCondition(condition);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (conditionToken != Actor.InvalidConditionToken)
			{
				self.RevokeCondition(conditionToken);
				var condition = info.Conditions.Random(self.World.SharedRandom);
				conditionToken = self.GrantCondition(condition);
			}
		}
	}
}
