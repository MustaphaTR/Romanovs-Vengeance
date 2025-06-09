#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	class ExplodeWeaponTeleportEffectInfo : ConditionalTraitInfo, IRulesetLoaded
	{
		[Desc("Effect only works when teleport with this teleport type.")]
		public readonly string TeleportType = "RA2ChronoPower";

		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Default weapon to use for explosion.")]
		public readonly string ImpactWeapon = null;

		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Default weapon to use for explosion. Use Weapon if not set.")]
		public readonly string TeleportWeapon = null;

		public WeaponInfo ImpactWeaponInfo { get; private set; }
		public WeaponInfo TeleportWeaponInfo { get; private set; }

		[Desc("Weapon offset relative to actor's position.")]
		public readonly WVec LocalOffset = WVec.Zero;

		public override object Create(ActorInitializer init) { return new ExplodeWeaponTeleportEffect(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(ImpactWeapon))
			{
				var weaponToLower = ImpactWeapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");
				ImpactWeaponInfo = weapon;
			}

			if (!string.IsNullOrEmpty(TeleportWeapon))
			{
				var weaponToLower = TeleportWeapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");
				TeleportWeaponInfo = weapon;
			}

			base.RulesetLoaded(rules, ai);
		}
	}

	sealed class ExplodeWeaponTeleportEffect : ConditionalTrait<ExplodeWeaponTeleportEffectInfo>, IOnSuccessfulTeleportRA2
	{
		readonly Actor self;

		public ExplodeWeaponTeleportEffect(Actor self, ExplodeWeaponTeleportEffectInfo info)
			: base(info)
		{
			this.self = self;
		}

		void IOnSuccessfulTeleportRA2.OnSuccessfulTeleport(string type, WPos oldPos, WPos newPos)
		{
			if (type != Info.TeleportType || IsTraitDisabled)
				return;

			// Generate a weapon on the place of impact, Generate a weapon on the place of teleport
			var weapon = Info.TeleportWeaponInfo;
			var weapon2 = Info.ImpactWeaponInfo;
			var firer = self;

			self.World.AddFrameEndTask(w =>
			{
				if (weapon.Report != null && weapon.Report.Length > 0)
				{
					if (weapon.AudibleThroughFog || (!self.World.ShroudObscures(newPos) && !self.World.FogObscures(newPos)))
						Game.Sound.Play(SoundType.World, weapon.Report, self.World, newPos, null, weapon.SoundVolume);
				}

				weapon.Impact(Target.FromPos(newPos), firer);

				if (weapon2.Report != null && weapon2.Report.Length > 0)
				{
					if (weapon2.AudibleThroughFog || (!self.World.ShroudObscures(oldPos) && !self.World.FogObscures(oldPos)))
						Game.Sound.Play(SoundType.World, weapon2.Report, self.World, oldPos, null, weapon2.SoundVolume);
				}

				weapon2.Impact(Target.FromPos(oldPos), firer);
			});
		}
	}
}
