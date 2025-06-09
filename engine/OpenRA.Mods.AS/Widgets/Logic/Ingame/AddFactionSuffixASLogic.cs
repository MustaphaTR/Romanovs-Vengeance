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

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.AS.Widgets.Logic
{
	public class AddFactionSuffixASLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AddFactionSuffixASLogic(Widget widget, World world)
		{
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			if (!ChromeMetrics.TryGet("FactionSuffix-" + world.LocalPlayer.Faction.InternalName, out string faction))
				faction = world.LocalPlayer.Faction.InternalName;
			var suffix = "-" + faction;

			if (widget is PowerMeterWidget pmw)
				pmw.ImageCollection += suffix;
			else if (widget is HealthBarWidget hbw)
			{
				hbw.Background += suffix;
				hbw.EmptyHealthBar += suffix;
				hbw.RedHealthBar += suffix;
				hbw.YellowHealthBar += suffix;
				hbw.GreenHealthBar += suffix;
			}
			else
				throw new InvalidOperationException("AddFactionASSuffixLogic only supports PowerMeterWidget and HealthBarWidget");
		}
	}
}
