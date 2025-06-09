#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Flags]
	public enum PeriodicConditionTrigger
	{
		None = 0,
		Attack = 1,
		Move = 2,
		Damage = 4,
		Heal = 8
	}

	[Desc("Grants a condition when a selected event occurs.")]
	public class GrantPeriodicConditionOnEventInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("The range of time (in ticks) with the condition being disabled.")]
		public readonly int[] CooldownDuration = { 1000 };

		[Desc("The range of time (in ticks) with the condition being enabled.")]
		public readonly int[] ActiveDuration = { 100 };

		public readonly PeriodicConditionTrigger Triggers = PeriodicConditionTrigger.Damage;

		public readonly bool StartsCharged = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color CooldownColor = Color.DarkRed;
		public readonly Color ActiveColor = Color.DarkMagenta;

		public override object Create(ActorInitializer init) { return new GrantPeriodicConditionOnEvent(init, this); }
	}

	public enum PeriodicConditionState { Charging, Ready, Active }

	public class GrantPeriodicConditionOnEvent : PausableConditionalTrait<GrantPeriodicConditionOnEventInfo>, ISelectionBar,
		ITick, ISync, INotifyDamage, INotifyAttack
	{
		readonly Actor self;
		readonly GrantPeriodicConditionOnEventInfo info;

		[Sync]
		int ticks;

		int cooldown, active;
		int token = Actor.InvalidConditionToken;
		WPos? lastPos;

		bool IsEnabled { get { return token != Actor.InvalidConditionToken; } }

		PeriodicConditionState state;

		public GrantPeriodicConditionOnEvent(ActorInitializer init, GrantPeriodicConditionOnEventInfo info)
			: base(info)
		{
			self = init.Self;
			this.info = info;
		}

		void SetDefaultState()
		{
			if (info.StartsCharged)
			{
				ticks = info.ActiveDuration.Length == 2
					? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
					: info.ActiveDuration[0];
				active = ticks;
				state = PeriodicConditionState.Ready;
			}
			else
			{
				ticks = info.CooldownDuration.Length == 2
					? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
					: info.CooldownDuration[0];
				cooldown = ticks;
				state = PeriodicConditionState.Charging;
				if (IsEnabled)
					DisableCondition();
			}
		}

		protected override void Created(Actor self)
		{
			if (!IsTraitDisabled)
				SetDefaultState();

			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (state != PeriodicConditionState.Ready && --ticks < 0)
			{
				if (IsEnabled)
				{
					ticks = info.CooldownDuration.Length == 2
						? self.World.SharedRandom.Next(info.CooldownDuration[0], info.CooldownDuration[1])
						: info.CooldownDuration[0];
					cooldown = ticks;
					DisableCondition();
					state = PeriodicConditionState.Charging;
				}
				else
				{
					ticks = info.ActiveDuration.Length == 2
						? self.World.SharedRandom.Next(info.ActiveDuration[0], info.ActiveDuration[1])
						: info.ActiveDuration[0];
					active = ticks;
					state = PeriodicConditionState.Ready;
				}
			}

			if (Info.Triggers.HasFlag(PeriodicConditionTrigger.Move))
			{
				if (state == PeriodicConditionState.Ready && (lastPos == null || lastPos.Value != self.CenterPosition))
					TryEnableCondition();

				lastPos = self.CenterPosition;
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			SetDefaultState();
		}

		protected override void TraitDisabled(Actor self)
		{
			if (IsEnabled)
			{
				DisableCondition();
				state = PeriodicConditionState.Ready;
			}
		}

		protected override void TraitPaused(Actor self)
		{
			TraitDisabled(self);
		}

		protected override void TraitResumed(Actor self)
		{
			TryEnableCondition();
		}

		void TryEnableCondition()
		{
			if (IsTraitDisabled)
				return;

			if (state == PeriodicConditionState.Ready && token == Actor.InvalidConditionToken)
			{
				token = self.GrantCondition(info.Condition);
				state = PeriodicConditionState.Active;
			}
		}

		void DisableCondition()
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			return state != PeriodicConditionState.Charging
				? (float)ticks / active
					: (float)(cooldown - ticks) / cooldown;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return info.ShowSelectionBar && !IsTraitDisabled; } }

		Color ISelectionBar.GetColor() { return state == PeriodicConditionState.Charging ? info.CooldownColor : info.ActiveColor; }

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (Info.Triggers.HasFlag(PeriodicConditionTrigger.Damage) && e.Damage.Value > 0)
				TryEnableCondition();

			if (Info.Triggers.HasFlag(PeriodicConditionTrigger.Heal) && e.Damage.Value < 0)
				TryEnableCondition();
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (Info.Triggers.HasFlag(PeriodicConditionTrigger.Attack))
				TryEnableCondition();
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
