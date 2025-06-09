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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor provides Ranged GPS.")]
	public class RangedGpsProviderInfo : ConditionalTraitInfo
	{
		[Desc("Range for the GPS effect to apply.")]
		public readonly WDist Range = WDist.FromCells(5);

		public override object Create(ActorInitializer init) { return new RangedGpsProvider(init.Self, this); }
	}

	public class RangedGpsProvider : ConditionalTrait<RangedGpsProviderInfo>, ITick, INotifyAddedToWorld,
		INotifyRemovedFromWorld, INotifyOwnerChanged, INotifyKilled, INotifyActorDisposing
	{
		readonly Actor self;
		readonly List<Actor> actorsInRange = new();
		protected RangedGpsWatcher Watcher { get; private set; }
		int proximityTrigger;
		WPos prevPosition;

		public RangedGpsProvider(Actor self, RangedGpsProviderInfo info)
			: base(info)
		{
			this.self = self;
			Watcher = self.Owner.PlayerActor.Trait<RangedGpsWatcher>();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (!IsTraitDisabled)
				TraitEnabled(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!IsTraitDisabled)
				TraitDisabled(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (!IsTraitDisabled)
				TraitDisabled(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (!IsTraitDisabled)
				TraitDisabled(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!IsTraitDisabled)
				TraitDisabled(self);

			Watcher = newOwner.PlayerActor.Trait<RangedGpsWatcher>();
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld || self.CenterPosition == prevPosition)
				return;

			self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, self.CenterPosition, Info.Range, WDist.Zero);
			prevPosition = self.CenterPosition;
		}

		void ActorEntered(Actor other)
		{
			var dot = other.TraitOrDefault<RangedGpsDot>();
			if (dot != null)
			{
				actorsInRange.Add(other);
				dot.Providers.Add(self);
			}
		}

		void ActorLeft(Actor other)
		{
			if (other.IsDead)
			{
				actorsInRange.Remove(other);
				return;
			}

			var dot = other.TraitOrDefault<RangedGpsDot>();
			if (dot != null)
			{
				actorsInRange.Remove(other);
				dot.Providers.Remove(self);
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			Watcher.ActivateGps(this, self.Owner);
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(self.CenterPosition, Info.Range, WDist.Zero, ActorEntered, ActorLeft);
		}

		protected override void TraitDisabled(Actor self)
		{
			Watcher.DeactivateGps(this, self.Owner);
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
			foreach (var a in actorsInRange)
				if (!a.IsDead)
					a.Trait<RangedGpsDot>().Providers.Remove(self);

			actorsInRange.Clear();
		}
	}
}
