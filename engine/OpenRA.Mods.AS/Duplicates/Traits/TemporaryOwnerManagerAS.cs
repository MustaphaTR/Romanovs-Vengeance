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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Interacts with the ChangeOwner warhead.",
		"Displays a bar how long this actor is affected and reverts back to the old owner on temporary changes.")]
	public class TemporaryOwnerManagerASInfo : TraitInfo
	{
		public readonly Color BarColor = Color.Orange;

		[GrantedConditionReference]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new TemporaryOwnerManagerAS(init.Self, this); }
	}

	public class TemporaryOwnerManagerAS : ISelectionBar, ITick, ISync, INotifyOwnerChanged
	{
		readonly TemporaryOwnerManagerASInfo info;

		int conditionToken = Actor.InvalidConditionToken;

		Player originalOwner;
		Player changingOwner;

		[Sync]
		int remaining = -1;
		int duration;

		public TemporaryOwnerManagerAS(Actor self, TemporaryOwnerManagerASInfo info)
		{
			this.info = info;
			originalOwner = self.Owner;
		}

		public void ChangeOwner(Actor self, Player newOwner, int duration)
		{
			remaining = this.duration = duration;
			changingOwner = newOwner;
			self.ChangeOwner(newOwner);

			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			if (--remaining == 0)
			{
				changingOwner = originalOwner;
				self.ChangeOwner(originalOwner);
				self.CancelActivity(); // Stop shooting, you have got new enemies

				if (conditionToken != Actor.InvalidConditionToken)
					conditionToken = self.RevokeCondition(conditionToken);
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (changingOwner == null || changingOwner != newOwner)
				originalOwner = newOwner; // It wasn't a temporary change, so we need to update here
			else
				changingOwner = null; // It was triggered by this trait: reset
		}

		float ISelectionBar.GetValue()
		{
			if (remaining <= 0)
				return 0;

			return (float)remaining / duration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.BarColor;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }
	}
}
