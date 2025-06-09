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
using System.Linq;
using OpenRA.Mods.Common.Traits;

/*
 * Needs base engine modification. (Becaus DroneSpawner.cs mods it)
 */

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Can be slaved to a drone spawner.")]
	public class DroneSpawnerSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Aircraft slaves outside of this range from master while moving will be call back")]
		public readonly int MovingCallBackCellDistance = 2;

		[Desc("Slaves will follow master instead of attack while target outside of this range")]
		public readonly WDist AttackCallBackDistance = WDist.FromCells(10);

		public override object Create(ActorInitializer init) { return new DroneSpawnerSlave(this); }
	}

	public class DroneSpawnerSlave : BaseSpawnerSlave
	{
		public IMove[] Moves { get; private set; }
		public IPositionable Positionable { get; private set; }
		public bool IsAircraft;
		public readonly DroneSpawnerSlaveInfo Info;
		Actor currentActor;
		Actor masterActor;
		public readonly Predicate<Actor> InvalidActor;

		public bool IsMoving(CPos gatherlocation)
		{
			if (IsAircraft)
			{
				if (!InvalidActor(currentActor) && !InvalidActor(masterActor) &&
					(currentActor.Location - gatherlocation).LengthSquared > Info.MovingCallBackCellDistance * Info.MovingCallBackCellDistance)
					return false;

				return true;
			}

			return Array.Exists(Moves, m => m.IsTraitEnabled()
				&& (m.CurrentMovementTypes.HasFlag(MovementType.Horizontal) || m.CurrentMovementTypes.HasFlag(MovementType.Vertical)));
		}

		public DroneSpawnerSlave(DroneSpawnerSlaveInfo info)
			: base(info)
		{
			InvalidActor = a => a == null || a.IsDead || !a.IsInWorld;
			Info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			currentActor = self;

			Moves = self.TraitsImplementing<IMove>().ToArray();

			var positionables = self.TraitsImplementing<IPositionable>();
			if (positionables.Count() != 1)
				throw new InvalidOperationException($"Actor {self} has multiple (or no) traits implementing IPositionable.");

			Positionable = positionables.First();

			IsAircraft = self.Info.HasTraitInfo<AircraftInfo>();
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			masterActor = master;
		}

		public void Move(Actor self, CPos location)
		{
			// And tell attack bases to stop attacking.
			if (Moves.Length == 0)
				return;

			foreach (var mv in Moves)
				if (mv.IsTraitEnabled())
				{
					if (IsAircraft)
						self.QueueActivity(mv.MoveTo(location, 0));
					else
						self.QueueActivity(mv.MoveTo(location, 2));
					break;
				}
		}
	}
}
