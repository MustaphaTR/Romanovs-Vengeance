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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public enum OwnerChangeType { Firer, InternalName }

	[Desc("Interacts with the `" + nameof(TemporaryOwnerManager) + "` trait.")]
	public class ChangeOwnerWarhead : Warhead
	{
		[Desc("Duration of the owner change (in ticks). Set to 0 to make it permanent.")]
		public readonly int Duration = 0;

		[Desc("Owner to change to. Allowed keywords:" +
			"'Firer' and 'InternalName'.")]
		public readonly OwnerChangeType OwnerType = OwnerChangeType.Firer;

		[Desc("Map player to use when 'InternalName' is defined on 'OwnerType'.")]
		public readonly string InternalOwner = "Neutral";

		public readonly WDist Range = WDist.FromCells(1);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			var actors = target.Type == TargetType.Actor ? [target.Actor] :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				if (!IsValidAgainst(a, firedBy))
					continue;

				var owner = firedBy.Owner;
				if (OwnerType == OwnerChangeType.InternalName)
					owner = firedBy.World.Players.First(p => p.InternalName == InternalOwner);

				// Don't do anything on if already target owner
				if (a.Owner == owner)
					continue;

				if (Duration == 0)
					a.ChangeOwner(owner); // Permanent
				else
				{
					var tempOwnerManager = a.TraitOrDefault<TemporaryOwnerManager>();
					if (tempOwnerManager == null)
						continue;

					tempOwnerManager.ChangeOwner(a, owner, Duration);
				}

				// Stop shooting, you have new enemies
				a.CancelActivity();
			}
		}
	}
}
