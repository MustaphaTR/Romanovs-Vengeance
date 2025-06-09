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
using Eluant;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Scripting
{
	[ScriptPropertyGroup("Transports")]
	public class SharedTransportProperties : ScriptActorProperties, Requires<SharedCargoInfo>
	{
		readonly SharedCargo sharedCargo;

		public SharedTransportProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			sharedCargo = self.Trait<SharedCargo>();
		}

		[Desc("Returns references to passengers inside the shared transport.")]
		public Actor[] SharedPassengers => sharedCargo.Manager.Passengers.ToArray();

		[Desc("Specifies whether shared transport has any passengers.")]
		public bool HasSharedPassengers => sharedCargo.Manager.Passengers.Any();

		[Desc("Specifies the amount of passengers.")]
		public int SharedPassengerCount => sharedCargo.Manager.Passengers.Count();

		[Desc("Teleport an existing actor inside this shared transport.")]
		public void LoadSharedPassenger(Actor a)
		{
			if (!a.IsIdle)
				throw new LuaException("LoadSharedPassenger requires the shared passenger to be idle.");

			sharedCargo.Load(Self, a);
		}

		[Desc("Remove an existing actor (or first actor if none specified) from the shared transport.  This actor is not added to the world.")]
		public Actor UnloadSharedPassenger(Actor a = null) { return sharedCargo.Unload(Self, a); }

		[ScriptActorPropertyActivity]
		[Desc("Command shared transport to unload passengers.")]
		public void UnloadSharedPassengers(CPos? cell = null, int unloadRange = 5)
		{
			if (cell.HasValue)
			{
				var destination = Target.FromCell(Self.World, cell.Value);
				Self.QueueActivity(new UnloadSharedCargo(Self, destination, WDist.FromCells(unloadRange)));
			}
			else
				Self.QueueActivity(new UnloadSharedCargo(Self, WDist.FromCells(unloadRange)));
		}
	}

	[ScriptPropertyGroup("Transports")]
	public class GarrisonableProperties : ScriptActorProperties, Requires<GarrisonableInfo>
	{
		readonly Garrisonable garrisonable;

		public GarrisonableProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			garrisonable = self.Trait<Garrisonable>();
		}

		[Desc("Returns references to garrisoners inside the transport.")]
		public Actor[] Garrisoners => garrisonable.Garrisoners.ToArray();

		[Desc("Specifies whether transport has any garrisoners.")]
		public bool HasGarrisoners => garrisonable.Garrisoners.Any();

		[Desc("Specifies the amount of garrisoners.")]
		public int GarrisonerCount => garrisonable.Garrisoners.Count();

		[Desc("Teleport an existing actor inside this transport.")]
		public void LoadGarrisoner(Actor a)
		{
			if (!a.IsIdle)
				throw new LuaException("LoadGarrisoner requires the garrisoner to be idle.");

			garrisonable.Load(Self, a);
		}

		[Desc("Remove an existing actor (or first actor if none specified) from the transport.  This actor is not added to the world.")]
		public Actor UnloadGarrisoner(Actor a = null) { return garrisonable.Unload(Self, a); }

		[ScriptActorPropertyActivity]
		[Desc("Command transport to unload garrisoners.")]
		public void UnloadGarrisoners(CPos? cell = null, int unloadRange = 5)
		{
			if (cell.HasValue)
			{
				var destination = Target.FromCell(Self.World, cell.Value);
				Self.QueueActivity(new UnloadGarrison(Self, destination, WDist.FromCells(unloadRange)));
			}
			else
				Self.QueueActivity(new UnloadGarrison(Self, WDist.FromCells(unloadRange)));
		}
	}
}
