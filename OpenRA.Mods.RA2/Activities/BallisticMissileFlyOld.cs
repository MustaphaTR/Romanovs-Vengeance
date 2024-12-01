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
using OpenRA.Mods.RA2.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Activities
{
	public class BallisticMissileFlyOld : Activity
	{
		readonly BallisticMissileOld bm;
		readonly WPos initPos;
		readonly WPos targetPos;
		readonly int length;
		readonly WAngle facing;
		int ticks;

		public BallisticMissileFlyOld(Actor self, Target t, BallisticMissileOld bm)
		{
			this.bm = bm;
			initPos = self.CenterPosition;
			targetPos = t.CenterPosition;
			length = Math.Max((targetPos - initPos).Length / this.bm.Info.Speed, 1);
			facing = (targetPos - initPos).Yaw;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (bm.Info.LaunchSounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (bm.Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, bm.Info.LaunchSounds, self.World, pos, null, bm.Info.SoundVolume);
			}
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = bm.Info.LaunchAngle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = (facing.Angle % 512) / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public override bool Tick(Actor self)
		{
			var d = targetPos - self.CenterPosition;
			var move = bm.FlyStep(bm.Facing);

			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				// Snap to the target position to prevent overshooting.
				bm.SetPosition(self, targetPos);
				Queue(new CallFunc(() => self.Kill(self, bm.Info.DamageTypes)));
				return true;
			}

			var pos = WPos.LerpQuadratic(initPos, targetPos, bm.Info.LaunchAngle, ticks, length);
			bm.SetPosition(self, pos);
			bm.Facing = GetEffectiveFacing();

			ticks++;
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
