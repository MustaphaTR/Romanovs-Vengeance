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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Handle infection by infector units.")]
	public class InfectableInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[Desc("Damage types that removes the infector.")]
		public readonly BitSet<DamageType> RemoveInfectorDamageTypes = default;

		[Desc("Damage types that kills the infector.")]
		public readonly BitSet<DamageType> KillInfectorDamageTypes = default;

		[Desc("Teleport types that removes the infector.")]
		public readonly HashSet<string> RemoveInfectorTeleportTypes = default;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while infected by any actor.")]
		public readonly string InfectedCondition = null;

		[GrantedConditionReference]
		[Desc("Condition granted when being infected by another actor.")]
		public readonly string BeingInfectedCondition = null;

		[Desc("Conditions to grant when infected by specified actors.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> InfectedByConditions = new();

		[GrantedConditionReference]
		public IEnumerable<string> LinterConditions { get { return InfectedByConditions.Values; } }

		public override object Create(ActorInitializer init) { return new Infectable(init.Self, this); }
	}

	public class Infectable : ConditionalTrait<InfectableInfo>, ISync, ITick, INotifyCreated, INotifyDamage, INotifyKilled, IOnSuccessfulTeleportRA2
	{
		readonly Health health;
		readonly Actor self;

		public Tuple<Actor, AttackInfect, AttackInfectInfo> Infector;
		public int[] FirepowerMultipliers = Array.Empty<int>();

		[Sync]
		public int Ticks;

		int beingInfectedToken = Actor.InvalidConditionToken;
		Actor enteringInfector;
		int infectedToken = Actor.InvalidConditionToken;
		int infectedByToken = Actor.InvalidConditionToken;

		int dealtDamage = 0;
		int suppressionCount = 0;

		public Infectable(Actor self, InfectableInfo info)
			: base(info)
		{
			this.self = self;
			health = self.Trait<Health>();
		}

		public bool TryStartInfecting(Actor self, Actor infector)
		{
			if (infector != null && enteringInfector == null)
			{
				enteringInfector = infector;

				if (beingInfectedToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.BeingInfectedCondition))
					beingInfectedToken = self.GrantCondition(Info.BeingInfectedCondition);

				return true;
			}

			return false;
		}

		public void GrantCondition(Actor self)
		{
			if (infectedToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.InfectedCondition))
				infectedToken = self.GrantCondition(Info.InfectedCondition);

			if (Info.InfectedByConditions.TryGetValue(Infector.Item1.Info.Name, out var infectedByCondition))
				infectedByToken = self.GrantCondition(infectedByCondition);
		}

		public void RevokeCondition(Actor self, Actor infector = null)
		{
			if (infector != null)
			{
				if (enteringInfector == infector)
				{
					enteringInfector = null;

					if (beingInfectedToken != Actor.InvalidConditionToken)
						beingInfectedToken = self.RevokeCondition(beingInfectedToken);
				}
			}
			else
			{
				if (infectedToken != Actor.InvalidConditionToken)
					infectedToken = self.RevokeCondition(infectedToken);

				if (infectedByToken != Actor.InvalidConditionToken)
					infectedByToken = self.RevokeCondition(infectedByToken);
			}
		}

		void RemoveInfector(Actor self, WPos spawnLoc, bool kill, AttackInfo e)
		{
			if (Infector != null && !Infector.Item1.IsDead)
			{
				Infector.Item1.TraitOrDefault<IPositionable>().SetPosition(Infector.Item1, spawnLoc);
				self.World.AddFrameEndTask(w =>
				{
					if (Infector == null || Infector.Item1.IsDead)
						return;

					w.Add(Infector.Item1);

					if (kill)
					{
						if (e != null)
							Infector.Item1.Kill(e.Attacker, e.Damage.DamageTypes);
						else
							Infector.Item1.Kill(self);
					}
					else
						Infector.Item1.QueueActivity(false, new Nudge(Infector.Item1));

					RevokeCondition(self);
					Infector = null;
					FirepowerMultipliers = Array.Empty<int>();
					dealtDamage = 0;
					suppressionCount = 0;
				});
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (Infector != null)
			{
				if (e.Damage.DamageTypes.Overlaps(Info.KillInfectorDamageTypes))
					RemoveInfector(self, self.CenterPosition, true, e);
				else if (e.Damage.DamageTypes.Overlaps(Info.RemoveInfectorDamageTypes))
					RemoveInfector(self, self.CenterPosition, false, e);
				else if (e.Attacker != Infector.Item1 && e.Damage.DamageTypes.Overlaps(Infector.Item3.SuppressionDamageType))
				{
					var kill = Infector.Item3.SuppressionDamageThreshold > 0 && e.Damage.Value > Infector.Item3.SuppressionDamageThreshold;

					dealtDamage += e.Damage.Value;
					kill |= Infector.Item3.SuppressionSumThreshold > 0 && dealtDamage > Infector.Item3.SuppressionSumThreshold;

					suppressionCount++;
					kill |= Infector.Item3.SuppressionCountThreshold > 0 && suppressionCount > Infector.Item3.SuppressionCountThreshold;

					if (kill)
						RemoveInfector(self, self.CenterPosition, true, e);
				}
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Infector != null)
			{
				var kill = !Infector.Item3.SurviveHostDamageTypes.Overlaps(e.Damage.DamageTypes);
				RemoveInfector(self, self.CenterPosition, kill, e);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && Infector != null && --Ticks < 0)
			{
				var damage = Util.ApplyPercentageModifiers(Infector.Item3.Damage, FirepowerMultipliers);
				health.InflictDamage(self, Infector.Item1, new Damage(damage, Infector.Item3.DamageTypes), false);

				Ticks = Infector.Item3.DamageInterval;
			}
		}

		void IOnSuccessfulTeleportRA2.OnSuccessfulTeleport(string type, WPos oldPos, WPos newPos)
		{
			if (Info.RemoveInfectorTeleportTypes.Contains(type))
				RemoveInfector(self, oldPos, false, null);
		}
	}
}
