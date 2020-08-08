#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RV.Traits.BotModules
{
	[Desc("Manages AI powerdown.")]
	public class PowerDownBotModuleRVInfo : ConditionalTraitInfo
	{
		[Desc("Delay (in ticks) between toggling powerdown")]
		public readonly int Interval = 150;

		public override object Create(ActorInitializer init) { return new PowerDownBotModuleRV(init.Self, this); }
	}

	public class PowerDownBotModuleRV : ConditionalTrait<PowerDownBotModuleRVInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		PowerManager playerPower;
		int toggleTick;
		readonly Func<Actor, bool> isToggledBuildingsValid;
		List<BuildingPowerWrapper> toggledBuildings;

		class BuildingPowerWrapper
		{
			public int PowerChanging;
			public Actor Actor;

			public BuildingPowerWrapper(Actor a, int p)
			{
				Actor = a;
				PowerChanging = p;
			}
		}

		public PowerDownBotModuleRV(Actor self, PowerDownBotModuleRVInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			toggledBuildings = new List<BuildingPowerWrapper>();
			isToggledBuildingsValid = a => a.Owner == self.Owner && !a.IsDead && a.IsInWorld && GetTogglePowerChanging(a) < 0;
		}

		protected override void Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query player traits from self, which refers
			// for bot modules always to the Player actor.
			playerPower = self.TraitOrDefault<PowerManager>();
		}

		protected override void TraitEnabled(Actor self)
		{
			toggleTick = world.LocalRandom.Next(0, Info.Interval);
			toggledBuildings = new List<BuildingPowerWrapper>();
		}

		int GetTogglePowerChanging(Actor a)
		{
			var powerChangingIfToggled = 0;
			var powerTarit = a.TraitsImplementing<Power>().Where(t => !t.IsTraitDisabled).ToArray();
			var powerMulTrait = a.TraitsImplementing<PowerMultiplier>().ToArray();
			if (powerTarit.Any())
			{
				powerChangingIfToggled = powerTarit.Sum(p => p.Info.Amount) * (powerMulTrait.Sum(p => p.Info.Modifier) - 100) / 100;
				if (powerMulTrait.Where(t => !t.IsTraitDisabled).Any())
					powerChangingIfToggled = -powerChangingIfToggled;
			}

			return powerChangingIfToggled;
		}

		IEnumerable<Actor> GetToggleableBuildings(IBot bot)
		{
			var toggleable = bot.Player.World.ActorsHavingTrait<ToggleConditionOnOrder>(t => !t.IsTraitDisabled && !t.IsTraitPaused)
				.Where(a => a != null && !a.IsDead && a.Owner == player && a.Info.HasTraitInfo<PowerInfo>() && a.Info.HasTraitInfo<PowerMultiplierInfo>() && a.Info.HasTraitInfo<BuildingInfo>());

			return toggleable;
		}

		IEnumerable<BuildingPowerWrapper> GetOnlineBuildings(IBot bot)
		{
			List<BuildingPowerWrapper> toggleableBuilding = new List<BuildingPowerWrapper>();

			foreach (var a in GetToggleableBuildings(bot))
			{
				var powerChanging = GetTogglePowerChanging(a);
				if (powerChanging > 0)
					toggleableBuilding.Add(new BuildingPowerWrapper(a, powerChanging));
			}

			return toggleableBuilding.OrderBy(bpw => bpw.PowerChanging);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (toggleTick > 0 || playerPower == null)
			{
				toggleTick--;
				return;
			}

			var power = playerPower.ExcessPower;
			toggledBuildings = toggledBuildings.Where(bpw => isToggledBuildingsValid(bpw.Actor)).OrderByDescending(bpw => bpw.PowerChanging).ToList();

			// When there is extra power, check if AI can toggle on
			if (power > 0)
			{
				foreach (var bpw in toggledBuildings)
				{
					if (power + bpw.PowerChanging < 0)
						continue;

					bot.QueueOrder(new Order("PowerDown", bpw.Actor, false));
					power += bpw.PowerChanging;
				}
			}

			// When there is no power, check if AI can toggle off
			else if (power < 0)
			{
				var buildingsCanBeOff = GetOnlineBuildings(bot);
				foreach (var bpw in buildingsCanBeOff)
				{
					if (power > 0)
						break;

					bot.QueueOrder(new Order("PowerDown", bpw.Actor, false));
					toggledBuildings.Add(new BuildingPowerWrapper(bpw.Actor, -bpw.PowerChanging));
					power += bpw.PowerChanging;
				}
			}

			toggleTick = Info.Interval;
		}
	}
}
