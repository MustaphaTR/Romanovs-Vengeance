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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a shield with its own health pool. Main health pool is unaffected by damage until the shield is broken.")]
	public class ShieldedInfo : PausableConditionalTraitInfo
	{
		[Desc("The strength of the shield (amount of damage it will absorb).")]
		public readonly int MaxStrength = 1000;

		[Desc("Strength of the shield when the trait is enabled.")]
		public readonly int InitialStrength = 1000;

		[Desc("Delay in ticks before shield regenerate for the first time after trait is enabled.")]
		public readonly int InitialRegenDelay = 0;

		[Desc("Delay in ticks after absorbing damage before the shield will regenerate.")]
		public readonly int DamageRegenDelay = 0;

		[Desc("Amount to recharge at each interval.")]
		public readonly int RegenAmount = 0;

		[Desc("Number of ticks between recharging.")]
		public readonly int RegenInterval = 25;

		[Desc("Block the remaining damage after shield breaks.")]
		public readonly bool BlockExcessDamage = false;

		[Desc("Damage types that ignore this shield.")]
		public readonly BitSet<DamageType> IgnoreShieldDamageTypes = default;

		[GrantedConditionReference]
		[Desc("Condition to grant when shields are active.")]
		public readonly string ShieldsUpCondition = null;

		[Desc("Hides selection bar when shield is at max strength.")]
		public readonly bool HideBarWhenFull = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.FromArgb(128, 200, 255);

		public override object Create(ActorInitializer init) { return new Shielded(init, this); }
	}

	public class Shielded : PausableConditionalTrait<ShieldedInfo>, ITick, ISync, ISelectionBar, IDamageModifier, INotifyDamage
	{
		int conditionToken = Actor.InvalidConditionToken;
		readonly Actor self;

		[Sync]
		public int Strength;
		int ticks;

		public Shielded(ActorInitializer init, ShieldedInfo info)
			: base(info)
		{
			self = init.Self;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			Strength = Info.InitialStrength;
			ticks = Info.InitialRegenDelay;
		}

		void ITick.Tick(Actor self)
		{
			Regenerate(self);
		}

		protected void Regenerate(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (Strength == Info.MaxStrength)
				return;

			if (--ticks > 0)
				return;

			Strength += Info.RegenAmount;

			if (Strength > Info.MaxStrength)
				Strength = Info.MaxStrength;

			if (Strength > 0 && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);

			ticks = Info.RegenInterval;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled)
				return;

			if (e.Damage.Value < 0 || (!Info.IgnoreShieldDamageTypes.IsEmpty && e.Damage.DamageTypes.Overlaps(Info.IgnoreShieldDamageTypes)))
				return;

			if (ticks < Info.DamageRegenDelay)
				ticks = Info.DamageRegenDelay;

			if (Strength == 0 || e.Damage.Value == 0 || e.Attacker == self)
				return;

			var damageAmt = Convert.ToInt32(e.Damage.Value / 0.01);
			var damageTypes = e.Damage.DamageTypes;
			var excessDamage = damageAmt - Strength;
			Strength = Math.Max(Strength - damageAmt, 0);

			var health = self.TraitOrDefault<IHealth>();

			if (health != null)
			{
				var absorbedDamage = new Damage(-e.Damage.Value, damageTypes);
				health.InflictDamage(self, self, absorbedDamage, true);
			}

			if (Strength == 0 && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);

			if (excessDamage > 0 && !Info.BlockExcessDamage)
			{
				var hullDamage = new Damage(excessDamage, damageTypes);

				health?.InflictDamage(self, e.Attacker, hullDamage, true);
			}
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || Strength == 0 || (Strength == Info.MaxStrength && Info.HideBarWhenFull))
				return 0;

			var selected = self.World.Selection.Contains(self);
			var rollover = self.World.Selection.RolloverContains(self);
			var regularWorld = self.World.Type == WorldType.Regular;
			var statusBars = Game.Settings.Game.StatusBars;

			var displayHealth = selected || rollover || (regularWorld && statusBars == StatusBarsType.AlwaysShow)
				|| (regularWorld && statusBars == StatusBarsType.DamageShow && Strength < Info.MaxStrength);

			if (!displayHealth)
				return 0;

			return (float)Strength / Info.MaxStrength;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return IsTraitDisabled || Strength == 0 || (!Info.IgnoreShieldDamageTypes.IsEmpty && damage.DamageTypes.Overlaps(Info.IgnoreShieldDamageTypes)) ? 100 : 1;
		}

		protected override void TraitEnabled(Actor self)
		{
			ticks = Info.InitialRegenDelay;
			Strength = Info.InitialStrength;

			if (conditionToken == Actor.InvalidConditionToken && Strength > 0)
				conditionToken = self.GrantCondition(Info.ShieldsUpCondition);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (conditionToken == Actor.InvalidConditionToken)
				return;

			conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
