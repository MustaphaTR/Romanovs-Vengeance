#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor gives experience to a GainsExperience actor when they are killed.")]
	class GivesExperienceToMasterOrTransportInfo : TraitInfo
	{
		[Desc("If -1, use the value of the unit cost.")]
		public readonly int Experience = -1;

		[Desc("Player relationships the attacking player needs to receive the experience.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor's mindcontrol master.")]
		public readonly int ActorExperienceModifierOnMindControlMaster = 5000;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor's parent.")]
		public readonly int ActorExperienceModifierOnSummonMaster = 10000;

		[Desc("Percentage of the `Experience` value that is being granted to the killing actor's transport.")]
		public readonly int ActorExperienceModifierOnTransport = 5000;

		public override object Create(ActorInitializer init) { return new GivesExperienceToMasterOrTransport(this); }
	}

	class GivesExperienceToMasterOrTransport : INotifyKilled, INotifyCreated
	{
		readonly GivesExperienceToMasterOrTransportInfo info;

		int exp;
		IEnumerable<int> experienceModifiers;

		public GivesExperienceToMasterOrTransport(GivesExperienceToMasterOrTransportInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			exp = info.Experience >= 0 ? info.Experience
				: valued != null ? valued.Cost : 0;

			experienceModifiers = self.TraitsImplementing<IGivesExperienceModifier>().ToArray().Select(m => m.GetGivesExperienceModifier());
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (exp == 0 || e.Attacker == null || e.Attacker.Disposed)
				return;

			exp = Util.ApplyPercentageModifiers(exp, experienceModifiers);

			var killer = e.Attacker;
			if (killer != null)
			{
				if (info.ActorExperienceModifierOnMindControlMaster > 0)
				{
					foreach (var mindControllable in killer.TraitsImplementing<MindControllable>())
						if (mindControllable.Master != null)
							GiveExperience(self, mindControllable.Master, exp, info.ActorExperienceModifierOnMindControlMaster);
				}

				if (info.ActorExperienceModifierOnTransport > 0)
				{
					foreach (var pass in killer.TraitsImplementing<Passenger>())
						if (pass.Transport != null)
							GiveExperience(self, pass.Transport, exp, info.ActorExperienceModifierOnTransport);
				}

				if (info.ActorExperienceModifierOnSummonMaster > 0)
				{
					var parent = killer.TraitOrDefault<HasParent>()?.Parent;
					if (parent != null)
						GiveExperience(self, parent, exp, info.ActorExperienceModifierOnSummonMaster);
				}
			}
		}

		void GiveExperience(Actor self, Actor killer, int exp, int baseModifier)
		{
			if (killer.IsDead)
				return;

			if (!info.ValidRelationships.HasRelationship(killer.Owner.RelationshipWith(self.Owner)))
				return;

			var gainsExperience = killer.TraitOrDefault<GainsExperience>();
			if (gainsExperience == null)
				return;

			var experienceModifier = killer.TraitsImplementing<IGainsExperienceModifier>()
				.Select(x => x.GetGainsExperienceModifier()).Append(baseModifier);
			gainsExperience.GiveExperience(Util.ApplyPercentageModifiers(exp, experienceModifier));
		}
	}
}
