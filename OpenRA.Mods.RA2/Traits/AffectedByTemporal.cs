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

using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
    [Desc("This actor can be affected by temporal warheads.")]
    public class AffectedByTemporalInfo : ITraitInfo
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

        [Desc("If erase delay is calculated from cost, multipile the cost with this to get the time.")]
        public readonly int EraseDamageMultiplier = 100;

        public readonly bool ShowSelectionBar = true;
        public readonly Color SelectionBarColor = Color.Magenta;

        public object Create(ActorInitializer init) { return new AffectedByTemporal(init, this); }
    }

    public class AffectedByTemporal : INotifyCreated, ISync, ITick, ISelectionBar
    {
        Actor self;
        AffectedByTemporalInfo info;
        ConditionManager conditionManager;

        int token = ConditionManager.InvalidConditionToken;
        int requiredDamage;
        int recievedDamage;
        [Sync] int tick;

        public AffectedByTemporal(ActorInitializer init, AffectedByTemporalInfo info)
        {
            this.info = info;
            self = init.Self;
            var health = self.Info.TraitInfoOrDefault<IHealthInfo>();
            requiredDamage = info.EraseDamage >= 0 || health == null ? info.EraseDamage : health.MaxHP * info.EraseDamageMultiplier / 100;
        }

        void INotifyCreated.Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
        }

        public void AddDamage(int damage, Actor damager)
        {
            recievedDamage = recievedDamage + damage;
            tick = info.RevokeDelay;

            if (recievedDamage >= requiredDamage)
            {
                self.Dispose();

                if (info.EraseSounds.Any())
                {
                    var sound = info.EraseSounds.Random(self.World.LocalRandom);
                    Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
                }
            }

            if (conditionManager != null &&
                !string.IsNullOrEmpty(info.Condition) &&
                token == ConditionManager.InvalidConditionToken)
                token = conditionManager.GrantCondition(self, info.Condition);
        }

        void ITick.Tick(Actor self)
        {
            if (--tick < 0)
            {
                recievedDamage = 0;

                if (conditionManager != null && token != ConditionManager.InvalidConditionToken)
                    token = conditionManager.RevokeCondition(self, token);
            }
        }

        float ISelectionBar.GetValue()
        {
            if (!info.ShowSelectionBar)
                return 0;

            return (float)recievedDamage / requiredDamage;
        }

        bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

        Color ISelectionBar.GetColor() { return info.SelectionBarColor; }
    }
}
