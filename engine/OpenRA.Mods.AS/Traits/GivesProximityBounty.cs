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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class ProximityBountyType { }

	[Desc("When killed, this actor causes nearby actors with the ProximityBounty trait to receive money.")]
	class GivesProximityBountyInfo : ConditionalTraitInfo
	{
		[Desc("Percentage of the killed actor's Cost or CustomSellValue to be given.")]
		public readonly int Percentage = 10;

		[Desc("Stance the attacking player needs to grant bounty to actors.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("DeathTypes for which a bounty should be granted.",
			"Use an empty list (the default) to allow all DeathTypes.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		[Desc("Bounty types for the ProximityBounty traits which a bounty should be granted.",
			"Use an empty list (the default) to allow all of them.")]
		public readonly BitSet<ProximityBountyType> BountyTypes = default;

		public override object Create(ActorInitializer init) { return new GivesProximityBounty(this); }
	}

	class GivesProximityBounty : ConditionalTrait<GivesProximityBountyInfo>, INotifyKilled
	{
		public HashSet<ProximityBounty> Collectors;
		Cargo cargo;

		public GivesProximityBounty(GivesProximityBountyInfo info)
			: base(info)
		{
			Collectors = new HashSet<ProximityBounty>();
		}

		protected override void Created(Actor self)
		{
			cargo = self.TraitOrDefault<Cargo>();

			base.Created(self);
		}

		int GetBountyValue(Actor self)
		{
			return !IsTraitDisabled ? self.GetSellValue() * Info.Percentage / 100 : 0;
		}

		int GetDisplayedBountyValue(Actor self, BitSet<DamageType> deathTypes, BitSet<ProximityBountyType> bountyType)
		{
			var bounty = GetBountyValue(self);
			if (cargo == null)
				return bounty;

			foreach (var a in cargo.Passengers)
			{
				var givesProximityBounty = a.TraitsImplementing<GivesProximityBounty>().Where(gpb => deathTypes.Overlaps(gpb.Info.DeathTypes)
					&& gpb.Info.BountyTypes.Overlaps(bountyType));
				foreach (var gpb in givesProximityBounty)
					bounty += gpb.GetDisplayedBountyValue(a, deathTypes, bountyType);
			}

			return bounty;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Collectors.Count <= 0)
				return;

			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (!Info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
				return;

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			foreach (var c in Collectors)
			{
				if (!Info.BountyTypes.Overlaps(c.Info.BountyType))
					return;

				if (!c.Info.ValidRelationships.HasRelationship(e.Attacker.Owner.RelationshipWith(self.Owner)))
					return;

				c.AddBounty(GetDisplayedBountyValue(self, e.Damage.DamageTypes, c.Info.BountyType));
			}
		}
	}
}
