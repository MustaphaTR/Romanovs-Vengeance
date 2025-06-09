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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Projectiles;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Effects
{
	public class WarheadTrailProjectileEffect : IEffect, ISync
	{
		readonly WarheadTrailProjectileInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;
		readonly string trailPalette;
		readonly World world;

		[Sync]
		readonly WPos targetpos, source;
		[Sync]
		readonly WAngle facing;

		readonly int lifespan, estimatedlifespan;
		readonly bool forceToGround;

		readonly ContrailRenderable contrail;

		[Sync]
		WPos projectilepos, lastPos;

		int ticks, smokeTicks;
		public bool DetonateSelf { get; private set; }
		public WPos Position { get { return projectilepos; } }

		public WarheadTrailProjectileEffect(WarheadTrailProjectileInfo info, ProjectileArgs args, int lifespan, int estimatedlifespan, bool forceToGround)
		{
			this.info = info;
			this.args = args;
			this.lifespan = lifespan;
			this.estimatedlifespan = estimatedlifespan;
			this.forceToGround = forceToGround;
			projectilepos = args.Source;
			source = args.Source;

			world = args.SourceActor.World;
			targetpos = args.PassiveTarget;
			facing = args.Facing;

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(world, info.Image, new Func<WAngle>(GetEffectiveFacing));
				anim.PlayRepeating(info.Sequences.Random(world.SharedRandom));
			}

			if (info.ContrailLength > 0)
			{
				var startcolor = Color.FromArgb(info.ContrailStartColorAlpha, info.ContrailStartColor);
				var endcolor = Color.FromArgb(info.ContrailEndColorAlpha, info.ContrailEndColor ?? startcolor);
				contrail = new ContrailRenderable(world, args.SourceActor,
					startcolor, info.ContrailStartColorUsePlayerColor,
					endcolor, info.ContrailEndColor == null ? info.ContrailStartColorUsePlayerColor : info.ContrailEndColorUsePlayerColor,
					info.ContrailStartWidth,
					info.ContrailEndWidth ?? info.ContrailStartWidth,
					info.ContrailLength, info.ContrailDelay, info.ContrailZOffset);
			}

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

			smokeTicks = info.TrailDelay;
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (lifespan - 1);
			var attitude = WAngle.Zero.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = facing.Angle % 512 / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			if (anim == null || ticks >= lifespan)
				yield break;

			if (!world.FogObscures(projectilepos))
			{
				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(projectilepos);
					var shadowPos = projectilepos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, wr.Palette(info.ShadowPalette)))
						yield return r;
				}

				var palette = wr.Palette(info.Palette);
				foreach (var r in anim.Render(projectilepos, palette))
					yield return r;
			}
		}

		public void Tick(World world)
		{
			ticks++;
			anim?.Tick();

			lastPos = projectilepos;
			projectilepos = WPos.Lerp(source, targetpos, ticks, estimatedlifespan);

			// Check for walls or other blocking obstacles.
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, lastPos, projectilepos, info.Width, out var blockedPos))
			{
				projectilepos = blockedPos;
				DetonateSelf = true;
			}

			if (!string.IsNullOrEmpty(info.TrailImage) && --smokeTicks < 0)
			{
				var delayedPos = WPos.Lerp(source, targetpos, ticks - info.TrailDelay, estimatedlifespan);
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(delayedPos, GetEffectiveFacing(), w,
					info.TrailImage, info.TrailSequences.Random(world.SharedRandom), trailPalette)));

				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(projectilepos);

			var flightLengthReached = ticks >= lifespan;

			if (flightLengthReached)
				DetonateSelf = true;

			// Driving into cell with higher height level
			DetonateSelf |= world.Map.DistanceAboveTerrain(projectilepos) < info.ExplodeUnderThisAltitude;

			if (DetonateSelf)
				Explode(world);
		}

		public void Explode(World world)
		{
			Impact();

			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(projectilepos, contrail)));

			world.AddFrameEndTask(w => w.Remove(this));
		}

		public void Impact()
		{
			var pos = forceToGround ? projectilepos - new WVec(0, 0, world.Map.DistanceAboveTerrain(projectilepos).Length) : projectilepos;
			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(lastPos, projectilepos), args.Facing),
				ImpactPosition = pos,
			};

			args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
		}
	}
}
