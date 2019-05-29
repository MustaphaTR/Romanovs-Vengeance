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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	public class HeliDeployForGrantedCondition : Activity
	{
		readonly World w;
		readonly Aircraft aircraft;
		readonly HeliGrantConditionOnDeploy deploy;
		readonly bool canTurn;

		public HeliDeployForGrantedCondition(Actor self, HeliGrantConditionOnDeploy deploy)
		{
			w = self.World;
			this.deploy = deploy;
			aircraft = self.Trait<Aircraft>();
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
		}

		protected override void OnFirstRun(Actor self)
		{
			if (!aircraft.CanLand(w.Map.CellContaining(self.CenterPosition)))
			{
				var cells = w.Map.AllCells.Where(c => aircraft.CanLand(c)).Select(c => w.Map.CenterOfCell(c));
				var cell = w.Map.CellContaining(WorldUtils.PositionClosestTo(cells, self.CenterPosition));

				QueueChild(self, new HeliFly(self, Target.FromCell(w, cell)));
			}

			// Turn to the required facing.
			if (deploy.Info.Facing != -1 && canTurn)
				QueueChild(self, new Turn(self, deploy.Info.Facing));

			QueueChild(self, new Land(self));
		}

		public override Activity Tick(Actor self)
		{
			// Do turn first, if needed.
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			// Without this, turn for facing deploy angle will be canceled and immediately deploy!
			if (IsCanceling)
				return NextActivity;

			if (IsInterruptible)
			{
				IsInterruptible = false; // must DEPLOY from now.
				deploy.Deploy();
				return this;
			}

			// Wait for deployment
			if (deploy.DeployState == DeployState.Deploying)
				return this;

			// Failed or success, we are going to NextActivity.
			// Deploy() at the first run would have put DeployState == Deploying so
			// if we are back to DeployState.Undeployed, it means deploy failure.
			// Parent activity will see the status and will take appropriate action.
			return NextActivity;
		}
	}
}
