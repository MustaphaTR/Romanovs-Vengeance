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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	public enum FireMode
	{
		Spread,
		Line,
		Focus
	}

	[Desc("Detonates all warheads attached to Weapon each ExplosionInterval ticks.")]
	public class WarheadTrailProjectileInfo : IProjectileInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Warhead explosion offsets")]
		public readonly WVec[] Offsets = { new(0, 1, 0) };

		[Desc("Projectile speed in WDist / tick, two values indicate variable velocity.")]
		public readonly WDist[] Speed = { new(17) };

		[Desc("Maximum inaccuracy offset.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("How many ticks will pass between explosions.")]
		public readonly int ExplosionInterval = 8;

		[FieldLoader.Require]
		[WeaponReference]
		[Desc("Weapon that's detonated every interval.")]
		public readonly string Weapon = null;

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("If it's true then weapon won't continue firing past the target.")]
		public readonly bool KillProjectilesWhenReachedTargetLocation = false;

		[Desc("Where shall the bullets fly after instantiating? Possible values are Spread, Line and Focus")]
		public readonly FireMode FireMode = FireMode.Spread;

		[Desc("Impact the warheads at ground level, regardless of explosion altitude.")]
		public readonly bool ForceAtGroundLevel = false;

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[Desc("Loop a randomly chosen sequence of Image from this list while this projectile is moving.")]
		[SequenceReference(nameof(Image), allowNullImage: true)]
		public readonly string[] Sequences = { "idle" };

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference]
		public readonly string Palette = "effect";

		[Desc("Does this projectile have a shadow?")]
		public readonly bool Shadow = false;

		[Desc("Palette to use for this projectile's shadow if Shadow is true.")]
		[PaletteReference]
		public readonly string ShadowPalette = "shadow";

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Loop a randomly chosen sequence of TrailImage from this list while this projectile is moving.")]
		[SequenceReference(nameof(TrailImage), allowNullImage: true)]
		public readonly string[] TrailSequences = { "idle" };

		[Desc("Delay in ticks until trail animation is spawned.")]
		public readonly int TrailDelay = 1;

		[Desc("Palette used to render the trail sequence.")]
		[PaletteReference(nameof(TrailUsePlayerPalette))]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

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

		[Desc("Altitude where this bullet should explode when reached.",
			"Negative values allow this bullet to pass cliffs and terrain bumps.")]
		public readonly WDist ExplodeUnderThisAltitude = new(-1536);

		[Desc("Is this blocked by actors with BlocksProjectiles trait.")]
		public readonly bool Blockable = true;

		[Desc("Width of projectile (used for finding blocking actors).")]
		public readonly WDist Width = new(1);

		[Desc("If projectile touches an actor with one of these stances during or after the first bounce, trigger explosion.")]
		public readonly PlayerRelationship ValidBounceBlockerPlayerRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral | PlayerRelationship.Ally;

		public IProjectile Create(ProjectileArgs args) { return new WarheadTrailProjectile(this, args); }

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out var weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{Weapon.ToLowerInvariant()}'");
			WeaponInfo = weapon;
		}
	}

	public class WarheadTrailProjectile : IProjectile, ISync
	{
		readonly WarheadTrailProjectileInfo info;
		readonly ProjectileArgs args;

		[Sync]
		readonly WDist speed;

		readonly WRot offsetRotation;
		readonly int lifespan;
		readonly int mindelay;

		[Sync]
		readonly WPos projectilepos, targetpos, sourcepos, offsetTargetPos = WPos.Zero;

		readonly WarheadTrailProjectileEffect[] projectiles; // offset projectiles

		readonly WPos offsetSourcePos = WPos.Zero;
		readonly World world;

		int ticks;

		public Actor SourceActor { get { return args.SourceActor; } }

		public WarheadTrailProjectile(WarheadTrailProjectileInfo info, ProjectileArgs args)
		{
			this.info = info;
			this.args = args;

			projectilepos = args.Source;
			sourcepos = args.Source;

			var firedBy = args.SourceActor;

			world = args.SourceActor.World;

			if (info.Speed.Length > 1)
				speed = new WDist(world.SharedRandom.Next(info.Speed[0].Length, info.Speed[1].Length));
			else
				speed = info.Speed[0];

			targetpos = GetTargetPos();

			mindelay = args.Weapon.MinRange.Length / speed.Length;

			projectiles = new WarheadTrailProjectileEffect[info.Offsets.Length];
			var range = Common.Util.ApplyPercentageModifiers(args.Weapon.Range.Length, args.RangeModifiers);
			var mainFacing = (targetpos - sourcepos).Yaw.Facing + 64;

			// used for lerping projectiles at the same pace
			var estimatedLifespan = Math.Max(args.Weapon.Range.Length / speed.Length, 1);

			// target that will be assigned
			Target target;

			for (var i = 0; i < info.Offsets.Length; i++)
			{
				switch (info.FireMode)
				{
					case FireMode.Focus:
						offsetRotation = WRot.FromFacing(mainFacing);
						offsetTargetPos = sourcepos + new WVec(range, 0, 0).Rotate(offsetRotation);
						offsetSourcePos = sourcepos + info.Offsets[i].Rotate(offsetRotation);
						break;
					case FireMode.Line:
						offsetRotation = WRot.FromFacing(mainFacing);
						offsetTargetPos = sourcepos + new WVec(range + info.Offsets[i].X, info.Offsets[i].Y, info.Offsets[i].Z).Rotate(offsetRotation);
						offsetSourcePos = sourcepos + info.Offsets[i].Rotate(offsetRotation);
						break;
					case FireMode.Spread:
						offsetRotation = WRot.FromFacing(info.Offsets[i].Yaw.Facing - 64) + WRot.FromFacing(mainFacing);
						offsetSourcePos = sourcepos + info.Offsets[i].Rotate(offsetRotation);
						offsetTargetPos = sourcepos + new WVec(range + info.Offsets[i].X, info.Offsets[i].Y, info.Offsets[i].Z).Rotate(offsetRotation);
						break;
				}

				if (info.Inaccuracy.Length > 0)
				{
					var inaccuracy = Common.Util.ApplyPercentageModifiers(info.Inaccuracy.Length, args.InaccuracyModifiers);
					var maxOffset = inaccuracy * (args.PassiveTarget - projectilepos).Length / range;
					var inaccuracyOffset = WVec.FromPDF(world.SharedRandom, 2) * maxOffset / 1024;
					offsetTargetPos += inaccuracyOffset;
				}

				target = Target.FromPos(offsetTargetPos);

				// If it's true then lifespan is counted from source position to target instead of max range.
				lifespan = info.KillProjectilesWhenReachedTargetLocation
					? Math.Max((args.PassiveTarget - args.Source).Length / speed.Length, 1)
					: estimatedLifespan;

				var facing = (offsetTargetPos - offsetSourcePos).Yaw;
				var projectileArgs = new ProjectileArgs
				{
					Weapon = info.WeaponInfo,
					DamageModifiers = args.DamageModifiers,
					Facing = facing,
					Source = offsetSourcePos,
					CurrentSource = () => offsetSourcePos,
					SourceActor = firedBy,
					GuidedTarget = args.GuidedTarget,
					PassiveTarget = target.CenterPosition
				};

				projectiles[i] = new WarheadTrailProjectileEffect(info, projectileArgs, lifespan, estimatedLifespan, info.ForceAtGroundLevel);
			}

			foreach (var p in projectiles)
				world.AddFrameEndTask(w => w.Add(p));
		}

		// gets where main projectile should fly to
		WPos GetTargetPos()
		{
			var targetpos = args.PassiveTarget;
			var div = (targetpos - sourcepos).Length;

			return WPos.Lerp(sourcepos, targetpos, args.Weapon.Range.Length, div > 0 ? div : 1);
		}

		public void Tick(World world)
		{
			if (ticks % info.ExplosionInterval == 0 && mindelay <= ticks)
				DoImpact();

			if (ticks >= lifespan)
			{
				foreach (var projectile in projectiles)
					projectile.Explode(world);

				world.AddFrameEndTask(w => w.Remove(this));
			}

			ticks++;
		}

		void DoImpact()
		{
			// Trigger all so-far-untriggered explosions.
			foreach (var projectile in projectiles)
				if (!projectile.DetonateSelf)
					projectile.Impact();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield break;
		}
	}
}
