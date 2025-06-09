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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("AS warhead extension class." +
		"These warheads check for the Air TargetType when detonated inair!")]
	public abstract class WarheadAS : Warhead
	{
		[Desc("Whether to consider actors in determining whether the explosion should happen. If false, only terrain will be considered.")]
		public readonly bool ImpactActors = true;

		static readonly BitSet<TargetableType> TargetTypeAir = new("Air");

		protected enum ImpactActorType
		{
			None,
			Invalid,
			Valid,
		}

		/// <summary>Checks if there are any actors at impact position and if the warhead is valid against any of them.</summary>
		protected ImpactActorType ActorTypeAtImpact(World world, WPos pos, Actor firedBy)
		{
			var anyInvalidActor = false;

			// Check whether the impact position overlaps with an actor's hitshape
			var potentialVictims = world.FindActorsOnCircle(pos, WDist.Zero);
			foreach (var victim in potentialVictims)
			{
				if (!AffectsParent && victim == firedBy)
					continue;

				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (!activeShapes.Any(s => s.DistanceFromEdge(victim, pos).Length <= 0))
					continue;

				if (IsValidAgainst(victim, firedBy))
					return ImpactActorType.Valid;

				anyInvalidActor = true;
			}

			return anyInvalidActor ? ImpactActorType.Invalid : ImpactActorType.None;
		}

		/// <summary>Checks if the warhead is valid against the terrain at impact position.</summary>
		protected bool IsValidAgainstTerrain(World world, WPos pos)
		{
			var cell = world.Map.CellContaining(pos);
			if (!world.Map.Contains(cell))
				return false;

			var dat = world.Map.DistanceAboveTerrain(pos);
			return IsValidTarget(dat > AirThreshold ? TargetTypeAir : world.Map.GetTerrainInfo(cell).TargetTypes);
		}

		protected bool IsValidImpact(WPos pos, Actor firedBy)
		{
			var actorAtImpact = ImpactActors ? ActorTypeAtImpact(firedBy.World, pos, firedBy) : ImpactActorType.None;

			// If there's either a) an invalid actor, or b) no actor and invalid terrain, we don't trigger the effect(s).
			if (actorAtImpact == ImpactActorType.Invalid)
				return false;
			else if (actorAtImpact == ImpactActorType.None && !IsValidAgainstTerrain(firedBy.World, pos))
				return false;

			return true;
		}
	}
}
