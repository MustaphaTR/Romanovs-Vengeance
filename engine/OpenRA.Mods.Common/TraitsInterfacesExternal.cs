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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface IBuildPaletteOrderModifierInfo : ITraitInfoInterface { int GetBuildPaletteOrderModifier(TechTree techTree, string queue); }

	[RequireExplicitImplementation]
	public interface IResourceLogicLayer
	{
		void UpdatePosition(CPos cell, string resourceType, int density);
	}

	[RequireExplicitImplementation]
	public interface IRefineryResourceDelivered
	{
		void ResourceGiven(Actor self, int amount);
	}

	[RequireExplicitImplementation]
	public interface IPointDefense
	{
		bool Destroy(WPos position, Player attacker, BitSet<string> types);
	}

	[RequireExplicitImplementation]
	public interface INotifyPassengersDamage
	{
		void DamagePassengers(
			int damage, Actor attacker, int amount, Dictionary<string, int> versus, BitSet<DamageType> damageTypes, IEnumerable<int> damageModifiers);
	}

	[RequireExplicitImplementation]
	public interface ISupplyDock
	{
		bool IsFull();
		bool IsEmpty();
		int Fullness();
	}

	[RequireExplicitImplementation]
	public interface ISupplyCollector
	{
		int Amount();
	}
}
