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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class AirstrikePowerRVInfo : DirectionalSupportPowerInfo
	{
		[FieldLoader.Require]
		public readonly Dictionary<int, string> UnitTypes = new();

		[FieldLoader.Require]
		public readonly Dictionary<int, int> SquadSizes = new();

		public readonly WVec SquadOffset = new(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new(5120);

		[Desc("Delay between activation and aircraft spawning")]
		public readonly int ActivationDelay = 0;

		[ActorReference]
		[Desc("Actor to spawn when the aircraft start attacking")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have finished attacking")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Weapon range offset to apply during the beacon clock calculation")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		public override object Create(ActorInitializer init) { return new AirstrikePowerRV(init.Self, this); }
	}

	public class AirstrikePowerRV : DirectionalSupportPower
	{
		readonly AirstrikePowerRVInfo info;

		public AirstrikePowerRV(Actor self, AirstrikePowerRVInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.UseDirectionalTarget)
			{
				Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

				self.World.OrderGenerator = new SelectDirectionalTarget(self.World, order, manager, info);
			}
			else
				base.SelectTarget(self, order, manager);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var facing = info.UseDirectionalTarget && order.ExtraData != uint.MaxValue ? (WAngle?)WAngle.FromFacing((int)order.ExtraData) : null;
			SendAirstrike(self, order.Target.CenterPosition, facing);
		}

		public Actor[] SendAirstrike(Actor self, WPos target, WAngle? facing = null)
		{
			var level = GetLevel();
			if (level == 0)
				return Array.Empty<Actor>();

			var aircraft = new List<Actor>();
			if (!facing.HasValue)
				facing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);

			var altitude = self.World.Map.Rules.Actors[info.UnitTypes.First(ut => ut.Key == level).Value].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var attackRotation = WRot.FromYaw(facing.Value);
			var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
			target += new WVec(0, 0, altitude);
			var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
			var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

			Actor camera = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			void OnEnterRange(Actor a)
			{
				// Spawn a camera and remove the beacon when the first plane enters the target area
				if (info.CameraActor != null && camera == null && !aircraftInRange.Any(kv => kv.Value))
				{
					self.World.AddFrameEndTask(w =>
					{
						camera = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(self.World.Map.CellContaining(target)),
							new OwnerInit(self.Owner),
						});
					});
				}

				aircraftInRange[a] = true;
			}

			void OnExitRange(Actor a)
			{
				aircraftInRange[a] = false;

				// Remove the camera when the final plane leaves the target area
				if (!aircraftInRange.Any(kv => kv.Value))
					RemoveCamera(camera);
			}

			void OnRemovedFromWorld(Actor a)
			{
				aircraftInRange[a] = false;

				// Checking for attack range is not relevant here because
				// aircraft may be shot down before entering the range.
				// If at the map's edge, they may be removed from world before leaving.
				if (aircraftInRange.All(kv => !kv.Key.IsInWorld))
				{
					RemoveCamera(camera);
				}
			}

			// Create the actors immediately so they can be returned
			var squadSize = info.SquadSizes.First(ss => ss.Key == level).Value;
			for (var i = -squadSize / 2; i <= squadSize / 2; i++)
			{
				// Even-sized squads skip the lead plane
				if (i == 0 && (squadSize & 1) == 0)
					continue;

				// Includes the 90 degree rotation between body and world coordinates
				var so = info.SquadOffset;
				var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
				var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(attackRotation);
				var a = self.World.CreateActor(false, info.UnitTypes.First(ut => ut.Key == level).Value, new TypeDictionary
				{
					new CenterPositionInit(startEdge + spawnOffset),
					new OwnerInit(self.Owner),
					new FacingInit(facing.Value),
				});

				aircraft.Add(a);
				aircraftInRange.Add(a, false);

				var attack = a.Trait<AttackBomber>();
				attack.SetTarget(target + targetOffset);
				attack.OnEnteredAttackRange += OnEnterRange;
				attack.OnExitedAttackRange += OnExitRange;
				attack.OnRemovedFromWorld += OnRemovedFromWorld;
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				var effect = new AirstrikePowerRVEffect(
					self.World, self.Owner, target, startEdge, finishEdge, attackRotation, altitude, level, aircraft.ToArray(), this, info);
				self.World.Add(effect);
			});

			return aircraft.ToArray();
		}

		void RemoveCamera(Actor camera)
		{
			if (camera == null)
				return;

			camera.QueueActivity(new Wait(info.CameraRemoveDelay));
			camera.QueueActivity(new RemoveSelf());
		}
	}
}
