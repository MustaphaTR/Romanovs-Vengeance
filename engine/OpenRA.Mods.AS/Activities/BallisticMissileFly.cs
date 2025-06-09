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
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class BallisticMissileFly : Activity
	{
		enum BMFlyStatus { Prepare, Launch, NoCruiseLaunch, LazyCurve, Cruise, Hit, Unknown }

		readonly BallisticMissile bm;
		readonly BallisticMissileInfo bmInfo;
		readonly WPos initPos;
		readonly WPos targetPos;
		int ticks = 0;
		BMFlyStatus status = BMFlyStatus.Prepare;

		int speed = 0;
		readonly int dSpeed = 0;

		readonly int horizontalLength;
		readonly WAngle preparePitchIncrement;

		readonly int lazyCurveLength = 0;
		int lazyCurveTick = 0;

		public BallisticMissileFly(Actor self, Target t, BallisticMissile bm)
		{
			this.bm = bm;
			bmInfo = bm.Info;
			initPos = self.CenterPosition;
			targetPos = t.CenterPosition;

			horizontalLength = (initPos - targetPos).HorizontalLength;

			if (bmInfo.LaunchAcceleration == WDist.Zero)
			{
				speed = bmInfo.Speed.Length;
				dSpeed = 0;
			}
			else
			{
				speed = 0;
				dSpeed = bmInfo.LaunchAcceleration.Length;
			}

			if (bmInfo.LazyCurve)
			{
				lazyCurveLength = Math.Max((targetPos - initPos).Length / this.bm.Info.Speed.Length, 1);
			}

			preparePitchIncrement = new WAngle((bmInfo.LaunchAngle - bmInfo.CreateAngle).Angle / bmInfo.PrepareTick);

			if (bmInfo.WithoutCruise)
			{
				preparePitchIncrement = new WAngle((new WAngle(256) - bmInfo.CreateAngle).Angle / bmInfo.PrepareTick);
			}
		}

		protected override void OnFirstRun(Actor self)
		{
			bm.Pitch = bmInfo.CreateAngle;
		}

		void MoveForward(Actor self)
		{
			var move = new WVec(0, -speed, 0).Rotate(new WRot(bm.Pitch, WAngle.Zero, bm.Facing));
			bm.SetPosition(self, bm.CenterPosition + move);
			if (!self.IsInWorld)
				status = BMFlyStatus.Unknown;
		}

		void PrepareStatusHandle(Actor self)
		{
			if (ticks < bmInfo.PrepareTick)
				bm.Pitch += preparePitchIncrement;
			else
			{
				if (bm.Info.AudibleThroughFog || (!self.World.ShroudObscures(bm.CenterPosition) && !self.World.FogObscures(bm.CenterPosition)))
					Game.Sound.Play(SoundType.World, bm.Info.LaunchSounds, self.World, bm.CenterPosition, null, bm.Info.SoundVolume);
				if (bmInfo.WithoutCruise)
				{
					status = BMFlyStatus.NoCruiseLaunch;
					return;
				}

				if (bmInfo.LazyCurve)
				{
					status = BMFlyStatus.LazyCurve;
					return;
				}

				status = BMFlyStatus.Launch;
			}
		}

		void LaunchStatusHandle(Actor self)
		{
			MoveForward(self);
			speed = speed + dSpeed > bmInfo.Speed.Length ? bmInfo.Speed.Length : speed + dSpeed;
			if (bm.CenterPosition.Z - initPos.Z > bmInfo.BeginCruiseAltitude.Length)
			{
				status = BMFlyStatus.Cruise;
			}
		}

		void CruiseStatusHandle(Actor self)
		{
			MoveForward(self);
			if (bm.Pitch != WAngle.Zero)
			{
				if ((bm.Pitch.Angle < bm.TurnSpeed.Angle) || (1024 - bm.Pitch.Angle < bm.TurnSpeed.Angle))
				{
					bm.Pitch = WAngle.Zero;
				}
				else
				{
					bm.Pitch -= bm.TurnSpeed;
				}
			}

			var targetYaw = (targetPos - bm.CenterPosition).Yaw;
			var yawDiff = targetYaw - bm.Facing;
			if (yawDiff != WAngle.Zero)
			{
				if ((yawDiff.Angle < bm.TurnSpeed.Angle) || (1024 - yawDiff.Angle < bm.TurnSpeed.Angle))
				{
					bm.Facing = targetYaw;
				}
				else
				{
					if (yawDiff.Angle < 512)
						bm.Facing += bm.TurnSpeed;
					else
						bm.Facing -= bm.TurnSpeed;
				}
			}

			if ((targetPos - bm.CenterPosition).HorizontalLength < bmInfo.BeginHitRange.Length)
			{
				status = BMFlyStatus.Hit;
			}
		}

		void HitStatusHandle(Actor self)
		{
			MoveForward(self);
			speed += bmInfo.HitAcceleration.Length;
			var targetPitch = (targetPos - bm.CenterPosition).Pitch;
			var pitchDiff = targetPitch - bm.Pitch;
			if (pitchDiff != WAngle.Zero)
			{
				if ((pitchDiff.Angle < bm.TurnSpeed.Angle) || (1024 - pitchDiff.Angle < bm.TurnSpeed.Angle))
				{
					bm.Pitch = targetPitch;
				}
				else
				{
					if (pitchDiff.Angle < 512)
						bm.Pitch += bm.TurnSpeed;
					else
						bm.Pitch -= bm.TurnSpeed;
				}
			}

			var targetYaw = (targetPos - bm.CenterPosition).Yaw;
			var yawDiff = targetYaw - bm.Facing;
			if (yawDiff != WAngle.Zero)
			{
				if ((yawDiff.Angle < bm.TurnSpeed.Angle) || (1024 - yawDiff.Angle < bm.TurnSpeed.Angle))
				{
					bm.Facing = targetYaw;
				}
				else
				{
					if (yawDiff.Angle < 512)
						bm.Facing += bm.TurnSpeed;
					else
						bm.Facing -= bm.TurnSpeed;
				}
			}

			if ((targetPos - bm.CenterPosition).Length < bmInfo.ExplosionRange.Length)
			{
				status = BMFlyStatus.Unknown;
			}
		}

		void NoCruiseLaunchStatusHandle(Actor self)
		{
			MoveForward(self);
			speed = speed + dSpeed > bmInfo.Speed.Length ? bmInfo.Speed.Length : speed + dSpeed;
			if (bm.CenterPosition.Z - initPos.Z < horizontalLength && (bm.CenterPosition - targetPos).HorizontalLength > horizontalLength * 3 / 5)
			{
				return;
			}

			if (bm.Pitch != WAngle.Zero)
			{
				var newTurnSpeed = new WAngle(8192 * bm.TurnSpeed.Angle / horizontalLength);
				if ((bm.Pitch.Angle < newTurnSpeed.Angle) || (1024 - bm.Pitch.Angle < newTurnSpeed.Angle))
				{
					bm.Pitch = WAngle.Zero;
				}
				else
				{
					bm.Pitch -= newTurnSpeed;
				}

				return;
			}

			status = BMFlyStatus.Hit;
		}

		void LazyCurveHandle(Actor self)
		{
			var pos = WPos.LerpQuadratic(initPos, targetPos, bm.Info.LaunchAngle, lazyCurveTick, lazyCurveLength);
			bm.Pitch = (pos - bm.CenterPosition).Pitch;
			bm.SetPosition(self, pos);
			lazyCurveTick++;
			if ((targetPos - bm.CenterPosition).Length < bmInfo.ExplosionRange.Length)
			{
				status = BMFlyStatus.Unknown;
			}
		}

		public override bool Tick(Actor self)
		{
			switch (status)
			{
				case BMFlyStatus.Prepare:
					PrepareStatusHandle(self);
					break;
				case BMFlyStatus.Launch:
					LaunchStatusHandle(self);
					break;
				case BMFlyStatus.NoCruiseLaunch:
					NoCruiseLaunchStatusHandle(self);
					break;
				case BMFlyStatus.Cruise:
					CruiseStatusHandle(self);
					break;
				case BMFlyStatus.Hit:
					HitStatusHandle(self);
					break;
				case BMFlyStatus.LazyCurve:
					LazyCurveHandle(self);
					break;
				default:
					bm.SetPosition(self, targetPos);
					Queue(new CallFunc(() => self.Kill(self, bm.Info.DamageTypes)));
					return true;
			}

			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
