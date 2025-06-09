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
using OpenRA.GameRules;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class SmokeParticleEmitterInfo : ConditionalTraitInfo, ISmokeParticleInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("The duration of an individual particle. Two values mean actual lifetime will vary between them.")]
		public readonly int[] Duration;

		[Desc("Offset for the particle emitter.")]
		public readonly WVec[] Offset = { WVec.Zero };

		[Desc("Randomize particle forward movement.")]
		public readonly WDist[] Speed = { WDist.Zero };

		[Desc("Randomize particle gravity.")]
		public readonly WDist[] Gravity = { WDist.Zero };

		[Desc("Randomize particle facing.")]
		public readonly bool RandomFacing = true;

		[Desc("Randomize particle turnrate.")]
		public readonly int TurnRate = 0;

		[Desc("Rate to reset particle movement properties.")]
		public readonly int RandomRate = 4;

		[Desc("How many particles should spawn.")]
		public readonly int[] SpawnFrequency = { 100, 150 };

		[Desc("Which image to use.")]
		public readonly string Image = "particles";

		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use when the smoke starts.")]
		public readonly string[] StartSequences = Array.Empty<string>();

		[FieldLoader.Require]
		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use while smoke is active.")]
		public readonly string[] Sequences = Array.Empty<string>();

		[SequenceReference(nameof(Image))]
		[Desc("Which sequence to use when the smoke ends.")]
		public readonly string[] EndSequences = Array.Empty<string>();

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Which palette to use.")]
		public readonly string Palette = null;

		public readonly bool IsPlayerPalette = false;

		[WeaponReference]
		[Desc("Has to be defined in weapons.yaml, if defined, as well.")]
		public readonly string Weapon = null;

		public WeaponInfo WeaponInfo { get; private set; }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (string.IsNullOrEmpty(Weapon))
				return;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weaponInfo))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weaponInfo;
		}

		public override object Create(ActorInitializer init) { return new SmokeParticleEmitter(init.Self, this); }

		string ISmokeParticleInfo.Image
		{
			get { return Image; }
		}

		string[] ISmokeParticleInfo.StartSequences
		{
			get { return StartSequences; }
		}

		string[] ISmokeParticleInfo.Sequences
		{
			get { return Sequences; }
		}

		string[] ISmokeParticleInfo.EndSequences
		{
			get { return EndSequences; }
		}

		string ISmokeParticleInfo.Palette
		{
			get { return Palette; }
		}

		bool ISmokeParticleInfo.IsPlayerPalette
		{
			get { return IsPlayerPalette; }
		}

		WDist[] ISmokeParticleInfo.Speed
		{
			get { return Speed; }
		}

		WDist[] ISmokeParticleInfo.Gravity
		{
			get { return Gravity; }
		}

		int[] ISmokeParticleInfo.Duration
		{
			get { return Duration; }
		}

		WeaponInfo ISmokeParticleInfo.Weapon
		{
			get { return WeaponInfo; }
		}

		int ISmokeParticleInfo.TurnRate
		{
			get { return TurnRate; }
		}

		int ISmokeParticleInfo.RandomRate
		{
			get { return RandomRate; }
		}
	}

	public class SmokeParticleEmitter : ConditionalTrait<SmokeParticleEmitterInfo>, ITick
	{
		readonly MersenneTwister random;
		readonly WVec offset;

		IFacing facing;
		int ticks;

		public SmokeParticleEmitter(Actor self, SmokeParticleEmitterInfo info)
			: base(info)
		{
			random = self.World.SharedRandom;

			offset = Info.Offset.Length == 2
				? new WVec(
					random.Next(Info.Offset[0].X, Info.Offset[1].X), random.Next(Info.Offset[0].Y, Info.Offset[1].Y), random.Next(Info.Offset[0].Z, Info.Offset[1].Z))
				: Info.Offset[0];
		}

		protected override void Created(Actor self)
		{
			facing = self.TraitOrDefault<IFacing>();

			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld || IsTraitDisabled)
				return;

			if (--ticks < 0)
			{
				ticks = Info.SpawnFrequency.Length == 2 ? random.Next(Info.SpawnFrequency[0], Info.SpawnFrequency[1]) : Info.SpawnFrequency[0];

				var spawnFacing = (!Info.RandomFacing && facing != null) ? facing.Facing.Facing : -1;

				self.World.AddFrameEndTask(w => w.Add(new SmokeParticle(self, Info, self.CenterPosition + offset, spawnFacing)));
			}
		}
	}
}
