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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can mind control other actors.")]
	public class MindControllerInfo : PausableConditionalTraitInfo, Requires<ArmamentInfo>, Requires<HealthInfo>
	{
		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new() { "primary" };

		[Desc("Up to how many units can this unit control?",
			"Use 0 or negative numbers for infinite.")]
		public readonly int Capacity = 1;

		[Desc("If the capacity is reached, discard the oldest mind controlled unit and control the new one",
			"If false, controlling new units is forbidden after capacity is reached.")]
		public readonly bool DiscardOldest = true;

		[Desc("Condition to grant to self when controlling actors." +
			"Can stack up by the number of enslaved actors." +
			"You can use this to forbid firing of the dummy MC weapon.")]
		[GrantedConditionReference]
		public readonly string ControllingCondition;

		[Desc("The sound played when the unit is mindcontrolled.")]
		public readonly string[] Sounds = Array.Empty<string>();

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the Sounds played at.")]
		public readonly float Volume = 1f;

		public override object Create(ActorInitializer init) { return new MindController(this); }
	}

	public class MindController : PausableConditionalTrait<MindControllerInfo>, INotifyAttack, INotifyKilled,
		INotifyActorDisposing, INotifyCreated, INotifyOwnerChanged
	{
		readonly MindControllerInfo info;
		readonly List<Actor> slaves = new();
		readonly Stack<int> controllingTokens = new();

		public IEnumerable<Actor> Slaves { get { return slaves; } }

		public MindController(MindControllerInfo info)
			: base(info)
		{
			this.info = info;
		}

		void StackControllingCondition(Actor self, string condition)
		{
			if (string.IsNullOrEmpty(condition))
				return;

			controllingTokens.Push(self.GrantCondition(condition));
		}

		void UnstackControllingCondition(Actor self, string condition)
		{
			if (string.IsNullOrEmpty(condition))
				return;

			self.RevokeCondition(controllingTokens.Pop());
		}

		public void UnlinkSlave(Actor self, Actor slave)
		{
			if (slaves.Contains(slave))
			{
				slaves.Remove(slave);
				UnstackControllingCondition(self, info.ControllingCondition);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!info.ArmamentNames.Contains(a.Info.Name))
				return;

			if (target.Actor == null || !target.IsValidFor(self))
				return;

			if (self.Owner.RelationshipWith(target.Actor.Owner) == PlayerRelationship.Ally)
				return;

			var mindControllable = target.Actor.TraitOrDefault<MindControllable>();

			if (mindControllable == null)
			{
				throw new InvalidOperationException(
					$"`{self.Info.Name}` tried to mindcontrol `{target.Actor.Info.Name}`, but the latter does not have the necessary trait!");
			}

			if (mindControllable.IsTraitDisabled || mindControllable.IsTraitPaused)
				return;

			if (info.Capacity > 0 && !info.DiscardOldest && slaves.Count >= info.Capacity)
				return;

			slaves.Add(target.Actor);
			StackControllingCondition(self, info.ControllingCondition);
			mindControllable.LinkMaster(target.Actor, self);

			if (info.Sounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, info.Sounds.Random(self.World.SharedRandom), pos, info.Volume);
			}

			if (info.Capacity > 0 && info.DiscardOldest && slaves.Count > info.Capacity)
				slaves[0].Trait<MindControllable>().RevokeMindControl(slaves[0]);
		}

		void ReleaseSlaves(Actor self)
		{
			foreach (var s in slaves)
			{
				if (s.IsDead || s.Disposed)
					continue;

				s.Trait<MindControllable>().RevokeMindControl(s);
			}

			slaves.Clear();
			while (controllingTokens.Count > 0)
				UnstackControllingCondition(self, info.ControllingCondition);
		}

		public void TransformSlave(Actor oldSlave, Actor newSlave)
		{
			if (slaves.Contains(oldSlave))
				slaves[slaves.FindIndex(o => o == oldSlave)] = newSlave;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ReleaseSlaves(self);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			ReleaseSlaves(self);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ReleaseSlaves(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			ReleaseSlaves(self);
		}
	}
}
