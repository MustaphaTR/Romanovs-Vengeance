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
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class SlaveMinerMasterInfo : SpawnerHarvestResourceInfo, Requires<TransformsInfo>
	{
		[Desc("When deployed, use this scan radius.")]
		public readonly int ShortScanRadius = 8;

		[Desc("Look this far when Searching for Ore (in Cells)")]
		public readonly int LongScanRadius = 24;

		[Desc("Look this far when trying to find a deployable position from the target resource patch")]
		public readonly int DeployScanRadius = 8;

		[Desc("If no resource within range at each kick, move.")]
		public readonly int KickScanRadius = 5;

		[Desc("If the SlaveMiner is idle for this long, he'll try to look for ore again at SlaveMinerShortScan range to find ore and wake up (in ticks)")]
		public readonly int KickDelay = 20;

		[Desc("Play this sound when the slave is freed")]
		public readonly string FreeSound = null;

		public override object Create(ActorInitializer init)
		{
			return new SlaveMinerMaster(init, this);
		}
	}

	public class SlaveMinerMaster : BaseSpawnerMaster, INotifyTransform,
		INotifyBuildingPlaced, ITick, IIssueOrder, IResolveOrder
	{
		const string OrderID = "SlaveMinerMasterHarvest";

		public MiningState MiningState = MiningState.Mining;
		public CPos? LastOrderLocation = null;
		readonly SlaveMinerMasterInfo info;
		readonly IResourceLayer resLayer;
		readonly bool allowKicks = true; // allow kicks?
		readonly Transforms transforms;
		int respawnTicks = 0;
		int kickTicks;
		bool force = false;
		CPos? forceMovePos = null;

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SlaveMinerHarvestOrderTargeter<SlaveMinerMasterInfo>(OrderID); }
		}

		public SlaveMinerMaster(ActorInitializer init, SlaveMinerMasterInfo info)
			: base(init, info)
		{
			this.info = info;
			resLayer = init.Self.World.WorldActor.Trait<ResourceLayer>();
			transforms = init.Self.Trait<Transforms>();
		}

		#region Transform
		public void AfterTransform(Actor toActor)
		{
			// When transform complete, assign the slaves to this transform actor
			var harvesterMaster = toActor.Trait<SlaveMinerHarvester>();
			foreach (var se in SlaveEntries)
			{
				var slave = se.Actor;
				se.SpawnerSlave.LinkMaster(slave, toActor, harvesterMaster);
				se.SpawnerSlave.Stop(slave);
				if (!slave.IsDead)
					slave.QueueActivity(new Follow(slave, Target.FromActor(toActor), WDist.FromCells(1), WDist.FromCells(3), null));
			}

			harvesterMaster.SlaveEntries = SlaveEntries;
			if (force)
			{
				harvesterMaster.LastOrderLocation = forceMovePos;
				toActor.QueueActivity(new SlaveMinerHarvesterHarvest(toActor));
			}
			else
			{
				toActor.QueueActivity(new SlaveMinerHarvesterHarvest(toActor));
			}
		}

		public void BeforeTransform(Actor self) { }

		public void OnTransform(Actor self) { }

		#endregion

		public bool CanHarvestCell(CPos cell)
		{
			// Resources only exist in the ground layer
			if (cell.Layer != 0)
				return false;

			var resType = resLayer.GetResource(cell).Type;
			if (resType == null)
				return false;

			// Can the harvester collect this kind of resource?
			return info.Resources.Contains(resType);
		}

		void Launch(Actor master, BaseSpawnerSlaveEntry slaveEntry)
		{
			var slave = slaveEntry.Actor;

			SpawnIntoWorld(master, slave, master.CenterPosition);
		}

		public override void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			base.SpawnIntoWorld(self, slave, centerPosition);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				slave.QueueActivity(new FindAndDeliverResources(slave, self.Location));
			});
		}

		void HandleSpawnerHarvest(Actor self, Order order)
		{
			// Maybe player have a better idea, let's move
			ForceMove(self.World.Map.CellContaining(order.Target.CenterPosition));
		}

		public void ForceMove(CPos pos)
		{
			force = true;
			forceMovePos = pos;
			transforms.DeployTransform(false);
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		protected override void Killed(Actor self, AttackInfo e)
		{
			base.Killed(self, e);

			if (!string.IsNullOrEmpty(info.FreeSound))
			{
				Game.Sound.Play(SoundType.World, info.FreeSound, self.CenterPosition);
			}
		}

		public void BuildingPlaced(Actor self) { }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == OrderID)
			{
				HandleSpawnerHarvest(self, order);
			}
			else if (order.OrderString == "Stop" || order.OrderString == "Move")
			{
				MiningState = MiningState.Scan;
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == OrderID)
				return new Order(order.OrderID, self, target, queued);
			return null;
		}

		public void TickIdle(Actor self)
		{
			if (allowKicks && self.IsIdle)
				kickTicks--;
			else
				kickTicks = info.KickDelay;

			if (kickTicks <= 0)
			{
				kickTicks = info.KickDelay;
				MiningState = MiningState.Packaging;
				self.QueueActivity(new SlaveMinerMasterHarvest(self));
			}
		}

		public BaseSpawnerSlaveEntry[] GetSlaves()
		{
			return SlaveEntries;
		}

		void ITick.Tick(Actor self)
		{
			respawnTicks--;
			if (respawnTicks > 0)
				return;

			if (MiningState != MiningState.Mining)
				return;

			Replenish(self, SlaveEntries);

			var hasInvalidEntry = false;
			foreach (var slaveEntry in SlaveEntries)
			{
				if (!slaveEntry.IsValid)
				{
					hasInvalidEntry = true;
				}
				else if (!slaveEntry.Actor.IsInWorld)
				{
					Launch(self, slaveEntry);
				}
			}

			if (hasInvalidEntry)
			{
				respawnTicks = Info.RespawnTicks;
			}
		}
	}
}
