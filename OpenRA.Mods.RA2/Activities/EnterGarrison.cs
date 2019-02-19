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
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class EnterGarrison : Activity
	{
		enum EnterState { Approaching, Waiting, Entering, Exiting }
		readonly IMove move;
		readonly Color? targetLineColor;
		readonly Garrisoner garrisoner;
		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		EnterState lastState = EnterState.Approaching;
		Activity moveActivity;

		// EnterGarrison Properties
		Target garrisonableBuilding;
		Actor enterActor;
		Garrisonable enterGarrison;

		public EnterGarrison(Actor self, Target garrisonableBuilding)
		{
			// Base - Enter Properties
			move = self.Trait<IMove>();
			this.target = garrisonableBuilding;

			// EnterGarrison Properties
			this.targetLineColor = null;
			this.garrisonableBuilding = garrisonableBuilding;
			garrisoner = self.TraitsImplementing<Garrisoner>().Single();
		}

		protected bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterGarrison = targetActor.TraitOrDefault<Garrisonable>();

			// Make sure we can still enter the transport
			// (but not before, because this may stop the actor in the middle of nowhere)
			if (enterGarrison == null || enterGarrison.IsTraitDisabled || enterGarrison.IsTraitPaused || !garrisoner.Reserve(self, enterGarrison))
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected void OnCancel(Actor self) { }

		protected void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				// Make sure the target hasn't changed while entering
				// OnEnterComplete is only called if targetActor is alive
				if (targetActor != enterActor)
					return;

				if (!enterGarrison.CanLoad(enterActor, self))
					return;

				enterGarrison.Load(enterActor, self);
				w.Remove(self);

				// Preemptively cancel any activities to avoid an edge-case where successively queued
				// EnterTransports corrupt the actor state. Activities are cancelled again on unload
				self.CancelActivity();
			});
		}

		/// Base Enter Methods Below
		public override Activity Tick(Actor self)
		{
			// Update our view of the target
			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);

			// Re-acquire the target after change owner has happened.
			if (target.Type == TargetType.Invalid)
				target = Target.FromActor(target.Actor);

			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
				lastVisibleTarget = Target.FromTargetPositions(target);

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Cancel immediately if the target died while we were entering it
			if (!IsCanceled && useLastVisibleTarget && lastState == EnterState.Entering)
				Cancel(self, true);

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget && targetLineColor.HasValue)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value, false);

			// We need to wait for movement to finish before transitioning to
			// the next state or next activity
			if (moveActivity != null)
			{
				moveActivity = ActivityUtils.RunActivity(self, moveActivity);
				if (moveActivity != null)
					return this;
			}

			// Note that lastState refers to what we have just *finished* doing
			switch (lastState)
			{
				case EnterState.Approaching:
				case EnterState.Waiting:
					{
						// NOTE: We can safely cancel in this case because we know the
						// actor has finished any in-progress move activities
						if (IsCanceled)
							return NextActivity;

						// Lost track of the target
						if (useLastVisibleTarget && lastVisibleTarget.Type == TargetType.Invalid)
							return NextActivity;

						// We are not next to the target - lets fix that
						if (target.Type != TargetType.Invalid && !move.CanEnterTargetNow(self, target))
						{
							lastState = EnterState.Approaching;

							// Target lines are managed by this trait, so we do not pass targetLineColor
							var initialTargetPosition = (useLastVisibleTarget ? lastVisibleTarget : target).CenterPosition;
							moveActivity = ActivityUtils.RunActivity(self, move.MoveToTarget(self, target, initialTargetPosition));
							break;
						}

						// We are next to where we thought the target should be, but it isn't here
						// There's not much more we can do here
						if (useLastVisibleTarget || target.Type != TargetType.Actor)
							return NextActivity;

						// Are we ready to move into the target?
						if (TryStartEnter(self, target.Actor))
						{
							lastState = EnterState.Entering;
							moveActivity = ActivityUtils.RunActivity(self, move.MoveIntoTarget(self, target));
							return this;
						}

						// Subclasses can cancel the activity during TryStartEnter
						// Return immediately to avoid an extra tick's delay
						if (IsCanceled)
							return NextActivity;

						lastState = EnterState.Waiting;
						break;
					}

				case EnterState.Entering:
					{
						if (target.Type == TargetType.Invalid)
							target = Target.FromActor(target.Actor);

						// Check that we reached the requested position
						var targetPos = target.Positions.PositionClosestTo(self.CenterPosition);

						if (!IsCanceled && self.CenterPosition == targetPos && target.Type == TargetType.Actor)
							OnEnterComplete(self, target.Actor);
						else
						{ // Need to move again as we have re-aquired a target
							moveActivity = ActivityUtils.RunActivity(self, move.MoveToTarget(self, target, targetPos));
							lastState = EnterState.Approaching;
							break;
						}

						lastState = EnterState.Exiting;
						moveActivity = ActivityUtils.RunActivity(self, move.MoveIntoWorld(self, self.Location));
						break;
					}

				case EnterState.Exiting:
					return NextActivity;
			}

			return this;
		}

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			OnCancel(self);

			if (!IsCanceled && moveActivity != null && !moveActivity.Cancel(self))
				return false;

			return base.Cancel(self, keepQueue);
		}
	}
}
