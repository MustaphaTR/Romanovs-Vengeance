#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Warheads
{
	[Desc("Steals cash from the target actor's owner.")]
	public class StealResourceWarhead : WarheadAS
	{
		[Desc("Amount of resources to steal from the affected player.")]
		public readonly int Cash = 10;

		[Desc("Whether to show the cash tick indicators rising from the actor.")]
		public readonly bool ShowTicks = true;

		public override void DoImpact(Target target, WarheadArgs args)
		{
			if (target.Actor == null)
				return;

			var firedBy = args.SourceActor;
			var targetResources = target.Actor.Owner.PlayerActor.Trait<PlayerResources>();
			var selfResources = firedBy.Owner.PlayerActor.Trait<PlayerResources>();

			var stolen = Math.Min(Cash, (targetResources.Cash + targetResources.Resources));

			targetResources.TakeCash(stolen);
			selfResources.GiveCash(stolen);

			if (ShowTicks)
				firedBy.World.AddFrameEndTask(w => w.Add(new FloatingText(target.Actor.CenterPosition, firedBy.Owner.Color, FloatingText.FormatCashTick(stolen), 30)));
		}
	}
}
