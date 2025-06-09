#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Dogs use this attack model.")]
	class AttackLeapASInfo : AttackFrontalInfo
	{
		[Desc("Leap speed (in units/tick).")]
		public readonly WDist Speed = new(426);

		public readonly WAngle Angle = WAngle.FromDegrees(20);

		[Desc("Types of damage that this trait causes. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("The condition to apply to the target while leaping. Must be included in the target actor's ExternalConditions list.")]
		public readonly string LeapTargetCondition = null;

		public override object Create(ActorInitializer init) { return new AttackLeapAS(init.Self, this); }
	}

	class AttackLeapAS : AttackFrontal
	{
		readonly Barrel barrel;
		public readonly AttackLeapASInfo LeapInfo;

		INotifyAttack[] notifyAttacks;
		(Actor Actor, int Token) targetCondition;

		public AttackLeapAS(Actor self, AttackLeapASInfo info)
			: base(self, info)
		{
			LeapInfo = info;
			barrel = new Barrel { Offset = WVec.Zero, Yaw = WAngle.Zero };
		}

		protected override void Created(Actor self)
		{
			notifyAttacks = self.TraitsImplementing<INotifyAttack>().ToArray();

			base.Created(self);
		}

		public override void DoAttack(Actor self, in Target target)
		{
			if (target.Type != TargetType.Actor || !CanAttack(self, target))
				return;

			var a = ChooseArmamentsForTarget(target, true).FirstOrDefault();
			if (a == null)
				return;

			if (!target.IsInRange(self.CenterPosition, a.MaxRange()))
				return;

			self.CancelActivity();

			foreach (var na in notifyAttacks)
				na.PreparingAttack(self, target, a, barrel);

			if (LeapInfo.LeapTargetCondition != null)
			{
				// Lambdas can't use 'in' variables, so capture a copy for later
				var delayedTarget = target;
				var external = target.Actor.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == LeapInfo.LeapTargetCondition && t.CanGrantCondition(self));

				if (external != null)
					targetCondition = (target.Actor, external.GrantCondition(target.Actor, self));
			}

			self.QueueActivity(new LeapAS(self, target.Actor, a, this));
		}

		public void NotifyAttacking(Actor self, Target target, Armament a)
		{
			foreach (var na in notifyAttacks)
				na.Attacking(self, target, a, barrel);
		}

		public void FinishAttacking(Actor self)
		{
			if (targetCondition.Actor != null && !targetCondition.Actor.IsDead)
			{
				foreach (var external in targetCondition.Actor.TraitsImplementing<ExternalCondition>())
					if (external.TryRevokeCondition(targetCondition.Actor, self, targetCondition.Token))
						break;
			}
		}
	}
}
