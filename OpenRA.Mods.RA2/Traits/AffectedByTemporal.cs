#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("This actor can be affected by temporal warheads.")]
	public class AffectedByTemporalInfo : ConditionalTraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition type to grant when the actor is affected.")]
		public readonly string Condition = null;

		[Desc("Amount of ticks required to pass without being damaged to revoke the affect of the temporal weapon.")]
		public readonly int RevokeDelay = 1;

		[Desc("Amount of damage required to be taked for the unit to be killed.",
			"Use -1 to be calculated from the actor health.")]
		public readonly int EraseDamage = -1;

		[Desc("List of sound which one randomly played after erasing is done.")]
		public readonly string[] EraseSounds = { };

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the sounds played at.")]
		public readonly float SoundVolume = 1f;

		[Desc("If erase delay is calculated from health, multipile the cost with this to get the time.")]
		public readonly int EraseDamageMultiplier = 100;

		[Desc("Remove the erased actor from the world (and destroy it) instead of killing it.")]
		public readonly bool RemoveInstead = false;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.Magenta;

		public override object Create(ActorInitializer init) { return new AffectedByTemporal(init, this); }
	}

	public class AffectedByTemporal : ConditionalTrait<AffectedByTemporalInfo>, ISync, ITick, ISelectionBar
	{
		Actor self;

		int token = Actor.InvalidConditionToken;
		int requiredDamage;
		int recievedDamage;

		[Sync]
		int tick;

		public AffectedByTemporal(ActorInitializer init, AffectedByTemporalInfo info)
			: base(info)
		{
			self = init.Self;
			var health = self.Info.TraitInfoOrDefault<IHealthInfo>();
			requiredDamage = info.EraseDamage >= 0 || health == null ? info.EraseDamage : health.MaxHP * info.EraseDamageMultiplier / 100;
		}

		public void AddDamage(int damage, Actor damager, BitSet<DamageType> damageTypes)
		{
			if (IsTraitDisabled)
				return;

			recievedDamage = recievedDamage + damage;
			tick = Info.RevokeDelay;

			if (recievedDamage >= requiredDamage)
			{
				if (Info.RemoveInstead)
					self.Dispose();
				else
					self.Kill(damager, damageTypes);

				if (Info.EraseSounds.Any())
				{
					var pos = self.CenterPosition;
					if (Info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					{
						var sound = Info.EraseSounds.Random(self.World.LocalRandom);
						Game.Sound.Play(SoundType.World, sound, pos, Info.SoundVolume);
					}
				}
			}

			if (!string.IsNullOrEmpty(Info.Condition) &&
				token == Actor.InvalidConditionToken)
				token = self.GrantCondition(Info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (--tick < 0)
			{
				recievedDamage = 0;

				if (token != Actor.InvalidConditionToken)
					token = self.RevokeCondition(token);
			}
		}

		float ISelectionBar.GetValue()
		{
			if (!Info.ShowSelectionBar)
				return 0;

			return (float)recievedDamage / requiredDamage;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }

		protected override void TraitDisabled(Actor self)
		{
			recievedDamage = 0;

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
