#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.AS.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can enter Garrisonable actors.")]
	public class GarrisonerInfo : TraitInfo
	{
		public readonly string GarrisonType = null;

		[Desc("If defined, use a custom pip type defined on the transport's WithGarrisonPipsDecoration.CustomPipSequences list.")]
		public readonly string CustomPipType = null;

		public readonly int Weight = 1;

		[Desc("What diplomatic stances can be Garrisoned by this actor.")]
		public readonly PlayerRelationship TargetRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral;

		[GrantedConditionReference]
		[Desc("The condition to grant to when this actor is loaded inside any transport.")]
		public readonly string GarrisonCondition = null;

		[Desc("Conditions to grant when this actor is loaded inside specified transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> GarrisonConditions = new();

		[GrantedConditionReference]
		public IEnumerable<string> LinterGarrisonConditions { get { return GarrisonConditions.Values; } }

		[VoiceReference]
		public readonly string Voice = "Action";

		[ConsumedConditionReference]
		[Desc("Boolean expression defining the condition under which the regular (non-force) enter cursor is disabled.")]
		public readonly BooleanExpression RequireForceMoveCondition = null;

		public override object Create(ActorInitializer init) { return new Garrisoner(this); }
	}

	public class Garrisoner : IIssueOrder, IResolveOrder, IOrderVoice,
		INotifyRemovedFromWorld, INotifyEnteredGarrison, INotifyExitedGarrison, INotifyKilled, IObservesVariables
	{
		public readonly GarrisonerInfo Info;
		public Actor Transport;
		bool requireForceMove;

		int anyGarrisonToken = Actor.InvalidConditionToken;
		int specificGarrisonToken = Actor.InvalidConditionToken;

		public Garrisoner(GarrisonerInfo info)
		{
			Info = info;
		}

		public Garrisonable ReservedGarrison { get; private set; }

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new EnterGarrisonOrderTargeter<GarrisonableInfo>("EnterGarrison", 5, IsCorrectGarrisonType, CanEnter, Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "EnterGarrison")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsCorrectGarrisonType(Actor target, TargetModifiers modifiers)
		{
			if (requireForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return IsCorrectGarrisonType(target);
		}

		bool IsCorrectGarrisonType(Actor target)
		{
			var ci = target.Info.TraitInfo<GarrisonableInfo>();
			return ci.Types.Contains(Info.GarrisonType);
		}

		bool CanEnter(Garrisonable garrison)
		{
			return garrison != null && garrison.HasSpace(Info.Weight) && !garrison.IsTraitPaused && !garrison.IsTraitDisabled;
		}

		bool CanEnter(Actor target)
		{
			return CanEnter(target.TraitOrDefault<Garrisonable>());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterGarrison")
				return null;

			if (order.Target.Type != TargetType.Actor || !CanEnter(order.Target.Actor))
				return null;

			return Info.Voice;
		}

		void INotifyEnteredGarrison.OnEnteredGarrison(Actor self, Actor garrison)
		{
			if (anyGarrisonToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.GarrisonCondition))
				anyGarrisonToken = self.GrantCondition(Info.GarrisonCondition);

			if (specificGarrisonToken == Actor.InvalidConditionToken && Info.GarrisonConditions.TryGetValue(garrison.Info.Name, out var specificGarrisonCondition))
				specificGarrisonToken = self.GrantCondition(specificGarrisonCondition);

			// Allow scripted / initial actors to move from the unload point back into the cell grid on unload
			// This is handled by the RideTransport activity for player-loaded cargo
			if (self.IsIdle)
			{
				// IMove is not used anywhere else in this trait, there is no benefit to caching it from Created.
				var move = self.TraitOrDefault<IMove>();
				if (move != null)
					self.QueueActivity(move.ReturnToCell(self));
			}
		}

		void INotifyExitedGarrison.OnExitedGarrison(Actor self, Actor garrison)
		{
			if (anyGarrisonToken != Actor.InvalidConditionToken)
				anyGarrisonToken = self.RevokeCondition(anyGarrisonToken);

			if (specificGarrisonToken != Actor.InvalidConditionToken)
				specificGarrisonToken = self.RevokeCondition(specificGarrisonToken);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterGarrison")
				return;

			if (order.Target.Type == TargetType.Actor)
			{
				var targetActor = order.Target.Actor;
				if (!CanEnter(targetActor))
					return;

				if (!IsCorrectGarrisonType(targetActor))
					return;
			}

			/*
			else
			{
				var targetActor = order.Target.FrozenActor;
			}
			*/

			self.QueueActivity(order.Queued, new EnterGarrison(self, order.Target, Color.Green));
			self.ShowTargetLines();
		}

		public bool Reserve(Actor self, Garrisonable garrison)
		{
			if (garrison == ReservedGarrison)
				return true;

			Unreserve(self);
			if (!garrison.ReserveSpace(self))
				return false;

			ReservedGarrison = garrison;
			return true;
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self) { Unreserve(self); }

		public void Unreserve(Actor self)
		{
			if (ReservedGarrison == null)
				return;

			ReservedGarrison.UnreserveSpace(self);
			ReservedGarrison = null;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Transport == null)
				return;

			// Something killed us, but it wasn't our transport blowing up. Remove us from the cargo.
			if (!Transport.IsDead)
				self.World.AddFrameEndTask(w => Transport.Trait<Garrisonable>().Unload(Transport, self));
		}

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			if (Info.RequireForceMoveCondition != null)
				yield return new VariableObserver(RequireForceMoveConditionChanged, Info.RequireForceMoveCondition.Variables);
		}

		void RequireForceMoveConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			requireForceMove = Info.RequireForceMoveCondition.Evaluate(conditions);
		}
	}
}
