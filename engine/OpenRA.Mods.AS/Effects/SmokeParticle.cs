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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Effects
{
	class SmokeParticle : IEffect
	{
		readonly Actor invoker;
		readonly World world;
		readonly ISmokeParticleInfo smoke;
		readonly Animation anim;
		readonly WDist[] speed;
		readonly WDist[] gravity;
		readonly bool visibleThroughFog;
		readonly bool canDamage;
		readonly int turnRate;
		readonly HashSet<IReloadModifier> reloadModifiers = new();
		readonly HashSet<IFirepowerModifier> damageModifiers = new();
		readonly string palette;

		WPos pos, lastPos;
		int lifetime;
		int explosionInterval;

		int facing;
		bool ending;

		public SmokeParticle(Actor invoker, ISmokeParticleInfo smoke, WPos pos, int facing = -1, bool visibleThroughFog = false)
		{
			this.invoker = invoker;
			world = invoker.World;
			this.pos = pos;
			this.smoke = smoke;
			speed = smoke.Speed;
			gravity = smoke.Gravity;
			this.visibleThroughFog = visibleThroughFog;

			palette = smoke.Palette;
			if (smoke.IsPlayerPalette)
				palette += invoker.Owner.InternalName;

			if (invoker != null && !invoker.IsDead)
			{
				reloadModifiers = invoker.TraitsImplementing<IReloadModifier>().ToHashSet();
				damageModifiers = invoker.TraitsImplementing<IFirepowerModifier>().ToHashSet();
			}

			this.facing = facing > -1
				? facing
				: world.SharedRandom.Next(256);

			turnRate = smoke.TurnRate;
			anim = new Animation(world, smoke.Image, () => WAngle.FromFacing(facing));
			if (smoke.StartSequences != null && smoke.StartSequences.Length > 0)
				anim.PlayThen(smoke.StartSequences.Random(world.SharedRandom),
					() => anim.PlayRepeating(smoke.Sequences.Random(world.SharedRandom)));
			else
				anim.PlayRepeating(smoke.Sequences.Random(world.SharedRandom));
			world.ScreenMap.Add(this, pos, anim.Image);
			lifetime = smoke.Duration.Length == 2
				? world.SharedRandom.Next(smoke.Duration[0], smoke.Duration[1])
				: smoke.Duration[0];

			canDamage = smoke.Weapon != null;
		}

		public void Tick(World world)
		{
			lastPos = pos;
			if (--lifetime < 0 && !ending)
			{
				if (smoke.EndSequences != null && smoke.EndSequences.Length > 0)
				{
					ending = true;
					anim.PlayThen(smoke.EndSequences.Random(world.SharedRandom), () =>
					{
						world.AddFrameEndTask(w =>
						{
							w.Remove(this);
							w.ScreenMap.Remove(this);
						});
					});
				}
				else
				{
					world.AddFrameEndTask(w =>
					{
						w.Remove(this);
						w.ScreenMap.Remove(this);
					});
				}

				return;
			}

			anim.Tick();

			var forward = speed.Length == 2
				? world.SharedRandom.Next(speed[0].Length, speed[1].Length)
				: speed[0].Length;

			var height = gravity.Length == 2
				? world.SharedRandom.Next(gravity[0].Length, gravity[1].Length)
				: gravity[0].Length;

			var offset = new WVec(forward, 0, height);

			if (turnRate > 0)
				facing = (facing + world.SharedRandom.Next(-turnRate, turnRate)) & 0xFF;

			offset = offset.Rotate(WRot.FromFacing(facing));

			pos += offset;

			world.ScreenMap.Update(this, pos, anim.Image);

			if (canDamage && --explosionInterval < 0)
			{
				var args = new WarheadArgs
				{
					Weapon = smoke.Weapon,
					DamageModifiers = damageModifiers.Select(a => a.GetFirepowerModifier(null)).ToArray(),
					Source = pos,
					SourceActor = invoker,
					WeaponTarget = Target.FromPos(pos),
					ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(lastPos, pos), WAngle.FromFacing(facing)),
					ImpactPosition = pos
				};

				smoke.Weapon.Impact(Target.FromPos(pos), args);
				explosionInterval = Common.Util.ApplyPercentageModifiers(smoke.Weapon.ReloadDelay, reloadModifiers.Select(m => m.GetReloadModifier(null)));
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(pos) && !visibleThroughFog)
				return SpriteRenderable.None;

			return anim.Render(pos, WVec.Zero, 0, wr.Palette(palette));
		}
	}
}
