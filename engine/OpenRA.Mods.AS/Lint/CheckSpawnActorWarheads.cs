#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.AS.Warheads;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Lint
{
	class CheckSpawnActorWarheads : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			foreach (var weaponInfo in rules.Weapons)
			{
				var warheads = weaponInfo.Value.Warheads.OfType<SpawnActorWarhead>().ToList();

				foreach (var warhead in warheads)
				{
					foreach (var a in warhead.Actors)
					{
						if (!rules.Actors.ContainsKey(a.ToLowerInvariant()))
						{
							emitError($"Warhead type {weaponInfo.Key} tries to spawn invalid actor {a}!");
							break;
						}

						if (!rules.Actors[a.ToLowerInvariant()].HasTraitInfo<IOccupySpaceInfo>())
							emitError($"Warhead type {weaponInfo.Key} tries to spawn unpositionable actor {a}!");

						if (rules.Actors[a.ToLowerInvariant()].HasTraitInfo<BuildingInfo>())
							emitError($"Warhead type {weaponInfo.Key} tries to spawn building {a}!");

						if (!rules.Actors[a.ToLowerInvariant()].HasTraitInfo<ParachutableInfo>() && warhead.Paradrop)
							emitError($"Warhead type {weaponInfo.Key} tries to paradrop actor {a} which doesn't have the Parachutable trait!");
					}
				}
			}
		}
	}
}
