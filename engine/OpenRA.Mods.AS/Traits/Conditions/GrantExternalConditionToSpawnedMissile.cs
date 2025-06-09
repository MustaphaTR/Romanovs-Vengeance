#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class GrantExternalConditionToSpawnedMissileInfo : ConditionalTraitInfo, Requires<MissileSpawnerMasterInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant to the missiles.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantExternalConditionToSpawnedMissile(init, this); }
	}

	public class GrantExternalConditionToSpawnedMissile : ConditionalTrait<GrantExternalConditionToSpawnedMissileInfo>
	{
		readonly MissileSpawnerMaster spawner;
		readonly Dictionary<Actor, int> tokens = new();

		public GrantExternalConditionToSpawnedMissile(ActorInitializer init, GrantExternalConditionToSpawnedMissileInfo info)
			: base(info)
		{
			spawner = init.Self.Trait<MissileSpawnerMaster>();
		}

		public void GrantCondition(Actor self, Actor slave)
		{
			if (tokens.ContainsKey(slave))
				return;

			var external = slave.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(self));

			if (external != null)
				tokens[slave] = external.GrantCondition(slave, self);
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var se in spawner.SlaveEntries)
			{
				if (!se.IsValid)
					continue;

				GrantCondition(self, se.Actor);
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var se in spawner.SlaveEntries)
			{
				if (!se.IsValid)
					continue;

				var a = se.Actor;
				if (!tokens.TryGetValue(a, out var token))
					continue;

				foreach (var external in a.TraitsImplementing<ExternalCondition>())
					if (external.TryRevokeCondition(a, self, token))
						break;
			}
		}
	}
}
