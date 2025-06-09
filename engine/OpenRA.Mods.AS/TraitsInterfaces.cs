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
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[RequireExplicitImplementation]
	public interface ISmokeParticleInfo
	{
		string Image { get; }
		string[] StartSequences { get; }
		string[] Sequences { get; }
		string[] EndSequences { get; }
		string Palette { get; }

		bool IsPlayerPalette { get; }

		int[] Duration { get; }

		WDist[] Speed { get; }

		WDist[] Gravity { get; }

		WeaponInfo Weapon { get; }

		int TurnRate { get; }

		int RandomRate { get; }
	}

	[RequireExplicitImplementation]
	public interface INotifyEnteredGarrison { void OnEnteredGarrison(Actor self, Actor garrison); }

	[RequireExplicitImplementation]
	public interface INotifyExitedGarrison { void OnExitedGarrison(Actor self, Actor garrison); }

	[RequireExplicitImplementation]
	public interface INotifyGarrisonerEntered { void OnGarrisonerEntered(Actor self, Actor garrisoner); }

	[RequireExplicitImplementation]
	public interface INotifyGarrisonerExited { void OnGarrisonerExited(Actor self, Actor garrisoner); }

	[RequireExplicitImplementation]
	public interface INotifyEnteredSharedCargo { void OnEnteredSharedCargo(Actor self, Actor cargo); }

	[RequireExplicitImplementation]
	public interface INotifyExitedSharedCargo { void OnExitedSharedCargo(Actor self, Actor cargo); }

	[RequireExplicitImplementation]
	public interface INotifyPrismCharging { void Charging(Actor self, in Target target); }

	[RequireExplicitImplementation]
	public interface IOnSuccessfulTeleportRA2 { void OnSuccessfulTeleport(string type, WPos oldPos, WPos newPos); }
}
