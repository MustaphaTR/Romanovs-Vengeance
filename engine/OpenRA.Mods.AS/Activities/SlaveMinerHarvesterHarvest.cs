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
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class SlaveMinerHarvesterHarvest : Activity
	{
		readonly SlaveMinerHarvester harv;
		readonly SlaveMinerHarvesterInfo harvInfo;
		readonly Mobile mobile;
		readonly ResourceClaimLayer claimLayer;
		readonly Transforms transforms;
		CPos deployDestPosition;
		readonly CPos? avoidCell;
		int cellRange;

		public SlaveMinerHarvesterHarvest(Actor self)
		{
			harv = self.Trait<SlaveMinerHarvester>();
			harvInfo = self.Info.TraitInfo<SlaveMinerHarvesterInfo>();
			mobile = self.Trait<Mobile>();
			claimLayer = self.World.WorldActor.TraitOrDefault<ResourceClaimLayer>();
			transforms = self.Trait<Transforms>();
			ChildHasPriority = false;
		}

		public SlaveMinerHarvesterHarvest(Actor self, CPos avoidCell)
			: this(self)
		{
			this.avoidCell = avoidCell;
		}

		void ScanAndMove(Actor self, out MiningState state)
		{
			var closestHarvestablePosition = ClosestHarvestablePos(self, harvInfo.LongScanRadius);

			// No suitable resource field found.
			// We only have to wait for resource to regen.
			if (!closestHarvestablePosition.HasValue)
			{
				var randFrames = self.World.SharedRandom.Next(100, 175);

				// Avoid creating an activity cycle
				QueueChild(new Wait(randFrames));
				state = MiningState.Scan;
			}

			// ... Don't claim resource layer here. Slaves will claim by themselves.

			// If not given a direct order, assume ordered to the first resource location we find:
			if (!harv.LastOrderLocation.HasValue)
				harv.LastOrderLocation = closestHarvestablePosition;

			// Calculate best depoly position.
			var deployPosition = CalcTransformPosition(self, closestHarvestablePosition.Value);

			// Just sit there until we can. Won't happen unless the map is filled with units.
			if (deployPosition == null)
			{
				QueueChild(new Wait(harvInfo.KickDelay));
				state = MiningState.Scan;
			}

			// TODO: The harvest-deliver-return sequence is a horrible mess of duplicated code and edge-cases
			var notify = self.TraitsImplementing<INotifyHarvestAction>();
			foreach (var n in notify)
				n.MovingToResources(self, deployPosition.Value);

			state = MiningState.Moving;

			// When it reached the best position, we will let it do this activity again
			deployDestPosition = deployPosition.Value;
			cellRange = 2;
			var moveActivity = mobile.MoveTo(deployPosition.Value, cellRange);
			moveActivity.Queue(this);
			QueueChild(moveActivity);
		}

		void CheckIfReachedBestLocation(Actor self, out MiningState state)
		{
			if ((self.Location - deployDestPosition).LengthSquared <= cellRange * cellRange)
			{
				ChildActivity.Cancel(self);
				state = MiningState.TryDeploy;
			}
			else
			{
				state = MiningState.Moving;
			}
		}

		void TryDeploy(out MiningState state)
		{
			if (!transforms.CanDeploy())
			{
				// If we can't deploy, go back to scan state so that we scan try deploy again.
				state = MiningState.Scan;
			}
			else
			{
				IsInterruptible = false;

				var transformsActivity = transforms.GetTransformActivity();
				QueueChild(transformsActivity);

				state = MiningState.Deploying;
			}
		}

		void Deploying(out MiningState state)
		{
			// deploy failure.
			if (!transforms.CanDeploy())
			{
				QueueChild(new Wait(15));
				state = MiningState.Scan;
			}
			else
			{
				state = MiningState.Mining;
			}
		}

		Activity Mining(out MiningState state)
		{
			// Let the harvester become idle so it can shoot enemies.
			// Tick in SpawnerHarvester trait will kick activity back to KickTick.
			state = MiningState.Packaging;
			return ChildActivity;
		}

		void UndeployingCheck(Actor self, out MiningState state)
		{
			var closestHarvestablePosition = ClosestHarvestablePos(self, harvInfo.KickScanRadius);
			if (closestHarvestablePosition.HasValue)
			{
				// I may stay mining.
				state = MiningState.Mining;
			}
			else
			{
				// get going
				harv.LastOrderLocation = null;
				CheckWheteherNeedUndeployAndGo(out state);
			}
		}

		Activity CheckWheteherNeedUndeployAndGo(out MiningState state)
		{
			// QueueChild(new DeployForGrantedCondition(self, deploy));
			state = MiningState.Scan;
			return this;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			switch (harv.MiningState)
			{
				case MiningState.Scan:
					ScanAndMove(self, out harv.MiningState);
					break;
				case MiningState.Moving:
					CheckIfReachedBestLocation(self, out harv.MiningState);
					break;
				case MiningState.TryDeploy:
					TryDeploy(out harv.MiningState);
					break;
				case MiningState.Deploying:
					Deploying(out harv.MiningState);
					break;
				case MiningState.Mining:
					Mining(out harv.MiningState);
					break;
				case MiningState.Packaging:
					UndeployingCheck(self, out harv.MiningState);
					break;
			}

			return TickChild(self);
		}

		// Find a nearest Transformable position from harvestablePos
		CPos? CalcTransformPosition(Actor self, CPos harvestablePos)
		{
			var transformActorInfo = self.World.Map.Rules.Actors[transforms.Info.IntoActor];
			var transformBuildingInfo = transformActorInfo.TraitInfoOrDefault<BuildingInfo>();

			// FindTilesInAnnulus gives sorted cells by distance :) Nice.
			foreach (var tile in self.World.Map.FindTilesInAnnulus(harvestablePos, 0, harvInfo.DeployScanRadius))
				if (mobile.CanEnterCell(tile) && self.World.CanPlaceBuilding(tile + transforms.Info.Offset, transformActorInfo, transformBuildingInfo, self))
					return tile;

			// Try broader search if unable to find deploy location
			foreach (var tile in self.World.Map.FindTilesInAnnulus(harvestablePos, harvInfo.DeployScanRadius, harvInfo.LongScanRadius))
				if (mobile.CanEnterCell(tile) && self.World.CanPlaceBuilding(tile + transforms.Info.Offset, transformActorInfo, transformBuildingInfo, self))
					return tile;

			return null;
		}

		/// <summary>
		/// Using LastOrderLocation and self.Location as starting points,
		/// perform A* search to find the nearest accessible and harvestable cell.
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self, int searchRadius)
		{
			if (harv.CanHarvestCell(self.Location) && claimLayer.CanClaimCell(self, self.Location))
				return self.Location;

			// Determine where to search from and how far to search:
			var searchFromLoc = harv.LastOrderLocation ?? self.Location;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Find any harvestable resources:
			// var passable = (uint)mobileInfo.GetMovementClass(self.World.Map.Rules.TileSet);
			var path = mobile.PathFinder.FindPathToTargetCellByPredicate(
				self,
				new[] { searchFromLoc, self.Location },
				loc =>
					harv.CanHarvestCell(loc) &&
					claimLayer.CanClaimCell(self, loc),
				BlockedByActor.All,
				loc =>
				{
					if ((avoidCell.HasValue && loc == avoidCell.Value) ||
						(loc - self.Location).LengthSquared > searchRadiusSquared)
						return int.MaxValue;

					return 0;
				});

			if (path.Count > 0)
				return path[0];

			return null;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}
	}
}
