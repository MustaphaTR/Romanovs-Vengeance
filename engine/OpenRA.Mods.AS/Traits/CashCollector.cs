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
using System.Linq;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Periodically collects cash from actors with CashCollectable traits.")]
	public class CashCollectorInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The range within cash gets collected.")]
		public readonly WDist Range;

		[Desc("The maximum vertical range above terrain within cash gets collected.",
			  "Ignored if 0 (actors are upgraded regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		[Desc("What diplomatic stances cash is collected from.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[FieldLoader.Require]
		[Desc("Delay between two collections.")]
		public readonly int Delay;

		[FieldLoader.Require]
		[Desc("The type which allows the actor to collect nearby cash.")]
		public readonly BitSet<CashCollectableType> Type = default;

		[Desc("Whether to show a floating text.")]
		public readonly bool ShowTicks = true;

		public readonly string EnableSound = null;
		public readonly string DisableSound = null;

		public override object Create(ActorInitializer init) { return new CashCollector(init.Self, this); }
	}

	public class CashCollector : ConditionalTrait<CashCollectorInfo>, ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly Actor self;

		int proximityTrigger;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		readonly HashSet<CashCollectable> collectables;

		bool cachedDisabled = true;
		int ticks;

		public CashCollector(Actor self, CashCollectorInfo info)
			: base(info)
		{
			this.self = self;
			cachedRange = info.Range;
			cachedVRange = info.MaximumVerticalOffset;
			ticks = Info.Delay;
			collectables = new HashSet<CashCollectable>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
			CollectCash();
		}

		void ITick.Tick(Actor self)
		{
			var disabled = IsTraitDisabled;

			if (cachedDisabled != disabled)
			{
				Game.Sound.Play(SoundType.World, disabled ? Info.DisableSound : Info.EnableSound, self.CenterPosition);
				desiredRange = disabled ? WDist.Zero : Info.Range;
				desiredVRange = disabled ? WDist.Zero : Info.MaximumVerticalOffset;
				cachedDisabled = disabled;
			}

			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange || desiredVRange != cachedVRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				cachedVRange = desiredVRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
			}

			if (!IsTraitDisabled && --ticks < 0)
			{
				CollectCash();

				ticks = Info.Delay;
			}
		}

		void CollectCash()
		{
			var cash = 0;

			foreach (var trait in collectables)
			{
				if (!trait.IsTraitDisabled)
					cash += trait.Info.Value;
			}

			if (Info.ShowTicks && self.Owner.IsAlliedWith(self.World.RenderPlayer))
				self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(cash), 30)));

			self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(cash);
		}

		void ActorEntered(Actor a)
		{
			if (a == self || a.Disposed || self.Disposed)
				return;

			var relationship = self.Owner.RelationshipWith(a.Owner);
			if (!Info.ValidRelationships.HasRelationship(relationship))
				return;

			var cc = a.TraitsImplementing<CashCollectable>().Where(t => t.Info.Types.Overlaps(Info.Type));
			foreach (var trait in cc)
				collectables.Add(trait);
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (produced.OccupiesSpace == null)
				return;

			if (IsTraitDisabled)
				return;

			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				var relationship = self.Owner.RelationshipWith(produced.Owner);
				if (!Info.ValidRelationships.HasRelationship(relationship))
					return;

				var cc = produced.TraitsImplementing<CashCollectable>().Where(t => t.Info.Types.Overlaps(Info.Type));
				foreach (var trait in cc)
					collectables.Add(trait);
			}
		}

		void ActorExited(Actor a)
		{
			if (a == self || a.Disposed || self.Disposed)
				return;

			var relationship = self.Owner.RelationshipWith(a.Owner);
			if (!Info.ValidRelationships.HasRelationship(relationship))
				return;

			var cc = a.TraitsImplementing<CashCollectable>().Where(t => t.Info.Types.Overlaps(Info.Type));
			foreach (var trait in cc)
				collectables.Remove(trait);
		}
	}
}
