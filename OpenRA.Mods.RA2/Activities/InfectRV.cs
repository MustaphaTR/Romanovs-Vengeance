#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class InfectRV : Enter
	{
		readonly AttackInfectRV infector;
		readonly AttackInfectRVInfo info;
		readonly Target target;

		bool jousting;

		public InfectRV(Actor self, Target target, AttackInfectRV infector, AttackInfectRVInfo info, Color? targetLineColor)
			: base(self, target, targetLineColor)
		{
			this.target = target;
			this.infector = infector;
			this.info = info;
		}

		protected override void OnFirstRun(Actor self)
		{
			infector.IsAiming = true;
		}

		protected override void OnLastRun(Actor self)
		{
			infector.IsAiming = false;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (infector.IsTraitDisabled)
					return;

				if (jousting)
				{
					infector.RevokeJoustCondition(self);
					jousting = false;
				}

				infector.DoAttack(self, target);

				var infectable = targetActor.TraitOrDefault<InfectableRV>();
				if (infectable == null || infectable.IsTraitDisabled || infectable.Infector != null)
					return;

				w.Remove(self);

				infectable.Infector = Tuple.Create(self, infector, info);
				infectable.FirepowerMultipliers = self.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier()).ToArray();
				infectable.Ticks = info.DamageInterval;
				infectable.GrantCondition(targetActor);
				infectable.RevokeCondition(targetActor, self);
			});
		}

		void CancelInfection(Actor self)
		{
			if (jousting)
			{
				infector.RevokeJoustCondition(self);
				jousting = false;
			}

			if (target.Type != TargetType.Actor)
				return;

			if (target.Actor.IsDead)
				return;

			var infectable = target.Actor.TraitOrDefault<InfectableRV>();
			if (infectable == null || infectable.IsTraitDisabled || infectable.Infector != null)
				return;

			infectable.RevokeCondition(target.Actor, self);
		}

		bool IsValidInfection(Actor self, Actor targetActor)
		{
			if (infector.IsTraitDisabled)
				return false;

			if (targetActor.IsDead)
				return false;

			if (!target.IsValidFor(self) || !infector.HasAnyValidWeapons(target))
				return false;

			var infectable = targetActor.TraitOrDefault<InfectableRV>();
			if (infectable == null || infectable.IsTraitDisabled || infectable.Infector != null)
				return false;

			return true;
		}

		bool CanStartInfect(Actor self, Actor targetActor)
		{
			if (!IsValidInfection(self, targetActor))
				return false;

			// IsValidInfection validated the lookup, no need to check here.
			var infectable = targetActor.Trait<InfectableRV>();
			return infectable.TryStartInfecting(targetActor, self);
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			var canStartInfect = CanStartInfect(self, targetActor);
			if (canStartInfect == false)
			{
				CancelInfection(self);
				Cancel(self, true);
			}

			// Can't leap yet
			if (infector.Armaments.All(a => a.IsReloading))
				return false;

			return true;
		}

		protected override void TickInner(Actor self, in Target target, bool targetIsDeadOrHiddenActor)
		{
			if (target.Type != TargetType.Actor || !IsValidInfection(self, target.Actor))
			{
				CancelInfection(self);
				Cancel(self, true);
				return;
			}

			if (!jousting && !IsCanceling && (self.CenterPosition - target.CenterPosition).Length < info.JoustRange.Length)
			{
				jousting = true;
				infector.GrantJoustCondition(self);
			}
		}
	}
}
