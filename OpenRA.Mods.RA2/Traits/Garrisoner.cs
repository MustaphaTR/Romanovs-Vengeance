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

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Mods.RA2.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	public enum AlternateGarrisonMode { None, Force, Default, Always }

	[Desc("This actor can enter Garrisonable actors.")]
	public class GarrisonerInfo : ITraitInfo
	{
		public readonly string GarrisonType = null;
		public readonly PipType PipType = PipType.Green;
		public readonly int Weight = 1;

		[Desc("Use to set when to use alternate transports (Never, Force, Default, Always).",
			"Force - use force move modifier (Alt) to enable.",
			"Default - use force move modifier (Alt) to disable.")]
		public readonly AlternateGarrisonMode AlternateGarrisoningMode = AlternateGarrisonMode.Default;

		[Desc("Number of retries using alternate transports.")]
		public readonly int MaxAlternateTransportAttempts = 1;

		[Desc("Range from self for looking for an alternate transport (default: 5.5 cells).")]
		public readonly WDist AlternateTransportScanRange = WDist.FromCells(11) / 2;

		[Desc("What diplomatic stances can be Garrisoned by this actor.")]
		public readonly Stance TargetStances = Stance.Ally | Stance.Neutral;

		[GrantedConditionReference]
		[Desc("The condition to grant to when this actor is loaded inside any transport.")]
		public readonly string GarrisonCondition = null;

		[Desc("Conditions to grant when this actor is loaded inside specified transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> GarrisonConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterGarrisonConditions { get { return GarrisonConditions.Values; } }

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new Garrisoner(this); }
	}

	public class Garrisoner : INotifyCreated, IIssueOrder, IResolveOrder, IOrderVoice, INotifyRemovedFromWorld, INotifyEnteredGarrison, INotifyExitedGarrison
	{
		public readonly GarrisonerInfo Info;
		public Actor Transport;

		ConditionManager conditionManager;
		int anyGarrisonToken = ConditionManager.InvalidConditionToken;
		int specificGarrisonToken = ConditionManager.InvalidConditionToken;

		public Garrisoner(GarrisonerInfo info)
		{
			Info = info;
			Func<Actor, Actor, bool> canTarget = IsCorrectGarrisonType;
			Func<Actor, Actor, bool> useEnterCursor = CanEnter;
			Orders = new EnterGarrisonOrderTargeter<GarrisonableInfo>[]
			{
				new EnterGarrisonOrderTargeter<GarrisonableInfo>("EnterGarrison", 5, canTarget, useEnterCursor, Info.AlternateGarrisoningMode, info)
			};
		}

		public Garrisonable ReservedGarrison { get; private set; }

		public IEnumerable<IOrderTargeter> Orders { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterGarrison")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsCorrectGarrisonType(Actor self, Actor target)
		{
			var ci = target.Info.TraitInfo<GarrisonableInfo>();
			return ci.Types.Contains(Info.GarrisonType);
		}

		bool CanEnter(Garrisonable garrison)
		{
			return garrison != null && garrison.HasSpace(Info.Weight);
		}

		bool CanEnter(Actor self, Actor target)
		{
			return CanEnter(target.TraitOrDefault<Garrisonable>());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterGarrison")
				return null;

			if (order.Target.Type != TargetType.Actor || !CanEnter(self, order.Target.Actor))
				return null;

			return Info.Voice;
		}

		void INotifyEnteredGarrison.OnEnteredGarrison(Actor self, Actor garrison)
		{
			string specificGarrisonCondition;
			if (conditionManager != null)
			{
				if (anyGarrisonToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.GarrisonCondition))
					anyGarrisonToken = conditionManager.GrantCondition(self, Info.GarrisonCondition);

				if (specificGarrisonToken == ConditionManager.InvalidConditionToken && Info.GarrisonConditions.TryGetValue(garrison.Info.Name, out specificGarrisonCondition))
					specificGarrisonToken = conditionManager.GrantCondition(self, specificGarrisonCondition);
			}
		}

		void INotifyExitedGarrison.OnExitedGarrison(Actor self, Actor garrison)
		{
			if (anyGarrisonToken != ConditionManager.InvalidConditionToken)
				anyGarrisonToken = conditionManager.RevokeCondition(self, anyGarrisonToken);

			if (specificGarrisonToken != ConditionManager.InvalidConditionToken)
				specificGarrisonToken = conditionManager.RevokeCondition(self, specificGarrisonToken);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EnterGarrison")
				return;

			if (order.Target.Type == TargetType.Actor)
			{
				var targetActor = order.Target.Actor;
				if (!CanEnter(self, targetActor))
					return;
			}
			else
			{
				var targetActor = order.Target.FrozenActor;
			}

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(order.Target, Color.Green);
			self.QueueActivity(new EnterGarrison(self, order.Target));
		}

		public bool Reserve(Actor self, Garrisonable garrison)
		{
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
	}
}
