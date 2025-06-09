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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor explodes when killed and the kill XP goes to the parent actor.",
		"Hack: Explodes cannot pass XP because it is KIA and XP cannot pass in INotifyKilled, we will use this instead.")]
	public class ExplodesForMasterInfo : FireWarheadsOnDeathInfo
	{
		[Desc("Armament used by parent or master. Share the same modifier.")]
		public readonly string MasterArmamentName = null;

		[Desc("Allow share the same modifier from mindcontrol master.")]
		public readonly bool AllowShareFromMindControlMaster = false;

		[Desc("Allow share the same modifier from parent actor.")]
		public readonly bool AllowShareFromParent = true;

		public override object Create(ActorInitializer init) { return new ExplodesForMaster(this, init.Self); }
	}

	public class ExplodesForMaster : ConditionalTrait<ExplodesForMasterInfo>, INotifyKilled, INotifyDamage
	{
		readonly Health health;
		BuildingInfo buildingInfo;
		Armament[] armaments;

		public ExplodesForMaster(ExplodesForMasterInfo info, Actor self)
			: base(info)
		{
			health = self.Trait<Health>();
		}

		protected override void Created(Actor self)
		{
			buildingInfo = self.Info.TraitInfoOrDefault<BuildingInfo>();
			armaments = self.TraitsImplementing<Armament>().ToArray();

			base.Created(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > Info.Chance)
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			var weapon = ChooseWeaponForExplosion(self);
			if (weapon == null)
				return;

			if (weapon.Report != null && weapon.Report.Length > 0)
				Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition, weapon.SoundVolume);

			Actor attacker = null;
			var modifierActor = self;
			foreach (var mindControllable in self.TraitsImplementing<MindControllable>())
				if (mindControllable.MasterWhenDie != null && !mindControllable.MasterWhenDie.IsDead)
				{
					attacker = mindControllable.MasterWhenDie;
					modifierActor = Info.AllowShareFromMindControlMaster ? attacker : self;
					break;
				}

			if (attacker == null || attacker.IsDead)
			{
				attacker = self.TraitOrDefault<HasParent>()?.Parent;
				if (attacker == null || attacker.IsDead)
					attacker = self;
				else
					modifierActor = Info.AllowShareFromParent ? attacker : self;
			}

			var args = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = WAngle.Zero,
				CurrentMuzzleFacing = () => WAngle.Zero,

				DamageModifiers = Info.MasterArmamentName != null && !modifierActor.IsDead ? modifierActor.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier(Info.MasterArmamentName)).ToArray() : Array.Empty<int>(),

				InaccuracyModifiers = Array.Empty<int>(),

				RangeModifiers = Array.Empty<int>(),

				Source = self.CenterPosition,
				CurrentSource = () => self.CenterPosition,
				SourceActor = attacker,
				PassiveTarget = self.CenterPosition
			};

			if (Info.Type == ExplosionType.Footprint && buildingInfo != null)
			{
				var cells = buildingInfo.OccupiedTiles(self.Location);
				foreach (var c in cells)
					weapon.Impact(Target.FromPos(self.World.Map.CenterOfCell(c)), new WarheadArgs(args));

				return;
			}

			// Use .FromPos since this actor is killed. Cannot use Target.FromActor
			weapon.Impact(Target.FromPos(self.CenterPosition), new WarheadArgs(args));
		}

		WeaponInfo ChooseWeaponForExplosion(Actor self)
		{
			if (armaments.Length == 0)
				return Info.WeaponInfo;
			else if (self.World.SharedRandom.Next(100) > Info.LoadedChance)
				return Info.EmptyWeaponInfo;

			// PERF: Avoid LINQ
			foreach (var a in armaments)
				if (!a.IsReloading)
					return Info.WeaponInfo;

			return Info.EmptyWeaponInfo;
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (Info.DamageThreshold == 0 || IsTraitDisabled || !self.IsInWorld)
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			// Cast to long to avoid overflow when multiplying by the health
			var source = Info.DamageSource == DamageSource.Self ? self : e.Attacker;
			if (health.HP * 100L < Info.DamageThreshold * (long)health.MaxHP)
				self.World.AddFrameEndTask(w => self.Kill(source, e.Damage.DamageTypes));
		}
	}
}
