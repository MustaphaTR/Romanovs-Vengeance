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
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Allows the firer to capture targets. This warhead interacts with the Capturable trait.")]
	public class CaptureActorWarhead : WarheadAS
	{
		[Desc("Range of targets to be captured.")]
		public readonly WDist Range = new(64);

		[Desc("Types of actors that it can capture, as long as the type also exists in the Capturable Type: trait.")]
		public readonly BitSet<CaptureType> CaptureTypes = default;

		[Desc("Targets with health above this percentage will be sabotaged instead of captured.",
			"Set to 0 to disable sabotaging.")]
		public readonly int SabotageThreshold = 0;

		[Desc("Sabotage damage expressed as a percentage of maximum target health.")]
		public readonly int SabotageHPRemoval = 50;

		[Desc("Experience granted to the capturing actor.")]
		public readonly int Experience = 0;

		[Desc("PlayerRelationship that the structure's previous owner needs to have for the capturing actor to receive Experience.")]
		public readonly PlayerRelationship ExperiencePlayerRelationships = PlayerRelationship.Enemy;

		[Desc("Experience granted to the capturing player.")]
		public readonly int PlayerExperience = 0;

		[Desc("PlayerRelationship that the structure's previous owner needs to have for the capturing player to receive Experience.")]
		public readonly PlayerRelationship PlayerExperienceStances = PlayerRelationship.Enemy;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;

			if (!IsValidImpact(pos, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsOnCircle(pos, Range);

			foreach (var a in availableActors)
			{
				if (!IsValidAgainst(a, firedBy))
					continue;

				var activeShapes = a.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any())
					continue;

				var distance = activeShapes.Min(t => t.DistanceFromEdge(a, pos));

				if (distance > Range)
					continue;

				var capturable = a.TraitsImplementing<Capturable>()
					.FirstOrDefault(c => !c.IsTraitDisabled && c.Info.Types.Overlaps(CaptureTypes));

				if (a.IsDead || capturable == null)
					continue;

				firedBy.World.AddFrameEndTask(w =>
				{
					if (a.IsDead)
						return;

					if (SabotageThreshold > 0 && !a.Owner.NonCombatant)
					{
						var health = a.Trait<IHealth>();

						// Cast to long to avoid overflow when multiplying by the health
						if (100 * (long)health.HP > SabotageThreshold * (long)health.MaxHP)
						{
							var damage = (int)((long)health.MaxHP * SabotageHPRemoval / 100);
							a.InflictDamage(firedBy, new Damage(damage));

							return;
						}
					}

					var oldOwner = a.Owner;

					a.ChangeOwner(firedBy.Owner);

					foreach (var t in a.TraitsImplementing<INotifyCapture>())
						t.OnCapture(a, firedBy, oldOwner, a.Owner, CaptureTypes);

					if (!firedBy.IsDead && firedBy.Owner.RelationshipWith(oldOwner).HasRelationship(ExperiencePlayerRelationships))
					{
						var exp = firedBy.TraitOrDefault<GainsExperience>();
						exp?.GiveExperience(Experience);
					}

					if (firedBy.Owner.RelationshipWith(oldOwner).HasRelationship(PlayerExperienceStances))
					{
						var exp = firedBy.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
						exp?.GiveExperience(PlayerExperience);
					}
				});
			}
		}

		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			var capturable = victim.TraitsImplementing<Capturable>()
					.FirstOrDefault(c => !c.IsTraitDisabled && c.Info.Types.Overlaps(CaptureTypes));

			if (capturable == null || !ValidRelationships.HasRelationship(victim.Owner.RelationshipWith(firedBy.Owner)))
				return false;

			return base.IsValidAgainst(victim, firedBy);
		}
	}
}
