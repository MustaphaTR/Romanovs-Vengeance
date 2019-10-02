#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public class InfectorInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly BitSet<TargetableType> Types;

		[FieldLoader.Require]
		[Desc("How much damage to deal.")]
		public readonly int Damage;

		[FieldLoader.Require]
		[Desc("How often to deal the damage.")]
		public readonly int DamageInterval;

		[Desc("If more than this outside damage is dealt to infected unit while this actor is in, it is killed when infected unit dies.",
			"Use -1 to never kill the actor.")]
		public readonly int SuppressionDamageThreshold = -1;

		[Desc("If more than this many times outside damage with enough is dealt SuppressionDamageThreshold to infected unit while this actor is in, it is killed when infected unit dies.",
			"Use -1 to never kill the actor.")]
		public readonly int SuppressionAmountThreshold = 1;

		[Desc("If the infected actor enters this damage state, kill the actor.")]
		public readonly DamageState[] KillState = { };

		[Desc("Damage types for the infection damage.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[VoiceReference]
		[Desc("Voice string when ordered to infect an actor.")]
		public readonly string Voice = "Action";

		public readonly Stance TargetStances = Stance.Enemy | Stance.Neutral;
		public readonly Stance ForceTargetStances = Stance.Enemy | Stance.Neutral | Stance.Ally;

		public readonly string Cursor = "attack";

		public override object Create(ActorInitializer init) { return new Infector(this); }
	}

	public class Infector : ConditionalTrait<InfectorInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public Infector(InfectorInfo info)
			: base(info) { }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new InfectionOrderTargeter(Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "Infect")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Infect" || IsTraitDisabled)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.QueueActivity(new Infect(self, order.Target, this));
			self.ShowTargetLines();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Infect" ? Info.Voice : null;
		}

		class InfectionOrderTargeter : UnitOrderTargeter
		{
			readonly InfectorInfo info;

			public InfectionOrderTargeter(InfectorInfo info)
				: base("Infect", 7, info.Cursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var stance = self.Owner.Stances[target.Owner];
				if (!info.TargetStances.HasStance(stance) && !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;
				if (!info.ForceTargetStances.HasStance(stance) && modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				return info.Types.Overlaps(target.GetAllTargetTypes());
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var stance = self.Owner.Stances[target.Owner];
				if (!info.TargetStances.HasStance(stance) && !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;
				if (!info.ForceTargetStances.HasStance(stance) && modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				return info.Types.Overlaps(target.Info.GetAllTargetTypes());
			}
		}
	}
}
