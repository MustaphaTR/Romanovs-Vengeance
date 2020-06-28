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
		readonly bool moving;

		public HeliDeployForGrantedCondition(Actor self, HeliGrantConditionOnDeploy deploy, bool moving = false)
		{
			w = self.World;
			this.deploy = deploy;
			this.moving = moving;
			aircraft = self.Trait<Aircraft>();
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
		}

		protected override void OnFirstRun(Actor self)
		{
			if (!aircraft.CanLand(w.Map.CellContaining(self.CenterPosition)))
			{
				var cells = w.Map.AllCells.Where(c => aircraft.CanLand(c)).Select(c => w.Map.CenterOfCell(c));
				var cell = w.Map.CellContaining(WorldUtils.PositionClosestTo(cells, self.CenterPosition));

				QueueChild(new Fly(self, Target.FromCell(w, cell)));
			}

			// Turn to the required facing.
			if (deploy.DeployState == DeployState.Undeployed && deploy.Info.Facing != -1 && canTurn && !moving)
				QueueChild(new Turn(self, WAngle.FromFacing(deploy.Info.Facing)));

			QueueChild(new Land(self));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || (deploy.DeployState != DeployState.Deployed && moving))
				return true;

			QueueChild(new HeliDeployInner(self, deploy));
			return true;
		}
	}

	public class HeliDeployInner : Activity
	{
		readonly HeliGrantConditionOnDeploy deployment;
		bool initiated;

		public HeliDeployInner(Actor self, HeliGrantConditionOnDeploy deployment)
		{
			this.deployment = deployment;

			// Once deployment animation starts, the animation must finish.
			IsInterruptible = false;
		}

		public override bool Tick(Actor self)
		{
			// Wait for deployment
			if (deployment.DeployState == DeployState.Deploying || deployment.DeployState == DeployState.Undeploying)
				return false;

			if (initiated)
				return true;

			if (deployment.DeployState == DeployState.Undeployed)
				deployment.Deploy();
			else
				deployment.Undeploy();

			initiated = true;
			return false;
		}
	}
}
