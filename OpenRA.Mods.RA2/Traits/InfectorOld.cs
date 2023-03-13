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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public class InfectorOldInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly BitSet<TargetableType> Types;

		[FieldLoader.Require]
		[Desc("How much damage to deal.")]
		public readonly int Damage;

		[FieldLoader.Require]
		[Desc("How often to deal the damage.")]
		public readonly int DamageInterval;

		[Desc("Sounds to play when damage is dealt.")]
		public readonly string[] DamageSounds = { };

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the sounds played at.")]
		public readonly float Volume = 1f;

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

		[SequenceReference]
		[Desc("Sequence to use upon infection beginning.")]
		public readonly string StartSequence = null;

		[SequenceReference]
		[Desc("Sequence name to play during infection.")]
		public readonly string Sequence = null;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[VoiceReference]
		[Desc("Voice string when ordered to infect an actor.")]
		public readonly string Voice = "Action";

		public readonly PlayerRelationship TargetRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;
		public readonly PlayerRelationship ForceTargetRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral | PlayerRelationship.Ally;

		public readonly string Cursor = "attack";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while infecting any actor.")]
		public readonly string InfectingCondition = null;

		public override object Create(ActorInitializer init) { return new InfectorOld(this); }
	}

	public class InfectorOld : ConditionalTrait<InfectorOldInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		int token = Actor.InvalidConditionToken;

		public InfectorOld(InfectorOldInfo info)
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

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
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

			self.QueueActivity(new InfectOld(self, order.Target, this));
			self.ShowTargetLines();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Infect" ? Info.Voice : null;
		}

		public void GrantCondition(Actor self)
		{
			if (token == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.InfectingCondition))
				token = self.GrantCondition(Info.InfectingCondition);
		}

		public void RevokeCondition(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		class InfectionOrderTargeter : UnitOrderTargeter
		{
			readonly InfectorOldInfo info;

			public InfectionOrderTargeter(InfectorOldInfo info)
				: base("Infect", 7, info.Cursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var relationship = self.Owner.RelationshipWith(target.Owner);
				if (!info.TargetRelationships.HasRelationship(relationship) && !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;
				if (!info.ForceTargetRelationships.HasRelationship(relationship) && modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				return info.Types.Overlaps(target.GetAllTargetTypes());
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var relationship = self.Owner.RelationshipWith(target.Owner);
				if (!info.TargetRelationships.HasRelationship(relationship) && !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;
				if (!info.ForceTargetRelationships.HasRelationship(relationship) && modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				return info.Types.Overlaps(target.Info.GetAllTargetTypes());
			}
		}
	}
}
