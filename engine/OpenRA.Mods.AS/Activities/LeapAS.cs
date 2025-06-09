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
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	class LeapAS : Activity
	{
		readonly Mobile mobile;
		readonly Armament armament;
		readonly int length;
		readonly AttackLeapAS trait;
		readonly WAngle angle;
		readonly Target target;

		readonly WPos from;
		readonly WPos to;
		int ticks;

		public LeapAS(Actor self, Actor target, Armament a, AttackLeapAS trait)
		{
			var targetMobile = target.TraitOrDefault<Mobile>();
			if (targetMobile == null)
				throw new InvalidOperationException("Leap requires a target actor with the Mobile trait");

			armament = a;
			angle = trait.LeapInfo.Angle;
			this.trait = trait;
			this.target = Target.FromActor(target);
			mobile = self.Trait<Mobile>();
			mobile.SetLocation(mobile.FromCell, mobile.FromSubCell, targetMobile.FromCell, targetMobile.FromSubCell);

			from = self.CenterPosition;
			to = self.World.Map.CenterOfSubCell(targetMobile.FromCell, targetMobile.FromSubCell);
			length = Math.Max((to - from).Length / trait.LeapInfo.Speed.Length, 1);

			if (armament.Weapon.Report != null && armament.Weapon.Report.Length > 0)
			{
				var pos = self.CenterPosition;
				if (armament.Weapon.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, armament.Weapon.Report.Random(self.World.SharedRandom), pos, armament.Weapon.SoundVolume);
			}
		}

		public override bool Tick(Actor self)
		{
			if (ticks == 0 && IsCanceling)
				return true;

			mobile.SetCenterPosition(self, WPos.LerpQuadratic(from, to, angle, ++ticks, length));
			if (ticks >= length)
			{
				mobile.SetLocation(mobile.ToCell, mobile.ToSubCell, mobile.ToCell, mobile.ToSubCell);
				mobile.FinishedMoving(self);

				trait.NotifyAttacking(self, target, armament);

				var actors = self.World.ActorMap.GetActorsAt(mobile.ToCell, mobile.ToSubCell)
					.Except(new[] { self }).Where(t => armament.Weapon.IsValidAgainst(t, self));
				foreach (var a in actors)
					a.Kill(self, trait.LeapInfo.DamageTypes);

				trait.FinishAttacking(self);

				return true;
			}

			return false;
		}
	}
}
