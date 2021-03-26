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
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This unit, when ordered to move, will fly in ballistic path then will detonate itself upon reaching target.")]
	public class BallisticMissileOldInfo : TraitInfo, IMoveInfo, IPositionableInfo, IFacingInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly int Speed = 17;

		[Desc("In angle. Missile is launched at this pitch and the intial tangential line of the ballistic path will be this.")]
		public readonly WAngle LaunchAngle = WAngle.Zero;

		[Desc("Minimum altitude where this missile is considered airborne")]
		public readonly int MinAirborneAltitude = 5;

		[Desc("Types of damage missile explosion is triggered with. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[GrantedConditionReference]
		[Desc("The condition to grant to self while airborne.")]
		public readonly string AirborneCondition = null;

		[Desc("Sounds to play when the actor is taking off.")]
		public readonly string[] LaunchSounds = { };

		[Desc("Do the launching sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the LaunchSounds played at.")]
		public readonly float SoundVolume = 1f;

		public override object Create(ActorInitializer init) { return new BallisticMissileOld(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any) { return new ReadOnlyDictionary<CPos, SubCell>(); }
		bool IOccupySpaceInfo.SharesCell { get { return false; } }
		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// SBMs may not land.
			return false;
		}

		// set by spawned logic, not this.
		public WAngle GetInitialFacing() { return WAngle.Zero; }
		public Color GetTargetLineColor() { return Color.Green; }
	}

	public class BallisticMissileOld : ISync, IFacing, IMove, IPositionable,
		INotifyCreated, INotifyAddedToWorld, INotifyRemovedFromWorld, IOccupySpace
	{
		static readonly (CPos Cell, SubCell SubCell)[] NoCells = { };

		public readonly BallisticMissileOldInfo Info;
		readonly Actor self;
		public Target Target;

		IEnumerable<int> speedModifiers;

		WRot orientation;

		[Sync]
		public WAngle Facing
		{
			get { return orientation.Yaw; }
			set { orientation = orientation.WithYaw(value); }
		}

		public WAngle Pitch
		{
			get { return orientation.Pitch; }
			set { orientation = orientation.WithPitch(value); }
		}

		public WAngle Roll
		{
			get { return orientation.Roll; }
			set { orientation = orientation.WithRoll(value); }
		}

		public WRot Orientation { get { return orientation; } }

		[Sync]
		public WPos CenterPosition { get; private set; }

		public CPos TopLeft { get { return self.World.Map.CellContaining(CenterPosition); } }

		bool airborne;
		int airborneToken = Actor.InvalidConditionToken;

		public BallisticMissileOld(ActorInitializer init, BallisticMissileOldInfo info)
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
		public WAngle TurnSpeed { get { return new WAngle(40); } }

		void INotifyCreated.Created(Actor self)
		{
			speedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray().Select(sm => sm.GetSpeedModifier());
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);
			self.QueueActivity(new BallisticMissileFlyOld(self, Target, this));

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
			get { return Util.ApplyPercentageModifiers(Info.Speed, speedModifiers); }
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
			return new BallisticMissileFlyOld(self, Target.FromCell(self.World, cell), this);
		}

		public Activity MoveWithinRange(in Target target, WDist range,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new BallisticMissileFlyOld(self, target, this);
		}

		public Activity MoveWithinRange(in Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition = null, Color? targetLineColor = null)
		{
			return new BallisticMissileFlyOld(self, target, this);
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
			return new BallisticMissileFlyOld(self, target, this);
		}

		public Activity MoveIntoTarget(Actor self, in Target target)
		{
			return new BallisticMissileFlyOld(self, target, this);
		}

		public Activity LocalMove(Actor self, WPos fromPos, WPos toPos)
		{
			return new BallisticMissileFlyOld(self, Target.FromPos(toPos), this);
		}

		public int EstimatedMoveDuration(Actor self, WPos fromPos, WPos toPos)
		{
			var speed = MovementSpeed;
			return speed > 0 ? (toPos - fromPos).Length / speed : 0;
		}

		public CPos NearestMoveableCell(CPos cell) { return cell; }

		// Actors with BallisticMissile always move
		public MovementType CurrentMovementTypes { get { return MovementType.Horizontal | MovementType.Vertical; } set { } }

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
