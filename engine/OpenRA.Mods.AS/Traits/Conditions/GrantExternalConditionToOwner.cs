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
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants an external condition to the owner player's actor.")]
	class GrantExternalConditionToOwnerInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantExternalConditionToOwner(this); }
	}

	class GrantExternalConditionToOwner : ConditionalTrait<GrantExternalConditionToOwnerInfo>, INotifyRemovedFromWorld,
		INotifyAddedToWorld, INotifyOwnerChanged, INotifyKilled
	{
		int conditionToken = Actor.InvalidConditionToken;
		ExternalCondition playerConditionTrait;

		public GrantExternalConditionToOwner(GrantExternalConditionToOwnerInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);

			UpdatePlayerConditionReference(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			UpdatePlayerConditionReference(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!self.IsDead && self.IsInWorld && conditionToken == Actor.InvalidConditionToken)
				conditionToken = playerConditionTrait.GrantCondition(self.Owner.PlayerActor, self);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!self.IsDead && self.IsInWorld && conditionToken != Actor.InvalidConditionToken
				&& playerConditionTrait.TryRevokeCondition(self.Owner.PlayerActor, self, conditionToken))
				conditionToken = Actor.InvalidConditionToken;
		}

		void UpdatePlayerConditionReference(Actor self)
		{
			playerConditionTrait = self.Owner.PlayerActor.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			if (!self.IsDead && !IsTraitDisabled && conditionToken == Actor.InvalidConditionToken)
				conditionToken = playerConditionTrait.GrantCondition(self.Owner.PlayerActor, self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!self.IsDead && !IsTraitDisabled && conditionToken != Actor.InvalidConditionToken
				&& playerConditionTrait.TryRevokeCondition(self.Owner.PlayerActor, self, conditionToken))
				conditionToken = Actor.InvalidConditionToken;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (conditionToken != Actor.InvalidConditionToken
				&& playerConditionTrait.TryRevokeCondition(self.Owner.PlayerActor, self, conditionToken))
				conditionToken = Actor.InvalidConditionToken;
		}
	}
}
