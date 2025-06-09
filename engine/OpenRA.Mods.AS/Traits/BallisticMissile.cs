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
using OpenRA.Activities;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This unit, when add to world, will fly in ballistic path then will detonate itself upon reaching target.")]
	public class BallisticMissileInfo : TraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo
	{
		[Desc("Pitch angle at which the actor will be created.")]
		public readonly WAngle CreateAngle = WAngle.Zero;

		[Desc("The time it takes for the actor to be created to launch.")]
		public readonly int PrepareTick = 10;

		[Desc("The altitude at which the actor begins to cruise.")]
		public readonly WDist BeginCruiseAltitude = WDist.FromCells(7);

		[Desc("Turn speed.")]
		public readonly WAngle TurnSpeed = new(25);

		[Desc("The actor starts hitting the target when the horizontal distance is less than this value.")]
		public readonly WDist BeginHitRange = WDist.FromCells(4);

		[Desc("If the actor is closer to the target than this value, it will explode.")]
		public readonly WDist ExplosionRange = new(1536);

		[Desc("The acceleration of the actor during the launch phase, the speed during the launch phase will not be more than the speed value.")]
		public readonly WDist LaunchAcceleration = WDist.Zero;

		[Desc("Unit acceleration during the strike, no upper limit for speed value.")]
		public readonly WDist HitAcceleration = new(20);

		[Desc("Simplify the trajectory into a parabola." +
			"The following values will have no effect: BeginCruiseAltitude, TurnSpeed, BeginHitRange, ExplosionRange, LaunchAcceleration, HitAcceleration")]
		public readonly bool LazyCurve = false;

		[Desc("Skip the cruise phase, BeginCruiseAltitude and BeginHitRange will no longer be valid, LaunchAngle is hard-coded to 256.")]
		public readonly bool WithoutCruise = false;

		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist Speed = new(17);

		[Desc("In angle. Missile is launched at this pitch and the intial tangential line of the ballistic path will be this.")]
		public readonly WAngle LaunchAngle = WAngle.Zero;

		[Desc("Minimum altitude where this missile is considered airborne")]
		public readonly int MinAirborneAltitude = 5;

		[Desc("Types of damage missile explosion is triggered with. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while airborne.")]
		public readonly string AirborneCondition = null;

		[Desc("Sounds to play when the actor is taking off.")]
		public readonly string[] LaunchSounds = Array.Empty<string>();

		[Desc("Do the launching sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the LaunchSounds played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new BallisticMissile(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new Dictionary<CPos, SubCell>();
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

		public bool CanEnterCell(
			World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// SBMs may not land.
			return false;
		}

		// set by spawned logic, not this.
		public WAngle GetInitialFacing() { return WAngle.Zero; }
		public Color GetTargetLineColor() { return Color.Green; }
	}

	public class BallisticMissile : ISync, IFacing, IMove, IPositionable,
		INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, IOccupySpace
	{
		static readonly (CPos Cell, SubCell SubCell)[] NoCells = Array.Empty<(CPos Cell, SubCell SubCell)>();

		public readonly BallisticMissileInfo Info;
		readonly Actor self;
		public Target Target;

		IEnumerable<int> speedModifiers;

		[Sync]
		public WAngle Facing
		{
			get => Orientation.Yaw;
			set => Orientation = Orientation.WithYaw(value);
		}

		public WAngle Pitch
		{
			get => Orientation.Pitch;
			set => Orientation = Orientation.WithPitch(value);
		}

		public WAngle Roll
		{
			get => Orientation.Roll;
			set => Orientation = Orientation.WithRoll(value);
		}

		public WRot Orientation { get; private set; }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft { get { return self.World.Map.CellContaining(CenterPosition); } }

		bool airborne;
		int airborneToken = Actor.InvalidConditionToken;

		public BallisticMissile(ActorInitializer init, BallisticMissileInfo info)
		{
			Info = info;
			self = init.Self;

			var locationInit = init.GetOrDefault<LocationInit>(info);
			if (locationInit != null)
				SetPosition(self, locationInit.Value);

			var centerPositionInit = init.GetOrDefault<CenterPositionInit>(info);
			if (centerPositionInit != null)
				SetPosition(self, centerPositionInit.Value);

			// I need facing but initial facing doesn't matter, they are determined by the spawner's facing.
			Facing = init.GetValue<FacingInit, WAngle>(info, WAngle.Zero);
		}

		// This kind of missile will not turn anyway. Hard-coding here.
		public WAngle TurnSpeed => Info.TurnSpeed;

		void INotifyCreated.Created(Actor self)
		{
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
			Pitch = Info.CreateAngle;
			self.QueueActivity(new BallisticMissileFly(self, Target, this));

			var altitude = self.World.Map.DistanceAboveTerrain(CenterPosition);
			if (altitude.Length >= Info.MinAirborneAltitude)
				OnAirborneAltitudeReached();
		}

		(CPos Cell, SubCell SubCell)[] IOccupySpace.OccupiedCells()
		{
			return NoCells;
		}

		public int MovementSpeed
		{
			get { return Util.ApplyPercentageModifiers(Info.Speed.Length, speedModifiers); }
		}

		public WVec FlyStep(WAngle facing)
		{
			return FlyStep(MovementSpeed, facing);
		}

		public WVec FlyStep(int speed, WAngle facing)
		{
			var dir = new WVec(0, -1024, 0).Rotate(WRot.FromFacing(facing.Facing));
			return speed * dir / 1024;
		}

		#region Implement IPositionable

		public bool CanExistInCell(CPos cell) { return true; }
		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return false; } // TODO: Handle landing
		public bool CanEnterCell(CPos location, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All) { return true; }
		public SubCell GetValidSubCell(SubCell preferred) { return SubCell.Invalid; }
		public SubCell GetAvailableSubCell(CPos location, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// Does not use any subcell
			return SubCell.Invalid;
		}

		public void SetCenterPosition(Actor self, WPos pos) { SetPosition(self, pos); }

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

		public Activity MoveTo(CPos cell, int nearEnough = 0, Actor ignoreActor = null,
			bool evaluateNearestMovableCell = false, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity MoveFollow(Actor self, in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity ReturnToCell(Actor self)
		{
			return null;
		}

		public Activity MoveToTarget(Actor self, in Target target,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity MoveIntoTarget(Actor self, in Target target)
		{
			return null;
		}

		public Activity MoveOntoTarget(Actor self, in Target target, in WVec offset, WAngle? facing, Color? targetLineColor = null)
		{
			return null;
		}

		public Activity LocalMove(Actor self, WPos fromPos, WPos toPos)
		{
			return null;
		}

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeed;
			return speed > 0 ? (toPos - fromPos).Length / speed : 0;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

		// Actors with BallisticMissile always move
		public MovementType CurrentMovementTypes { get => MovementType.Horizontal | MovementType.Vertical; set { } }

		public bool CanEnterTargetNow(Actor self, in Target target)
		{
			// you can never control ballistic missiles anyway
			return false;
		}

		#endregion

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
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
			if (!string.IsNullOrEmpty(Info.AirborneCondition) && airborneToken == Actor.InvalidConditionToken)
				airborneToken = self.GrantCondition(Info.AirborneCondition);
		}

		void OnAirborneAltitudeLeft()
		{
			if (!airborne)
				return;

			airborne = false;
			if (airborneToken != Actor.InvalidConditionToken)
				airborneToken = self.RevokeCondition(airborneToken);
		}

		#endregion
	}
}
