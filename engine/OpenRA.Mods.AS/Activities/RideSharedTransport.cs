#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	class RideSharedTransport : Enter
	{
		readonly SharedPassenger passenger;

		Actor enterActor;
		SharedCargo enterCargo;
		Aircraft enterAircraft;

		public RideSharedTransport(Actor self, in Target target, Color? targetLineColor)
			: base(self, target, targetLineColor)
		{
			passenger = self.Trait<SharedPassenger>();
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterCargo = targetActor.TraitOrDefault<SharedCargo>();
			enterAircraft = targetActor.TraitOrDefault<Aircraft>();

			// Make sure we can still enter the transport
			// (but not before, because this may stop the actor in the middle of nowhere)
			if (enterCargo == null || !passenger.Reserve(self, enterCargo))
			{
				Cancel(self, true);
				return false;
			}

			if (enterAircraft != null && !enterAircraft.AtLandAltitude)
				return false;

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				// Make sure the target hasn't changed while entering
				// OnEnterComplete is only called if targetActor is alive
				if (targetActor != enterActor)
					return;

				if (!enterCargo.CanLoad(enterActor, self))
					return;

				foreach (var inl in targetActor.TraitsImplementing<INotifyLoadCargo>())
					inl.Loading(self);

				enterCargo.Load(enterActor, self);
				w.Remove(self);

				// Preemptively cancel any activities to avoid an edge-case where successively queued
				// EnterTransports corrupt the actor state. Activities are cancelled again on unload
				self.CancelActivity();
			});
		}

		protected override void OnLastRun(Actor self)
		{
			passenger.Unreserve(self);
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			passenger.Unreserve(self);

			base.Cancel(self, keepQueue);
		}
	}
}
