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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
    [Desc("Overrides the default Tooltip when this actor is a mirage (aids in deceiving enemy players).")]
    class MirageTooltipInfo : TooltipInfo, Requires<MirageInfo>
    {
        public override object Create(ActorInitializer init) { return new MirageTooltip(init.Self, this); }
    }

    class MirageTooltip : ConditionalTrait<MirageTooltipInfo>, ITooltip
    {
        readonly Actor self;
        readonly Mirage mirage;

        public MirageTooltip(Actor self, MirageTooltipInfo info)
            : base(info)
        {
            this.self = self;
            mirage = self.Trait<Mirage>();
        }

        public ITooltipInfo TooltipInfo { get { return Info; } }

        public Player Owner
        {
            get
            {
                if (!mirage.IsMirage || self.Owner.IsAlliedWith(self.World.RenderPlayer))
                    return self.Owner;

                return self.World.Players.First(p => p.InternalName == mirage.Info.EffectiveOwner);
            }
        }
    }

    [Flags]
    public enum MirageRevealType
    {
		None = 0,
		Attack = 1,
		Move = 2,
		Unload = 4,
		Infiltrate = 8,
		Demolish = 16,
		Damage = 32,
		Heal = 64,
		SelfHeal = 128,
		Dock = 256
    }

    // Type tag for miragetypes
    public class MirageType { }

    [Desc("This actor can appear as a differnt actor in specific situations.")]
    public class MirageInfo : PausableConditionalTraitInfo
    {
        [Desc("Measured in game ticks.")]
        public readonly int InitialDelay = 10;

        [Desc("Measured in game ticks.")]
        public readonly int MirageDelay = 30;

        [Desc("Events leading to the actor getting revealed. Possible values are: Attack, Move, Unload, Infiltrate, Demolish, Dock, Damage, Heal and SelfHeal.")]
        public readonly MirageRevealType RevealOn = MirageRevealType.Attack
            | MirageRevealType.Unload | MirageRevealType.Infiltrate | MirageRevealType.Demolish | MirageRevealType.Dock;

        public readonly string MirageSound = null;
        public readonly string RevealSound = null;

        [Desc("Map player to use as an Effective Owner when actor is a mirage.")]
        public readonly string EffectiveOwner = "Neutral";

        [PaletteReference("IsPlayerPalette")]
        public readonly string Palette = "cloak";
        public readonly bool IsPlayerPalette = false;

        public readonly BitSet<MirageType> MirageTypes = new BitSet<MirageType>("Mirage");

        [GrantedConditionReference]
        [Desc("The condition to grant to self while a mirage.")]
        public readonly string MirageCondition = null;

        public override object Create(ActorInitializer init) { return new Mirage(init, this); }
    }

    public class Mirage : PausableConditionalTrait<MirageInfo>, INotifyDamage, IEffectiveOwner, INotifyUnload, INotifyDemolition, INotifyInfiltration,
        INotifyAttack, ITick, INotifyCreated, INotifyHarvesterAction
    {
        [Sync]
        private int remainingTime;

        Actor self;

        bool isDocking;
        ConditionManager conditionManager;
        Mirage[] otherMirages;

        CPos? lastPos;
        bool wasMirage = false;
        bool firstTick = true;
        int mirageToken = ConditionManager.InvalidConditionToken;

        public bool Disguised { get { return IsMirage; } }
        public Player Owner { get { return IsMirage ? self.World.Players.First(p => p.InternalName == Info.EffectiveOwner) : null; } }

        public Mirage(ActorInitializer init, MirageInfo info)
            : base(info)
        {
            self = init.Self;
            remainingTime = info.InitialDelay;
        }

        protected override void Created(Actor self)
        {
            conditionManager = self.TraitOrDefault<ConditionManager>();
            otherMirages = self.TraitsImplementing<Mirage>()
                .Where(c => c != this)
                .ToArray();

            if (IsMirage)
            {
                wasMirage = true;
                if (conditionManager != null && mirageToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.MirageCondition))
                    mirageToken = conditionManager.GrantCondition(self, Info.MirageCondition);
            }

            base.Created(self);
        }

        public bool IsMirage { get { return !IsTraitDisabled && !IsTraitPaused && remainingTime <= 0; } }

        public void Reveal() { Reveal(Info.MirageDelay); }

        public void Reveal(int time)
        {
            remainingTime = Math.Max(remainingTime, time);
        }

        void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel) { if (Info.RevealOn.HasFlag(MirageRevealType.Attack)) Reveal(); }

        void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

        void INotifyDamage.Damaged(Actor self, AttackInfo e)
        {
            if (e.Damage.Value == 0)
                return;

            var type = e.Damage.Value < 0
                ? (e.Attacker == self ? MirageRevealType.SelfHeal : MirageRevealType.Heal)
                : MirageRevealType.Damage;
            if (Info.RevealOn.HasFlag(type))
                Reveal();
        }

        void ITick.Tick(Actor self)
        {
            if (!IsTraitDisabled && !IsTraitPaused)
            {
                if (remainingTime > 0 && !isDocking)
                    remainingTime--;

                if (Info.RevealOn.HasFlag(MirageRevealType.Move) && (lastPos == null || lastPos.Value != self.Location))
                {
                    Reveal();
                    lastPos = self.Location;
                }
            }

            var isMirage = IsMirage;
            if (isMirage && !wasMirage)
            {
                if (conditionManager != null && mirageToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.MirageCondition))
                    mirageToken = conditionManager.GrantCondition(self, Info.MirageCondition);

                // Sounds shouldn't play if the actor starts cloaked
                if (!(firstTick && Info.InitialDelay == 0) && !otherMirages.Any(a => a.IsMirage))
                    Game.Sound.Play(SoundType.World, Info.MirageSound, self.CenterPosition);
            }
            else if (!isMirage && wasMirage)
            {
                if (mirageToken != ConditionManager.InvalidConditionToken)
                    mirageToken = conditionManager.RevokeCondition(self, mirageToken);

                if (!(firstTick && Info.InitialDelay == 0) && !otherMirages.Any(a => a.IsMirage))
                    Game.Sound.Play(SoundType.World, Info.RevealSound, self.CenterPosition);
            }

            wasMirage = isMirage;
            firstTick = false;
        }

        protected override void TraitEnabled(Actor self)
        {
            remainingTime = Info.InitialDelay;
        }

        protected override void TraitDisabled(Actor self) { Reveal(); }

        void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell) { }

        void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor refineryActor) { }

        void INotifyHarvesterAction.MovementCancelled(Actor self) { }

        void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource) { }

        void INotifyHarvesterAction.Docked()
        {
            if (Info.RevealOn.HasFlag(MirageRevealType.Dock))
            {
                isDocking = true;
                Reveal();
            }
        }

        void INotifyHarvesterAction.Undocked()
        {
            isDocking = false;
        }

        void INotifyUnload.Unloading(Actor self)
        {
            if (Info.RevealOn.HasFlag(MirageRevealType.Unload))
                Reveal();
        }

        void INotifyDemolition.Demolishing(Actor self)
        {
            if (Info.RevealOn.HasFlag(MirageRevealType.Demolish))
                Reveal();
        }

        void INotifyInfiltration.Infiltrating(Actor self)
        {
            if (Info.RevealOn.HasFlag(MirageRevealType.Infiltrate))
                Reveal();
        }
    }
}
