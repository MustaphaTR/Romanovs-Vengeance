#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Gives a condition to the actor after a delay.")]
	public class GrantConditionAfterDelayInfo : PausableConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[Desc("Number of ticks to wait before applying the condition.")]
		public readonly int Delay = 50;

		public readonly bool ShowSelectionBar = true;
		public readonly bool ShowFullBarAfterGranted = true;
		public readonly Color SelectionBarColor = Color.Magenta;

		public override object Create(ActorInitializer init) { return new GrantConditionAfterDelay(this); }
	}

	public class GrantConditionAfterDelay : PausableConditionalTrait<GrantConditionAfterDelayInfo>, ITick, ISync, ISelectionBar
	{
		readonly GrantConditionAfterDelayInfo info;
		int token = Actor.InvalidConditionToken;

		[Sync]
		public int Ticks { get; private set; }

		public GrantConditionAfterDelay(GrantConditionAfterDelayInfo info)
			: base(info)
		{
			this.info = info;
			Ticks = info.Delay;
		}

		void GrantCondition(Actor self, string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return;

			token = self.GrantCondition(cond);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				Ticks = info.Delay;

			if (IsTraitPaused || IsTraitDisabled)
				return;

			if (--Ticks < 0 && token == Actor.InvalidConditionToken)
				GrantCondition(self, info.Condition);
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || (1f - (float)Ticks / Info.Delay > 1f && !info.ShowFullBarAfterGranted))
				return 0f;

			if (1f - (float)Ticks / Info.Delay > 1f && info.ShowFullBarAfterGranted)
				return 1f;

			return 1f - (float)Ticks / Info.Delay;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return !IsTraitDisabled && Info.ShowSelectionBar; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		protected override void TraitDisabled(Actor self)
		{
			if (token == Actor.InvalidConditionToken)
				return;

			token = self.RevokeCondition(token);
		}
	}
}
