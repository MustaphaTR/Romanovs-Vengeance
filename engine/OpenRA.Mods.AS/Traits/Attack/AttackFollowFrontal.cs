#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class AttackFollowFrontalInfo : AttackFollowInfo
	{
		[Desc("Actor will turn directly to target regardless the FacingTolerance to catch its target in full fire angle.")]
		public readonly bool MustFaceTarget = false;

		public override object Create(ActorInitializer init) { return new AttackFollowFrontal(init.Self, this); }
	}

	public class AttackFollowFrontal : AttackBase, INotifyOwnerChanged, IOverrideAutoTarget, INotifyStanceChanged
	{
		public new readonly AttackFollowFrontalInfo Info;
		public Target RequestedTarget { get; private set; }
		public Target OpportunityTarget { get; private set; }

		Mobile mobile;
		AutoTarget autoTarget;
		bool requestedForceAttack;
		Activity requestedTargetPresetForActivity;
		bool opportunityForceAttack;
		bool opportunityTargetIsPersistentTarget;

		public void SetRequestedTarget(Actor self, in Target target, bool isForceAttack = false)
		{
			RequestedTarget = target;
			requestedForceAttack = isForceAttack;
			requestedTargetPresetForActivity = null;
		}

		public void ClearRequestedTarget()
		{
			if (Info.PersistentTargeting)
			{
				OpportunityTarget = RequestedTarget;
				opportunityForceAttack = requestedForceAttack;
				opportunityTargetIsPersistentTarget = true;
			}

			RequestedTarget = Target.Invalid;
			requestedTargetPresetForActivity = null;
		}

		public AttackFollowFrontal(Actor self, AttackFollowFrontalInfo info)
			: base(self, info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			mobile = self.TraitOrDefault<Mobile>();
			autoTarget = self.TraitOrDefault<AutoTarget>();
			base.Created(self);
		}

		protected bool CanAimAtTarget(Actor self, in Target target, bool forceAttack)
		{
			if (target.Type == TargetType.Actor && !target.Actor.CanBeViewedByPlayer(self.Owner))
				return false;

			if (target.Type == TargetType.FrozenActor && !target.FrozenActor.IsValid)
				return false;

			var pos = self.CenterPosition;
			var armaments = ChooseArmamentsForTarget(target, forceAttack);
			foreach (var a in armaments)
				if (target.IsInRange(pos, a.MaxRange()) && (a.Weapon.MinRange == WDist.Zero || !target.IsInRange(pos, a.Weapon.MinRange))
					&& TargetInFiringArc(self, target, Info.FacingTolerance))
					return true;

			return false;
		}

		protected override void Tick(Actor self)
		{
			if (IsTraitDisabled)
			{
				RequestedTarget = OpportunityTarget = Target.Invalid;
				opportunityTargetIsPersistentTarget = false;
			}

			if (requestedTargetPresetForActivity != null)
			{
				// RequestedTarget was set by OnQueueAttackActivity in preparation for a queued activity
				// requestedTargetPresetForActivity will be cleared once the activity starts running and calls UpdateRequestedTarget
				if (self.CurrentActivity != null && self.CurrentActivity.NextActivity == requestedTargetPresetForActivity)
				{
					RequestedTarget = RequestedTarget.Recalculate(self.Owner, out _);
				}

				// Requested activity has been canceled
				else
					ClearRequestedTarget();
			}

			// Can't fire on anything
			if (mobile != null && !mobile.CanInteractWithGroundLayer(self))
				return;

			if (RequestedTarget.Type != TargetType.Invalid)
			{
				IsAiming = CanAimAtTarget(self, RequestedTarget, requestedForceAttack);
				if (IsAiming)
					DoAttack(self, RequestedTarget);
			}
			else
			{
				IsAiming = false;

				if (OpportunityTarget.Type != TargetType.Invalid)
					IsAiming = CanAimAtTarget(self, OpportunityTarget, opportunityForceAttack);

				if (!IsAiming && Info.OpportunityFire && autoTarget != null &&
					!autoTarget.IsTraitDisabled && autoTarget.Stance >= UnitStance.Defend)
				{
					OpportunityTarget = autoTarget.ScanForTarget(self, false, false);
					opportunityForceAttack = false;
					opportunityTargetIsPersistentTarget = false;

					if (OpportunityTarget.Type != TargetType.Invalid)
						IsAiming = CanAimAtTarget(self, OpportunityTarget, opportunityForceAttack);
				}

				if (IsAiming)
					DoAttack(self, OpportunityTarget);
			}

			base.Tick(self);
		}

		public override Activity GetAttackActivity(
			Actor self, AttackSource source, in Target newTarget, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			return new AttackFrontalFollowActivity(self, newTarget, allowMove, forceAttack, targetLineColor);
		}

		public override void OnResolveAttackOrder(Actor self, Activity activity, in Target target, bool queued, bool forceAttack)
		{
			// We can improve responsiveness for turreted actors by preempting
			// the last order (usually a move) and setting the target immediately
			if (!queued)
			{
				RequestedTarget = target;
				requestedForceAttack = forceAttack;
				requestedTargetPresetForActivity = activity;
			}
		}

		public override void OnStopOrder(Actor self)
		{
			RequestedTarget = OpportunityTarget = Target.Invalid;
			opportunityTargetIsPersistentTarget = false;
			base.OnStopOrder(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			RequestedTarget = OpportunityTarget = Target.Invalid;
			opportunityTargetIsPersistentTarget = false;
		}

		bool IOverrideAutoTarget.TryGetAutoTargetOverride(Actor self, out Target target)
		{
			if (RequestedTarget.Type != TargetType.Invalid)
			{
				target = RequestedTarget;
				return true;
			}

			if (opportunityTargetIsPersistentTarget && OpportunityTarget.Type != TargetType.Invalid)
			{
				target = OpportunityTarget;
				return true;
			}

			target = Target.Invalid;
			return false;
		}

		void INotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel opportunity targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance || opportunityForceAttack)
				return;

			if (OpportunityTarget.Type == TargetType.Actor)
			{
				var a = OpportunityTarget.Actor;
				if (!autoTarget.HasValidTargetPriority(self, a.Owner, a.GetEnabledTargetTypes()))
					OpportunityTarget = Target.Invalid;
			}
			else if (OpportunityTarget.Type == TargetType.FrozenActor)
			{
				var fa = OpportunityTarget.FrozenActor;
				if (!autoTarget.HasValidTargetPriority(self, fa.Owner, fa.TargetTypes))
					OpportunityTarget = Target.Invalid;
			}
		}

		protected override bool CanAttack(Actor self, in Target target)
		{
			if (!base.CanAttack(self, target))
				return false;

			return TargetInFiringArc(self, target, Info.FacingTolerance);
		}
	}
}
