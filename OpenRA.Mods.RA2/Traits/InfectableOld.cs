#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Handle infection by infectior units.")]
	public class InfectableOldInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[Desc("Damage types that removes the infector.")]
		public readonly BitSet<DamageType> RemoveInfectorDamageTypes = default(BitSet<DamageType>);

		[Desc("Damage types that kills the infector.")]
		public readonly BitSet<DamageType> KillInfectorDamageTypes = default(BitSet<DamageType>);

		[Desc("Actor types that kills the infector." +
			"Define service depots here, since Repairable don't deal DamageTypes.")]
		public readonly HashSet<string> KillInfectorActorTypes = new HashSet<string> { };

		[GrantedConditionReference]
		[Desc("The condition to grant to self while infected by any actor.")]
		public readonly string InfectedCondition = null;

		[GrantedConditionReference]
		[Desc("Condition granted when being infected by another actor.")]
		public readonly string BeingInfectedCondition = null;

		[Desc("Conditions to grant when infected by specified actors.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> InfectedByConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions { get { return InfectedByConditions.Values; } }

		public override object Create(ActorInitializer init) { return new InfectableOld(init.Self, this); }
	}

	public class InfectableOld : ConditionalTrait<InfectableOldInfo>, ISync, ITick, INotifyDamage, INotifyKilled, IRemoveInfector
	{
		readonly Health health;

		public Actor Infector;
		public InfectorOld InfectorTrait;
		public int[] FirepowerMultipliers = new int[] { };

		[Sync]
		public int Ticks;

		int beingInfectedToken = Actor.InvalidConditionToken;
		int infectedToken = Actor.InvalidConditionToken;
		int infectedByToken = Actor.InvalidConditionToken;

		int dealthDamage = 0;

		public InfectableOld(Actor self, InfectableOldInfo info)
            : base(info)
        {
			health = self.Trait<Health>();
		}

		public void GrantCondition(Actor self, bool infecting = false)
		{
			if (infecting)
			{
				if (beingInfectedToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.BeingInfectedCondition))
					beingInfectedToken = self.GrantCondition(Info.BeingInfectedCondition);
			}
			else
			{
				if (infectedToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.InfectedCondition))
					infectedToken = self.GrantCondition(Info.InfectedCondition);

				string infectedByCondition;
				if (Info.InfectedByConditions.TryGetValue(Infector.Info.Name, out infectedByCondition))
					infectedByToken = self.GrantCondition(infectedByCondition);
			}
		}

		public void RevokeCondition(Actor self, bool infecting = false)
		{
			if (infecting)
			{
				if (beingInfectedToken != Actor.InvalidConditionToken)
					beingInfectedToken = self.RevokeCondition(beingInfectedToken);
			}
			else
			{
				if (infectedToken != Actor.InvalidConditionToken)
					infectedToken = self.RevokeCondition(infectedToken);

				if (infectedByToken != Actor.InvalidConditionToken)
					infectedByToken = self.RevokeCondition(infectedByToken);
			}
		}

		public void RemoveInfector(Actor self, bool kill, AttackInfo e)
		{
			if (Infector != null && !Infector.IsDead)
			{
				Infector.TraitOrDefault<IPositionable>().SetPosition(Infector, self.CenterPosition);
				self.World.AddFrameEndTask(w =>
				{
					w.Add(Infector);

					if (kill)
						Infector.Kill(e.Attacker, e.Damage.DamageTypes);
					else
						Infector.QueueActivity(false, new Move(Infector, self.Location));

					RevokeCondition(self);
					Infector = null;
					InfectorTrait = null;
					FirepowerMultipliers = new int[] { };
					dealthDamage = 0;
				});
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (Infector != null)
			{
				if (e.Attacker != Infector)
				{
					var damageThreshold = InfectorTrait.Info.SuppressionDamageThreshold;
					if (damageThreshold > 0 && e.Damage.Value > damageThreshold)
						dealthDamage++;
				}
				else
				{
					if (InfectorTrait.Info.KillState.Contains(e.DamageState))
					{
						self.World.AddFrameEndTask(w => health.Kill(self, Infector, InfectorTrait.Info.DamageTypes));
					}
				}

				if (e.Damage.DamageTypes.Overlaps(Info.KillInfectorDamageTypes) ||
					Info.KillInfectorActorTypes.Contains(e.Attacker.Info.Name))
					RemoveInfector(self, true, e);
				else if (e.Damage.DamageTypes.Overlaps(Info.RemoveInfectorDamageTypes))
					RemoveInfector(self, false, e);
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
        {
            if (InfectorTrait != null)
            {
                var kill = dealthDamage >= InfectorTrait.Info.SuppressionAmountThreshold;
                RemoveInfector(self, kill, e);
            }
		}

		void ITick.Tick(Actor self)
        {
            if (!IsTraitDisabled && Infector != null)
            {
				if (--Ticks < 0)
				{
					var damage = Util.ApplyPercentageModifiers(InfectorTrait.Info.Damage, FirepowerMultipliers);
					health.InflictDamage(self, Infector, new Damage(damage, InfectorTrait.Info.DamageTypes), false);

					Ticks = InfectorTrait.Info.DamageInterval;
				}
			}
		}

		void IRemoveInfector.RemoveInfector(Actor self, bool kill, AttackInfo e)
		{
			RemoveInfector(self, kill, e);
		}
	}
}
