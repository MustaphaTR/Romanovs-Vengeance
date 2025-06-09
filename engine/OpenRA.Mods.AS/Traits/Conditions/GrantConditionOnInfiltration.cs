#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition when this building is infiltrated.")]
	class GrantConditionOnInfiltrationInfo : ConditionalTraitInfo
	{
		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		[FieldLoader.Require]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("Use `TimedConditionBar` for visualization.")]
		public readonly int Duration = 0;

		public override object Create(ActorInitializer init) { return new GrantConditionOnInfiltration(this); }
	}

	class GrantConditionOnInfiltration : ConditionalTrait<GrantConditionOnInfiltrationInfo>, INotifyInfiltrated, INotifyCreated, ITick
	{
		int conditionToken = Actor.InvalidConditionToken;
		int duration;
		IConditionTimerWatcher[] watchers;

		public GrantConditionOnInfiltration(GrantConditionOnInfiltrationInfo info)
			: base(info) { }

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!Info.Types.Overlaps(types) || IsTraitDisabled)
				return;

			duration = Info.Duration;

			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
		}

		bool Notifies(IConditionTimerWatcher watcher) { return watcher.Condition == Info.Condition; }

		protected override void Created(Actor self)
		{
			watchers = self.TraitsImplementing<IConditionTimerWatcher>().Where(Notifies).ToArray();

			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (conditionToken != Actor.InvalidConditionToken && Info.Duration > 0)
			{
				if (--duration < 0)
				{
					conditionToken = self.RevokeCondition(conditionToken);
					foreach (var w in watchers)
						w.Update(0, 0);
				}
				else
					foreach (var w in watchers)
						w.Update(Info.Duration, duration);
			}
		}
	}
}
