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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using Util = OpenRA.Mods.Common.Util;

namespace OpenRA.Mods.AS.Projectiles
{
	public class BulletASInfo : IProjectileInfo
	{
		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new(17) };

		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are " +
			"'Maximum' - scale from 0 to max with range, " +
			"'PerCellIncrement' - scale from 0 with range, " +
			"'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference(nameof(IsPlayerPalette))]
		public readonly string Palette = "effect";

		public readonly bool IsPlayerPalette = false;

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Color to draw shadow if Shadow is true.")]
		public readonly Color ShadowColor = Color.FromArgb(140, 0, 0, 0);

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		[PaletteReference]
		public readonly string ShadowPalette = "shadow";

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		[SequenceReference(nameof(TrailImage), allowNullImage: true)]
		public readonly string[] TrailSequences = { "idle" };

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new(1);

		[Desc("Arc in WAngles, two values indicate variable arc.")]
		public readonly WAngle[] LaunchAngle = { WAngle.Zero };

		[Desc("Up to how many times does this bullet bounce when touching ground without hitting a target.",
			"0 implies exploding on contact with the originally targeted position.")]
		public readonly int BounceCount = 0;

		[Desc("Sound to play when the projectile hits the ground, but not the target.")]
		public readonly string BounceSound = null;

		[Desc("Terrain where the projectile explodes instead of bouncing.")]
		public readonly HashSet<string> InvalidBounceTerrain = new();

		[Desc("Modify distance of each bounce by this percentage of previous distance.")]
		public readonly int BounceRangeModifier = 60;

		[Desc("If projectile touches an actor with one of these stances during or after the first bounce, trigger explosion.")]
		public readonly PlayerRelationship ValidBounceBlockerStances = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		[Desc("Altitude where this bullet should explode when reached.",
			"Negative values allow this bullet to pass cliffs and terrain bumps.")]
		public readonly WDist ExplodeUnderThisAltitude = new(-1536);

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 1;

		[Desc("Palette used to render the trail sequence.")]
		[PaletteReference(nameof(TrailUsePlayerPalette))]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		[Desc("Types of point defense weapons that can target this projectile.")]
		public readonly BitSet<string> PointDefenseTypes = default;

		[Desc("When set, display a line behind the actor. Length is measured in ticks after appearing.")]
		public readonly int ContrailLength = 0;

		[Desc("Time (in ticks) after which the line should appear. Controls the distance to the actor.")]
		public readonly int ContrailDelay = 1;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ContrailZOffset = 2047;

		[Desc("Thickness of the emitted line at the start of the contrail.")]
		public readonly WDist ContrailStartWidth = new(64);

		[Desc("Thickness of the emitted line at the end of the contrail. Will default to " + nameof(ContrailStartWidth) + " if left undefined")]
		public readonly WDist? ContrailEndWidth = null;

		[Desc("RGB color at the contrail start.")]
		public readonly Color ContrailStartColor = Color.White;

		[Desc("Use player remap color instead of a custom color at the contrail the start.")]
		public readonly bool ContrailStartColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail the start.")]
		public readonly int ContrailStartColorAlpha = 255;

		[Desc("RGB color at the contrail end. Will default to " + nameof(ContrailStartColor) + " if left undefined")]
		public readonly Color? ContrailEndColor;

		[Desc("Use player remap color instead of a custom color at the contrail end.")]
		public readonly bool ContrailEndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail end.")]
		public readonly int ContrailEndColorAlpha = 0;

		public IProjectile Create(ProjectileArgs args) { return new BulletAS(this, args); }
	}

	public class BulletAS : IProjectile, ISync
	{
		readonly BulletASInfo info;
		readonly ProjectileArgs args;
		readonly Animation anim;

		readonly float3 shadowColor;
		readonly float shadowAlpha;

		[Sync]
		readonly WAngle angle;
		[Sync]
		readonly WDist speed;
		[Sync]
		readonly WAngle facing;

		readonly string trailPalette;
		readonly string paletteName;

		readonly ContrailRenderable contrail;

		[Sync]
		WPos pos, lastPos, target, source;
		int length;
		int ticks, smokeTicks;
		int remainingBounces;

		public Actor SourceActor { get { return args.SourceActor; } }

		public BulletAS(BulletASInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;
			pos = args.Source;
			source = args.Source;

			var world = args.SourceActor.World;

			paletteName = info.Palette;
			if (info.IsPlayerPalette)
				paletteName += args.SourceActor.Owner.InternalName;

			if (info.LaunchAngle.Length > 1)
				angle = new WAngle(world.SharedRandom.Next(info.LaunchAngle[0].Angle, info.LaunchAngle[1].Angle));
			else
				angle = info.LaunchAngle[0];

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			target = args.PassiveTarget;
			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(world.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}

			if (info.AirburstAltitude > WDist.Zero)
			{
				target += new WVec(WDist.Zero, WDist.Zero, info.AirburstAltitude);
			}

			facing = (target - pos).Yaw;
			length = Math.Max((target - pos).Length / speed.Length, 1);

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
			remainingBounces = info.BounceCount;

			shadowColor = new float3(info.ShadowColor.R, info.ShadowColor.G, info.ShadowColor.B) / 255f;
			shadowAlpha = info.ShadowColor.A / 255f;
		}

		WAngle GetEffectiveFacing()
		{
			var at = (float)ticks / (length - 1);
			var attitude = angle.Tan() * (1 - 2 * at) / (4 * 1024);

			var u = facing.Angle % 512 / 512f;
			var scale = 2048 * u * (1 - u);

			var effective = (int)(facing.Angle < 512
				? facing.Angle - scale * attitude
				: facing.Angle + scale * attitude);

			return new WAngle(effective);
		}

		public void Tick(World world)
		{
			anim?.Tick();

			lastPos = pos;
			pos = WPos.LerpQuadratic(source, target, angle, ticks, length);

			if (ShouldExplode(world))
				Explode(world);
		}

		bool ShouldExplode(World world)
		{
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, lastPos, pos, info.Width, out var blockedPos))
			{
				pos = blockedPos;
				return true;
			}

			if (!string.IsNullOrEmpty(info.TrailImage) && --smokeTicks < 0)
			{
				var delayedPos = WPos.LerpQuadratic(source, target, angle, ticks - info.TrailDelay, length);
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(delayedPos, GetEffectiveFacing(), w,
					info.TrailImage, info.TrailSequences.Random(world.SharedRandom), trailPalette)));

				smokeTicks = info.TrailInterval;
			}

			if (info.ContrailLength > 0)
				contrail.Update(pos);

			var flightLengthReached = ticks++ >= length;
			var shouldBounce = remainingBounces > 0;

			if (flightLengthReached && shouldBounce)
			{
				var cell = world.Map.CellContaining(pos);
				if (!world.Map.Contains(cell))
					return true;

				if (info.InvalidBounceTerrain.Contains(world.Map.GetTerrainInfo(cell).Type))
					return true;

				if (AnyValidTargetsInRadius(world, pos, info.Width, args.SourceActor, true))
					return true;

				target += (pos - source) * info.BounceRangeModifier / 100;
				var dat = world.Map.DistanceAboveTerrain(target);
				target += new WVec(0, 0, -dat.Length);
				length = Math.Max((target - pos).Length / speed.Length, 1);

				ticks = 0;
				source = pos;
				Game.Sound.Play(SoundType.World, info.BounceSound, source);
				remainingBounces--;
			}

			// Flight length reached / exceeded
			if (flightLengthReached && !shouldBounce)
				return true;

			// Driving into cell with different height level
			if (world.Map.DistanceAboveTerrain(pos) < info.ExplodeUnderThisAltitude)
				return true;

			// After first bounce, check for targets each tick
			if (remainingBounces < info.BounceCount && AnyValidTargetsInRadius(world, pos, info.Width, args.SourceActor, true))
				return true;

			if (!info.PointDefenseTypes.IsEmpty && world.ActorsWithTrait<IPointDefense>().Any(x => x.Trait.Destroy(pos, args.SourceActor.Owner, info.PointDefenseTypes)))
				return true;

			return false;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (info.ContrailLength > 0)
				yield return contrail;

			if (anim == null || ticks >= length)
				yield break;

			var world = args.SourceActor.World;
			if (!world.FogObscures(pos))
			{
				var palette = wr.Palette(paletteName);

				if (info.Shadow)
				{
					var dat = world.Map.DistanceAboveTerrain(pos);
					var shadowPos = pos - new WVec(0, 0, dat.Length);
					foreach (var r in anim.Render(shadowPos, palette))
						yield return ((IModifyableRenderable)r)
							.WithTint(shadowColor, ((IModifyableRenderable)r).TintModifiers | TintModifiers.ReplaceColor)
							.WithAlpha(shadowAlpha);
				}

				foreach (var r in anim.Render(pos, palette))
					yield return r;
			}
		}

		void Explode(World world)
		{
			if (info.ContrailLength > 0)
				world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, contrail)));

			world.AddFrameEndTask(w => w.Remove(this));

			var warheadArgs = new WarheadArgs(args)
			{
				ImpactOrientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(lastPos, pos), args.Facing),
				ImpactPosition = pos,
			};

			args.Weapon.Impact(Target.FromPos(pos), warheadArgs);
		}

		bool AnyValidTargetsInRadius(World world, WPos pos, WDist radius, Actor firedBy, bool checkTargetType)
		{
			foreach (var victim in world.FindActorsInCircle(pos, radius))
			{
				if (checkTargetType && !Target.FromActor(victim).IsValidFor(firedBy))
					continue;

				if (!info.ValidBounceBlockerStances.HasRelationship(firedBy.Owner.RelationshipWith(victim.Owner)))
					continue;

				// If the impact position is within any actor's HitShape, we have a direct hit
				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (activeShapes.Any(i => i.DistanceFromEdge(victim, pos).Length <= 0))
					return true;
			}

			return false;
		}
	}
}
