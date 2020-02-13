#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class InfectOld : Enter
	{
		readonly InfectorOld infector;
		readonly Target target;

		public InfectOld(Actor self, Target target, InfectorOld infector)
			: base(self, target, Color.Red)
		{
			this.target = target;
			this.infector = infector;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (infector.IsTraitDisabled)
					return;

				var infectable = targetActor.TraitOrDefault<InfectableOld>();
				if (infectable == null || infectable.Infector != null)
					return;

				w.Remove(self);

				infectable.Infector = self;
				infectable.InfectorTrait = infector;
				infectable.FirepowerMultipliers = self.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier()).ToArray();
				infectable.Ticks = infector.Info.DamageInterval;
				infectable.GrantCondition(targetActor);
				infectable.RevokeCondition(targetActor, true);
			});
		}

		protected override void OnLastRun(Actor self)
		{
			CancelInfection(self);
			base.OnLastRun(self);
		}

		protected override void OnActorDispose(Actor self)
		{
			CancelInfection(self);
			base.OnActorDispose(self);
		}

		void CancelInfection(Actor self)
		{
			if (target.Type != TargetType.Actor)
				return;

			if (target.Actor.IsDead)
				return;

			var infectable = target.Actor.TraitOrDefault<InfectableOld>();
			if (infectable == null || infectable.Infector != null)
				return;

			infectable.RevokeCondition(target.Actor, true);
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			if (infector.IsTraitDisabled)
				return false;

			if (targetActor.IsDead)
				return false;

			var infectable = targetActor.TraitOrDefault<InfectableOld>();
			if (infectable == null || infectable.Infector != null)
				return false;

			infectable.GrantCondition(targetActor, true);

			return true;
		}
	}
}
