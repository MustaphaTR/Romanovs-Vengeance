#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Mods.RA2.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This unit, when ordered to move, will fly in ballistic path then will detonate itself upon reaching target.")]
	public class ShootableBallisticMissileInfo : ITraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly int Speed = 17;

		/*
		// RA2 Dreadnaut style initial pitch. Can't test this in RA1, not implementing.
		// public readonly int InitialPitch = 0;
		// public readonly int LaunchTicks = 15; // Time needed to make init pitch to launch pitch.
		*/

		[Desc("In angle. Missile is launched at this pitch and the intial tangential line of the ballistic path will be this.")]
		public readonly WAngle LaunchAngle = WAngle.Zero;

		[Desc("Minimum altitude where this missile is considered airborne")]
		public readonly int MinAirborneAltitude = 5;

		public virtual object Create(ActorInitializer init) { return new ShootableBallisticMissile(init, this); }

		[GrantedConditionReference]
		[Desc("The condition to grant to self while airborne.")]
		public readonly string AirborneCondition = null;

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any) { return new ReadOnlyDictionary<CPos, SubCell>(); }
		bool IOccupySpaceInfo.SharesCell { get { return false; } }
		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			// SBMs may not land.
			return false;
		}

		// set by spawned logic, not this.
		public int GetInitialFacing() { return 0; }
	}

	public class ShootableBallisticMissile : ITick, ISync, IFacing, IMove, IPositionable,
		INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyActorDisposing, IOccupySpace
	{
		static readonly Pair<CPos, SubCell>[] NoCells = { };

		public readonly ShootableBallisticMissileInfo Info;
		readonly Actor self;
		public Target Target { get; set; }

		ConditionManager conditionManager;
		IEnumerable<int> speedModifiers;

		[Sync]
		public int Facing { get; set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft { get { return self.World.Map.CellContaining(CenterPosition); } }

		bool airborne;
		int airborneToken = ConditionManager.InvalidConditionToken;

		public ShootableBallisticMissile(ActorInitializer init, ShootableBallisticMissileInfo info)
		{
			Info = info;
			self = init.Self;

			if (init.Contains<LocationInit>())
				SetPosition(self, init.Get<LocationInit, CPos>());

			if (init.Contains<CenterPositionInit>())
				SetPosition(self, init.Get<CenterPositionInit, WPos>());

			// I need facing but initial facing doesn't matter, they are determined by the spawner's facing.
			Facing = init.Contains<FacingInit>() ? init.Get<FacingInit, int>() : 0;
		}

		// This kind of missile will not turn anyway. Hard-coding here.
		public int TurnSpeed { get { return 10; } }

		public void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
		}

		public void AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);
			if (altitude.Length >= Info.MinAirborneAltitude)
				OnAirborneAltitudeReached();
		}

		public virtual void Tick(Actor self) { }

		public int MovementSpeed
		{
			get { return Util.ApplyPercentageModifiers(Info.Speed, speedModifiers); }
		}

		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return NoCells; }

		public WVec FlyStep(int facing)
		{
			return FlyStep(MovementSpeed, facing);
		}

		public WVec FlyStep(int speed, int facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing));
			return speed * dir / 1024;
		}

		#region Implement IPositionable

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; } // TODO: Handle landing
		public bool CanEnterCell(CPos cell, Actor ignoreActor = null, bool checkTransientActors = true) { return true; }
		public SubCell GetValidSubCell(SubCell preferred) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos a, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			// Does not use any subcell
			return SubCell.Invalid;
		}

		public void SetVisualPosition(Actor self, WPos pos) { SetPosition(self, pos); }

		// Changes position, but not altitude
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(0, 0, CenterPosition.Z));
		}

		public void SetPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;

			if (!self.IsInWorld)
				return;

			self.World.UpdateMaps(self, this);

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);
			var isAirborne = altitude.Length >= Info.MinAirborneAltitude;
			if (isAirborne && !airborne)
				OnAirborneAltitudeReached();
			else if (!isAirborne && airborne)
				OnAirborneAltitudeLeft();
		}

		#endregion

		#region Implement IMove

		public Activity MoveTo(CPos cell, int nearEnough)
		{
			return new ShootableBallisticMissileFly(self, Target.FromCell(self.World, cell));
		}

		public Activity MoveTo(CPos cell, Actor ignoredActor)
		{
			return new ShootableBallisticMissileFly(self, Target.FromCell(self.World, cell));
		}

		public Activity MoveWithinRange(Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new ShootableBallisticMissileFly(self, target);
		}

		public Activity MoveWithinRange(Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new ShootableBallisticMissileFly(self, target);
		}

		public Activity MoveFollow(Actor self, Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			return null;
		}

		public Activity MoveToTarget(Actor self, Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new ShootableBallisticMissileFly(self, target);
		}

		public Activity MoveIntoTarget(Actor self, Target target)
		{
			return null;
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			return new ShootableBallisticMissileFly(self, Target.FromPos(toPos));
		}

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeed;
			return speed > 0 ? (toPos - fromPos).Length / speed : 0;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

		// Actors with ShootableBallisticMissile always move
		public MovementType CurrentMovementTypes { get { return MovementType.Horizontal | MovementType.Vertical; } set { } }

		public bool CanEnterTargetNow(Actor self, Target target)
		{
			// you can never control ballistic missiles anyway
			return false;
		}

		#endregion

		#region Implement order interfaces

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new ShootableBallisticMissileMoveOrderTargeter(Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Enter")
				return new Order(order.OrderID, self, target, queued);

			if (order.OrderID == "Move")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
				self.QueueActivity(order.Queued, new ShootableBallisticMissileFly(self, order.Target));
		}

		#endregion

		public void RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);
			OnAirborneAltitudeLeft();
		}

		#region Airborne conditions

		void OnAirborneAltitudeReached()
		{
			if (airborne)
				return;

			airborne = true;
			if (conditionManager != null && !string.IsNullOrEmpty(Info.AirborneCondition) && airborneToken == ConditionManager.InvalidConditionToken)
				airborneToken = conditionManager.GrantCondition(self, Info.AirborneCondition);
		}

		void OnAirborneAltitudeLeft()
		{
			if (!airborne)
				return;

			airborne = false;
			if (conditionManager != null && airborneToken != ConditionManager.InvalidConditionToken)
				airborneToken = conditionManager.RevokeCondition(self, airborneToken);
		}

		#endregion

		public void Disposing(Actor self)
		{
		}

		Pair<CPos, SubCell>[] IOccupySpace.OccupiedCells()
		{
			return NoCells;
		}
	}
}
