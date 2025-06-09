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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.Player)]
	public class CrateSpawnerInfo : PausableConditionalTraitInfo
	{
		[Desc("Minimum number of crates.")]
		public readonly int Minimum = 1;

		[Desc("Maximum number of crates.")]
		public readonly int Maximum = 255;

		[Desc("Average time (ticks) between crate spawn.")]
		public readonly int SpawnInterval = 4500;

		[Desc("Delay (in ticks) before the first crate spawns.")]
		public readonly int InitialSpawnDelay = 0;

		[Desc("Which terrain types can we drop on?")]
		public readonly HashSet<string> ValidGround = ["Clear", "Rough", "Road", "Ore", "Beach"];

		[Desc("Which terrain types count as water?")]
		public readonly HashSet<string> ValidWater = ["Water"];

		[Desc("Chance of generating a water crate instead of a land crate.")]
		public readonly int WaterChance = 20;

		[ActorReference]
		[Desc("Crate actors to drop.")]
		public readonly string[] CrateActors = ["crate"];

		[Desc("Chance of each crate actor spawning.")]
		public readonly int[] CrateActorShares = [10];

		[ActorReference]
		[Desc("If a DeliveryAircraft: is specified, then this actor will deliver crates.")]
		public readonly string DeliveryAircraft = null;

		[Desc("Number of facings that the delivery aircraft may approach from.")]
		public readonly int QuantizedFacings = 32;

		[Desc("Spawn and remove the plane this far outside the map.")]
		public readonly WDist Cordon = new(5120);

		public override object Create(ActorInitializer init) { return new CrateSpawner(init.Self, this); }
	}

	public class CrateSpawner : PausableConditionalTrait<CrateSpawnerInfo>, ITick
	{
		readonly Actor self;
		int crates;
		int ticks;

		public CrateSpawner(Actor self, CrateSpawnerInfo info)
			: base(info)
		{
			this.self = self;

			ticks = info.InitialSpawnDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (--ticks <= 0)
			{
				ticks = Info.SpawnInterval;

				var toSpawn = Math.Max(0, Info.Minimum - crates)
					+ (crates < Info.Maximum && Info.Maximum > Info.Minimum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self);
			}
		}

		void SpawnCrate(Actor self)
		{
			var inWater = self.World.SharedRandom.Next(100) < Info.WaterChance;
			var pp = ChooseDropCell(self, inWater, 100);

			if (pp == null)
				return;

			var p = pp.Value;
			var crateActor = ChooseCrateActor();

			self.World.AddFrameEndTask(w =>
			{
				if (Info.DeliveryAircraft != null)
				{
					var crate = w.CreateActor(false, crateActor, [new OwnerInit(w.WorldActor.Owner), new CrateSpawnerTraitInit(this)]);
					var dropFacing = new WAngle(1024 * self.World.SharedRandom.Next(Info.QuantizedFacings) / Info.QuantizedFacings);
					var delta = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(dropFacing));

					var altitude = self.World.Map.Rules.Actors[Info.DeliveryAircraft].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
					var target = self.World.Map.CenterOfCell(p) + new WVec(0, 0, altitude);
					var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + Info.Cordon).Length * delta / 1024;
					var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + Info.Cordon).Length * delta / 1024;

					var plane = w.CreateActor(Info.DeliveryAircraft,
					[
						new CenterPositionInit(startEdge),
						new OwnerInit(w.WorldActor.Owner),
						new FacingInit(dropFacing),
					]);

					var drop = plane.Trait<ParaDrop>();
					drop.SetLZ(p, true);
					plane.Trait<Cargo>().Load(plane, crate);

					plane.QueueActivity(false, new Fly(plane, Target.FromPos(finishEdge)));
					plane.QueueActivity(new RemoveSelf());
				}
				else
					w.CreateActor(crateActor, [new OwnerInit(w.WorldActor.Owner), new LocationInit(p), new CrateSpawnerTraitInit(this)]);
			});
		}

		CPos? ChooseDropCell(Actor self, bool inWater, int maxTries)
		{
			for (var n = 0; n < maxTries; n++)
			{
				var p = self.World.Map.ChooseRandomCell(self.World.SharedRandom);

				// Is this valid terrain?
				var terrainType = self.World.Map.GetTerrainInfo(p).Type;
				if (!(inWater ? Info.ValidWater : Info.ValidGround).Contains(terrainType))
					continue;

				// Don't drop on any actors
				if (self.World.ActorMap.GetActorsAt(p).Any())
					continue;

				return p;
			}

			return null;
		}

		string ChooseCrateActor()
		{
			var crateShares = Info.CrateActorShares;
			var n = self.World.SharedRandom.Next(crateShares.Sum());

			var cumulativeShares = 0;
			for (var i = 0; i < crateShares.Length; i++)
			{
				cumulativeShares += crateShares[i];
				if (n <= cumulativeShares)
					return Info.CrateActors[i];
			}

			return null;
		}

		public void IncrementCrates()
		{
			crates++;
		}

		public void DecrementCrates()
		{
			crates--;
		}

		protected override void TraitDisabled(Actor self)
		{
			ticks = Info.SpawnInterval;
		}
	}

	public class CrateSpawnerTraitInit : ValueActorInit<CrateSpawner>, ISingleInstanceInit
	{
		public CrateSpawnerTraitInit(CrateSpawner value)
			: base(value) { }
	}
}
