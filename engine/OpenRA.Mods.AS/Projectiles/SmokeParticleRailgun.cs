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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Projectiles
{
	[Desc("Laser effect with helix coiling made of sprite animations around.")]
	public class SmokeParticleRailgunInfo : IProjectileInfo, ISmokeParticleInfo, IRulesetLoaded<WeaponInfo>
	{
		[Desc("The width of the main trajectory used for damaging.",
			"Leave it on 0 to disable line damage and deliver damage only at the target position.")]
		public readonly WDist LineWidth = WDist.Zero;

		[Desc("Maximum offset at the maximum range.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Can this projectile be blocked when hitting actors with an IBlocksProjectiles trait.")]
		public readonly bool Blockable = false;

		[Desc("Duration of the beam.")]
		public readonly int Duration = 15;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("The width of an optional laser beam.")]
		public readonly WDist BeamWidth = new(86);

		[Desc("The shape of the beam.  Accepts values Cylindrical or Flat.")]
		public readonly BeamRenderableShape BeamShape = BeamRenderableShape.Cylindrical;

		[Desc("Beam color in (A),R,G,B.")]
		public readonly Color BeamColor = Color.FromArgb(128, 255, 255, 255);

		[Desc("When true, this will override BeamColor parameter and draw the laser with player color."
			+ " (Still uses BeamColor's alpha information)")]
		public readonly bool BeamPlayerColor = false;

		[Desc("Beam alpha gets + this value per tick during drawing; hence negative value makes it fade over time.")]
		public readonly int BeamAlphaDeltaPerTick = -8;

		[Desc("The radius of the spiral effect. (WDist)")]
		public readonly WDist HelixRadius = new(64);

		[Desc("Height of one complete helix turn, measured parallel to the axis of the helix (WDist)")]
		public readonly WDist HelixPitch = new(512);

		[Desc("Draw each cycle of helix with this many quantization steps")]
		public readonly int QuantizationCount = 16;

		[Desc("Helix animation.")]
		public readonly string HelixImage = "particles";

		[Desc("Sequence of helix animation to use at the start.")]
		[SequenceReference(nameof(HelixImage))]
		public readonly string[] HelixStartSequences;

		[FieldLoader.Require]
		[Desc("Sequence of helix animation to use.")]
		[SequenceReference(nameof(HelixImage))]
		public readonly string[] HelixSequences;

		[Desc("Sequence of helix animation to use at the end.")]
		[SequenceReference(nameof(HelixImage))]
		public readonly string[] HelixEndSequences;

		[PaletteReference(nameof(IsHelixPlayerPalette))]
		public readonly string HelixPalette = "effect";

		public readonly bool IsHelixPlayerPalette = false;

		[FieldLoader.Require]
		[Desc("The duration of an individual particle. Two values mean actual lifetime will vary between them.")]
		public readonly int[] HelixDuration;

		[Desc("Randomize particle forward movement.")]
		public readonly WDist[] HelixSpeed = { WDist.Zero };

		[Desc("Randomize particle gravity.")]
		public readonly WDist[] HelixGravity = { WDist.Zero };

		[Desc("Randomize particle turnrate.")]
		public readonly int HelixTurnRate = 0;

		[Desc("Randomize particle facing.")]
		public readonly bool HelixRandomFacing = true;

		[Desc("Rate to reset particle movement properties.")]
		public readonly int HelixRandomRate = 4;

		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml, if defined, as well.")]
		public readonly string HelixWeapon = null;

		[Desc("Impact animation.")]
		public readonly string HitAnim = null;

		[Desc("Sequence of impact animation to use.")]
		[SequenceReference(nameof(HitAnim), allowNullImage: true)]
		public readonly string HitAnimSequence = "idle";

		[PaletteReference]
		public readonly string HitAnimPalette = "effect";

		WeaponInfo helixWeapon;

		public IProjectile Create(ProjectileArgs args)
		{
			var bc = BeamPlayerColor ? Color.FromArgb(BeamColor.A, args.SourceActor.Owner.Color) : BeamColor;
			return new SmokeParticleRailgun(args, this, bc);
		}

		string ISmokeParticleInfo.Image
		{
			get { return HelixImage; }
		}

		string[] ISmokeParticleInfo.StartSequences
		{
			get { return HelixStartSequences; }
		}

		string[] ISmokeParticleInfo.Sequences
		{
			get { return HelixSequences; }
		}

		string[] ISmokeParticleInfo.EndSequences
		{
			get { return HelixEndSequences; }
		}

		string ISmokeParticleInfo.Palette
		{
			get { return HelixPalette; }
		}

		bool ISmokeParticleInfo.IsPlayerPalette
		{
			get { return IsHelixPlayerPalette; }
		}

		WDist[] ISmokeParticleInfo.Speed
		{
			get { return HelixSpeed; }
		}

		int[] ISmokeParticleInfo.Duration
		{
			get { return HelixDuration; }
		}

		WDist[] ISmokeParticleInfo.Gravity
		{
			get { return HelixGravity; }
		}

		WeaponInfo ISmokeParticleInfo.Weapon
		{
			get { return helixWeapon; }
		}

		int ISmokeParticleInfo.TurnRate
		{
			get { return HelixTurnRate; }
		}

		int ISmokeParticleInfo.RandomRate
		{
			get { return HelixRandomRate; }
		}

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (string.IsNullOrEmpty(HelixWeapon))
				return;

			if (!rules.Weapons.TryGetValue(HelixWeapon.ToLowerInvariant(), out helixWeapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{HelixWeapon.ToLowerInvariant()}'");
		}
	}

	public class SmokeParticleRailgun : IProjectile
	{
		readonly ProjectileArgs args;
		readonly SmokeParticleRailgunInfo info;
		readonly Animation hitanim;
		public readonly Color BeamColor;

		int ticks = 0;
		bool animationComplete;
		WPos target;

		int cycleCount;
		WVec forwardStep;
		WVec leftVector;
		WVec upVector;
		WAngle angleStep;

		public SmokeParticleRailgun(ProjectileArgs args, SmokeParticleRailgunInfo info, Color beamColor)
		{
			this.args = args;
			this.info = info;
			target = args.PassiveTarget;
			BeamColor = beamColor;

			if (!string.IsNullOrEmpty(info.HitAnim))
				hitanim = new Animation(args.SourceActor.World, info.HitAnim);

			CalculateVectors();

			var pos = args.Source;
			var angle = WAngle.Zero;
			for (var i = cycleCount * info.QuantizationCount - 1; i >= 0; i--)
			{
				// Make it narrower near the end.
				var rad = i < info.QuantizationCount ? info.HelixRadius / 4 :
					i < 2 * info.QuantizationCount ? info.HelixRadius / 2 :
					info.HelixRadius;

				// Note: WAngle.Sin(x) = 1024 * Math.Sin(2pi/1024 * x)
				var offset = rad.Length * angle.Cos() * leftVector / (1024 * 1024)
					+ rad.Length * angle.Sin() * upVector / (1024 * 1024);
				var animpos = pos + offset;
				args.SourceActor.World.AddFrameEndTask(w => w.Add(new SmokeParticle(args.SourceActor, info, animpos, info.HelixRandomFacing ? -1 : angle.Facing)));

				pos += forwardStep;
				angle += angleStep;
			}
		}

		void CalculateVectors()
		{
			// Check for blocking actors
			if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(args.SourceActor.World, args.SourceActor.Owner, target, args.Source,
					info.LineWidth, out var blockedPos))
				target = blockedPos;

			// Note: WAngle.Sin(x) = 1024 * Math.Sin(2pi/1024 * x)
			angleStep = new WAngle(1024 / info.QuantizationCount);

			var sourceToTarget = target - args.Source;

			// Forward step, pointing from src to target.
			// QuantizationCont * forwardStep == One cycle of beam in src2target direction.
			forwardStep = info.HelixPitch.Length * sourceToTarget / (info.QuantizationCount * sourceToTarget.Length);

			if (forwardStep == WVec.Zero)
				return;

			// An easy vector to find which is perpendicular vector to forwardStep, with 0 Z component
			leftVector = new WVec(forwardStep.Y, -forwardStep.X, 0);
			leftVector = 1024 * leftVector / leftVector.Length;

			// Vector that is pointing upwards from the ground
			upVector = leftVector.Length != 0
					? new WVec(
					-forwardStep.X * forwardStep.Z,
					-forwardStep.Z * forwardStep.Y,
					forwardStep.X * forwardStep.X + forwardStep.Y * forwardStep.Y)
					: new WVec(forwardStep.Z, forwardStep.Z, 0);
			upVector = 1024 * upVector / upVector.Length;

			//// LeftVector and UpVector are unit vectors of size 1024.

			cycleCount = sourceToTarget.Length / info.HelixPitch.Length;
			if (sourceToTarget.Length % info.HelixPitch.Length != 0)
				cycleCount++; // math.ceil, int version.
		}

		public void Tick(World world)
		{
			if (ticks == 0)
			{
				if (hitanim != null)
					hitanim.PlayThen(info.HitAnimSequence, () => animationComplete = true);
				else
					animationComplete = true;

				if (info.LineWidth.Length > 0)
				{
					var actors = world.FindActorsOnLine(args.Source, target, info.LineWidth);
					foreach (var a in actors)
					{
						var warheadArgs = new WarheadArgs(args)
						{
							ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(args.Source, target), args.Facing),

							// Calculating an impact position is bogus for line damage.
							// FindActorsOnLine guarantees that the beam touches the target's HitShape,
							// so we just assume a center hit to avoid bogus warhead recalculations.
							ImpactPosition = a.CenterPosition,
						};

						args.Weapon.Impact(Target.FromActor(a), warheadArgs);
					}
				}
				else
				{
					var warheadArgs = new WarheadArgs(args)
					{
						ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(args.Source, target), args.Facing),
						ImpactPosition = target,
					};

					args.Weapon.Impact(Target.FromPos(target), warheadArgs);
				}
			}

			hitanim?.Tick();

			if (ticks++ > info.Duration && animationComplete)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) &&
				wr.World.FogObscures(args.Source))
				yield break;

			if (info.BeamWidth.Length > 0 && ticks < info.Duration)
			{
				yield return new BeamRenderable(args.Source, info.ZOffset, args.PassiveTarget - args.Source, info.BeamShape, info.BeamWidth,
					Color.FromArgb(BeamColor.A + info.BeamAlphaDeltaPerTick * ticks, BeamColor));
			}

			if (hitanim != null)
				foreach (var r in hitanim.Render(target, wr.Palette(info.HitAnimPalette)))
					yield return r;
		}
	}
}
