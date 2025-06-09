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
	[Desc("This actor can destroy weaponry.")]
	public class PointDefenseInfo : ConditionalTraitInfo, Requires<ArmamentInfo>
	{
		[FieldLoader.Require]
		[Desc("Weapon used to shoot the projectile. Caution: make sure that this is an insta-hit weapon, otherwise will look very odd!")]
		public readonly string Armament;

		[FieldLoader.Require]
		[Desc("What kind of projectiles can this actor shoot at.")]
		public readonly BitSet<string> PointDefenseTypes = default;

		[Desc("What diplomatic stances are affected.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new PointDefense(init.Self, this); }
	}

	public class PointDefense : ConditionalTrait<PointDefenseInfo>, IPointDefense
	{
		readonly Actor self;
		readonly PointDefenseInfo info;
		readonly Armament armament;

		public PointDefense(Actor self, PointDefenseInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;
			armament = self.TraitsImplementing<Armament>().First(a => a.Info.Name == info.Armament);
		}

		bool IPointDefense.Destroy(WPos position, Player attacker, BitSet<string> types)
		{
			if (IsTraitDisabled || armament.IsTraitDisabled || armament.IsTraitPaused)
				return false;

			if (!info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(attacker)))
				return false;

			if (armament.IsReloading)
				return false;

			if (!info.PointDefenseTypes.Overlaps(types))
				return false;

			if ((self.CenterPosition - position).HorizontalLengthSquared > armament.MaxRange().LengthSquared)
				return false;

			self.World.AddFrameEndTask(w =>
			{
				if (!self.IsDead)
					armament.CheckFire(self, null, Target.FromPos(position));
			});
			return true;
		}
	}
}
