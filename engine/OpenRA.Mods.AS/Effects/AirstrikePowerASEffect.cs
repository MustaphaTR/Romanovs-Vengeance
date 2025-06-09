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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Effects
{
	public class AirstrikePowerASEffect : IEffect
	{
		readonly AirstrikePowerASInfo info;
		readonly Player owner;
		readonly World world;
		readonly WPos pos;

		IEnumerable<Actor> planes;
		Actor camera = null;
		Beacon beacon = null;
		bool enteredRange = false;

		public AirstrikePowerASEffect(World world, Player p, WPos pos, IEnumerable<Actor> planes, AirstrikePowerAS power, AirstrikePowerASInfo info)
		{
			var level = power.GetLevel();
			if (level == 0)
				return;

			this.info = info;
			this.world = world;
			owner = p;
			this.pos = pos;
			this.planes = planes;

			if (info.DisplayBeacon)
			{
				var distance = (planes.First().OccupiesSpace.CenterPosition - pos).HorizontalLength;

				beacon = new Beacon(
					owner,
					pos - new WVec(WDist.Zero, WDist.Zero, world.Map.DistanceAboveTerrain(pos)),
					info.BeaconPaletteIsPlayerPalette,
					info.BeaconPalette,
					info.BeaconImage,
					info.BeaconPosters.First(bp => bp.Key == level).Value,
					info.BeaconPosterPalette,
					info.BeaconSequence,
					info.ArrowSequence,
					info.CircleSequence,
					info.ClockSequence,
					() => 1 - ((planes.First().OccupiesSpace.CenterPosition - pos).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance,
					info.BeaconDelay);

				world.AddFrameEndTask(w => w.Add(beacon));
			}
		}

		void IEffect.Tick(World world)
		{
			planes = planes.Where(p => p.IsInWorld && !p.IsDead);

			if (!enteredRange && planes.Any(p => (p.OccupiesSpace.CenterPosition - pos).Length < info.BeaconDistanceOffset.Length))
			{
				OnEnterRange();
				enteredRange = true;
			}

			if (!planes.Any() || (enteredRange && planes.All(p => (p.OccupiesSpace.CenterPosition - pos).Length > info.BeaconDistanceOffset.Length)))
			{
				OnExitRange();
				world.AddFrameEndTask(w => w.Remove(this));
			}
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
							new LocationInit(world.Map.CellContaining(pos)),
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
