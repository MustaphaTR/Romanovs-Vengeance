#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class FlashTargetWarhead : WarheadAS
	{
		[Desc("Color of the flash.")]
		public readonly Color FlashColor = Color.White;

		[Desc("Use color of the firer for the flash, instead of `Color` value.")]
		public readonly bool UsePlayerColor = true;

		[Desc("Range of targets to be affected.")]
		public readonly WDist Range = new(64);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			var pos = target.CenterPosition;
			if (!IsValidImpact(pos, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsInCircle(pos, Range);
			foreach (var a in availableActors)
			{
				if (!IsValidAgainst(a, firedBy))
					continue;

				firedBy.World.AddFrameEndTask(w => w.Add(new FlashTarget(a, UsePlayerColor ? firedBy.Owner.Color : FlashColor)));
			}
		}
	}
}
