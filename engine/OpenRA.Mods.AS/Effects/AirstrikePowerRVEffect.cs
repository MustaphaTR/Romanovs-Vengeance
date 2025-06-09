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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Effects
{
	public class AirstrikePowerRVEffect : IEffect
	{
		readonly AirstrikePowerRVInfo info;
		readonly Player owner;
		readonly World world;
		readonly WPos target;
		readonly WPos finishEdge;
		readonly WRot attackRotation;
		readonly int level;

		int ticks = 0;

		readonly Actor[] aircraft;
		Actor camera = null;
		Beacon beacon = null;
		bool spawned = false;
		bool enteredRange = false;

		public AirstrikePowerRVEffect(World world, Player p, WPos target, WPos startEdge, WPos finishEdge,
			WRot attackRotation, int altitude, int level, Actor[] aircraft, AirstrikePowerRV power, AirstrikePowerRVInfo info)
		{
			this.info = info;
			this.world = world;
			owner = p;
			this.target = target;
			this.finishEdge = finishEdge;
			this.attackRotation = attackRotation;
			this.level = level;
			this.aircraft = aircraft;
			ticks = 0;

			if (info.DisplayBeacon)
			{
				var distance = (target - startEdge).HorizontalLength;
				var distanceTestActor = aircraft.Last();

				beacon = new Beacon(
					owner,
					target - new WVec(0, 0, altitude),
					info.BeaconPaletteIsPlayerPalette,
					info.BeaconPalette,
					info.BeaconImage,
					info.BeaconPosters.First(bp => bp.Key == level).Value,
					info.BeaconPosterPalette,
					info.BeaconSequence,
					info.ArrowSequence,
					info.CircleSequence,
					info.ClockSequence,
					() => FractionComplete(distanceTestActor, target, distance),
					info.BeaconDelay);

				world.AddFrameEndTask(w => w.Add(beacon));
			}
		}

		void IEffect.Tick(World world)
		{
			if (ticks < info.ActivationDelay)
			{
				ticks++;

				return;
			}

			if (!spawned)
			{
				world.AddFrameEndTask(w =>
				{
					var j = 0;
					var squadSize = info.SquadSizes.First(ss => ss.Key == level).Value;
					for (var i = -squadSize / 2; i <= squadSize / 2; i++)
					{
						// Even-sized squads skip the lead plane
						if (i == 0 && (squadSize & 1) == 0)
							continue;

						// Includes the 90 degree rotation between body and world coordinates
						var so = info.SquadOffset;
						var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);

						var a = aircraft[j++];
						if (a.IsDead)
							continue;

						world.Add(a);

						a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
						a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
						a.QueueActivity(new RemoveSelf());
					}
				});

				spawned = true;
			}

			var onMap = aircraft.Where(p => p.IsInWorld && !p.IsDead).ToArray();

			if (!enteredRange && Array.Exists(onMap, p => (p.OccupiesSpace.CenterPosition - target).Length < info.BeaconDistanceOffset.Length))
			{
				OnEnterRange();
				enteredRange = true;
			}

			if (onMap.Length <= 0 || (enteredRange && Array.TrueForAll(onMap, p => (p.OccupiesSpace.CenterPosition - target).Length > info.BeaconDistanceOffset.Length)))
			{
				OnExitRange();
				world.AddFrameEndTask(w => w.Remove(this));
			}
		}

		float FractionComplete(Actor distanceTestActor, WPos target, int distance)
		{
			if (info.ActivationDelay > 0)
				return (ticks * 1f / info.ActivationDelay +
					(1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length * 1f) / distance)) / 2;

			return 1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance;
		}

		void OnEnterRange()
		{
			// Spawn a camera and remove the beacon when the first plane enters the target area
			if (info.CameraActor != null)
			{
				world.AddFrameEndTask(w =>
				{
					camera = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(world.Map.CellContaining(target)),
							new OwnerInit(owner),
						});
				});
			}

			TryRemoveBeacon();
		}

		void OnExitRange()
		{
			if (camera != null)
			{
				camera.QueueActivity(new Wait(info.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());
			}

			camera = null;

			TryRemoveBeacon();
		}

		void TryRemoveBeacon()
		{
			if (beacon != null)
			{
				world.AddFrameEndTask(w =>
				{
					w.Remove(beacon);
					beacon = null;
				});
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r)
		{
			return Enumerable.Empty<IRenderable>();
		}
	}
}
