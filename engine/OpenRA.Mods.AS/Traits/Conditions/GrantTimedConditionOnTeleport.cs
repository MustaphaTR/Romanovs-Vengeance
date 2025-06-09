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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Gives a condition to the actor for a limited time after teleportation.")]
	public class GrantTimedConditionOnTeleportInfo : PausableConditionalTraitInfo
	{
		[Desc("Only apply the condition for teleports with these teleport types.")]
		public readonly HashSet<string> TeleportTypes = default;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before revoking the condition.")]
		public readonly int Duration = 120;

		public override object Create(ActorInitializer init) { return new GrantTimedConditionOnTeleport(init.Self, this); }
	}

	public class GrantTimedConditionOnTeleport : PausableConditionalTrait<GrantTimedConditionOnTeleportInfo>, ITick, ISync,
		INotifyCreated, IOnSuccessfulTeleportRA2
	{
		readonly Actor self;
		readonly GrantTimedConditionOnTeleportInfo info;
		int token = Actor.InvalidConditionToken;
		IConditionTimerWatcher[] watchers;

		[Sync]
		public int Ticks { get; private set; }

		public GrantTimedConditionOnTeleport(Actor self, GrantTimedConditionOnTeleportInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			Ticks = info.Duration;
		}

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void GrantCondition(Actor self, string condition)
		{
			if (string.IsNullOrEmpty(condition))
				return;

			if (token == Actor.InvalidConditionToken)
			{
				Ticks = info.Duration;
				token = self.GrantCondition(condition);
			}
		}

		void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled && token != Actor.InvalidConditionToken)
				RevokeCondition(self);

			if (IsTraitPaused || IsTraitDisabled)
				return;

			foreach (var w in watchers)
				w.Update(info.Duration, Ticks);

			if (token == Actor.InvalidConditionToken)
				return;

			if (--Ticks < 0)
				RevokeCondition(self);
		}

		void IOnSuccessfulTeleportRA2.OnSuccessfulTeleport(string type, WPos oldPos, WPos newPos)
		{
			if (Info.TeleportTypes.Count != 0 && Info.TeleportTypes.Contains(type))
				GrantCondition(self, info.Condition);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }
	}
}
