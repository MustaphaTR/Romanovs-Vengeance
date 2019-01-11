#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.RA2.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	class Infect : Enter
	{
		readonly Actor target;

		public Infect(Actor self, Actor target)
			: base(self, target, EnterBehaviour.Exit)
		{
			this.target = target;
		}

		protected override void OnInside(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (target.IsDead)
					return;

                var infectable = target.Trait<Infectable>();
                if (infectable.Infector != null)
                    return;

                w.Remove(self);

                var infector = self.Trait<Infector>();
                infectable.Infector = self;
                infectable.InfectorTrait = infector;
                infectable.Ticks = infector.Info.DamageInterval;
                infectable.GrantCondition(target);
                infectable.RevokeCondition(target, true);
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
            if (!target.IsDead)
            {
                var infectable = target.Trait<Infectable>();
                if (infectable.Infector != null)
                    return;

                infectable.RevokeCondition(target, true);
            }
        }

        protected override bool TryStartEnter(Actor self)
        {
            var infectable = target.Trait<Infectable>();
            if (infectable.Infector != null)
                return false;

            if (infectable.Infector != null)
                return false;

            infectable.GrantCondition(target, true);

            return true;
        }
    }
}
