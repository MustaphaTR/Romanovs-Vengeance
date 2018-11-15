#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Handle infection by infectior units.")]
	public class InfectableInfo : ITraitInfo, Requires<HealthInfo>
	{
		[Desc("If true and this actor has EjectOnDeath, no actor will be spawned.")]
		public readonly bool PreventsEjectOnDeath = false;

        [Desc("Damage types that removes the infector.")]
        public readonly BitSet<DamageType> RemoveInfectorDamageTypes = default(BitSet<DamageType>);

        [Desc("Damage types that kills the infector.")]
        public readonly BitSet<DamageType> KillInfectorDamageTypes = default(BitSet<DamageType>);

        [GrantedConditionReference]
        [Desc("The condition to grant to self while infected by any actor.")]
        public readonly string InfectedCondition = null;

        [Desc("Conditions to grant when infected by specified actors.",
            "A dictionary of [actor id]: [condition].")]
        public readonly Dictionary<string, string> InfectedByConditions = new Dictionary<string, string>();

        [GrantedConditionReference]
        public IEnumerable<string> LinterConditions { get { return InfectedByConditions.Values; } }

        public object Create(ActorInitializer init) { return new Infectable(init.Self, this); }
	}

    public class Infectable : ITick, INotifyCreated, INotifyDamage, INotifyKilled, IPreventsEjectOnDeath
    {
        readonly InfectableInfo info;
        readonly Health health;

        public Actor Infector;
        public Infector InfectorTrait;
        [Sync] public int Ticks;

        ConditionManager conditionManager;
        int infectedToken = ConditionManager.InvalidConditionToken;
        int infectedByToken = ConditionManager.InvalidConditionToken;

        bool killInfector = false;

        public Infectable(Actor self, InfectableInfo info)
        {
            this.info = info;

            health = self.Trait<Health>();
        }

        void INotifyCreated.Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
        }

        public bool PreventsEjectOnDeath(Actor self)
        {
            return info.PreventsEjectOnDeath;
        }

        public void GrantCondition(Actor self)
        {
            if (conditionManager != null)
            {
                if (infectedToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.InfectedCondition))
                    infectedToken = conditionManager.GrantCondition(self, info.InfectedCondition);

                string infectedByCondition;
                if (info.InfectedByConditions.TryGetValue(Infector.Info.Name, out infectedByCondition))
                    infectedByToken = conditionManager.GrantCondition(self, infectedByCondition);
            }
        }

        public void RevokeCondition(Actor self)
        {
            if (conditionManager != null)
            {
                if (infectedToken != ConditionManager.InvalidConditionToken)
                    infectedToken = conditionManager.RevokeCondition(self, infectedToken);

                if (infectedByToken != ConditionManager.InvalidConditionToken)
                    infectedByToken = conditionManager.RevokeCondition(self, infectedByToken);
            }
        }

        public void RemoveInfector(Actor self, bool kill, AttackInfo e = null)
        {
            if (Infector != null && !Infector.IsDead)
            {
                Infector.TraitOrDefault<IPositionable>().SetPosition(Infector, self.CenterPosition);
                self.World.AddFrameEndTask(w =>
                {
                    w.Add(Infector);

                    if (kill)
                        Infector.Kill(e.Attacker, e.Damage.DamageTypes);

                    RevokeCondition(self);
                    Infector = null;
                    InfectorTrait = null;
                    killInfector = false;
                });
            }

        }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            if (Infector != null)
            {
                if (e.Attacker != Infector)
                {
                    var threshold = InfectorTrait.Info.SuppressionThreshold;
                    if (threshold > 0 && e.Damage.Value > threshold)
                        killInfector = true;
                }
                else
                {
                    if (InfectorTrait.Info.KillState.Contains(e.DamageState))
                    {
                        self.World.AddFrameEndTask(w => health.Kill(self, Infector, InfectorTrait.Info.DamageTypes));
                    }
                }

                if (e.Damage.DamageTypes.Overlaps(info.KillInfectorDamageTypes))
                    RemoveInfector(self, true, e);
                else if (e.Damage.DamageTypes.Overlaps(info.RemoveInfectorDamageTypes))
                    RemoveInfector(self, false, e);
            }
        }

        void INotifyKilled.Killed(Actor self, AttackInfo e)
        {
            RemoveInfector(self, killInfector, e);
        }

        void ITick.Tick(Actor self)
        {
            if (Infector != null)
            {
                if (--Ticks < 0)
                {
                    health.InflictDamage(self, Infector, new Damage(InfectorTrait.Info.Damage, InfectorTrait.Info.DamageTypes), false);

                    Ticks = InfectorTrait.Info.DamageInterval;
                }
            }
        }
	}
}