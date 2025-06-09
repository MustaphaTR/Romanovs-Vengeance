#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grant an external condition to the killer.")]
	public class GrantExternalConditionToKillerInfo : TraitInfo
	{
		[Desc("The condition to apply. Must be included among the target actor's ExternalCondition traits.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		[Desc("Stance the attacking player needs to receive the condition.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("DeathType(s) that grant the condition. Leave empty to always grant the condition.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		public override object Create(ActorInitializer init) { return new GrantExternalConditionToKiller(this); }
	}

	public class GrantExternalConditionToKiller : INotifyKilled
	{
		public readonly GrantExternalConditionToKillerInfo Info;

		public GrantExternalConditionToKiller(GrantExternalConditionToKillerInfo info)
		{
			Info = info;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			if (!Info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			var external = e.Attacker.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(self));

			external?.GrantCondition(e.Attacker, self, Info.Duration);
		}
	}
}
