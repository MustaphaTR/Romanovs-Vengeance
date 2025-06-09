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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Player receives listed units for free as passenger once the trait is enabled.")]
	public class FreePassengerInfo : ConditionalTraitInfo, Requires<CargoInfo>
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Name of the actor.")]
		public readonly string[] Actors = Array.Empty<string>();

		[Desc("Whether another actor should spawn upon re-enabling the trait.")]
		public readonly bool AllowRespawn = false;

		public override object Create(ActorInitializer init) { return new FreePassenger(init, this); }
	}

	public class FreePassenger : ConditionalTrait<FreePassengerInfo>
	{
		protected bool allowSpawn = true;
		protected string faction;
		readonly Cargo cargo;

		public FreePassenger(ActorInitializer init, FreePassengerInfo info)
			: base(info)
		{
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
			cargo = init.Self.Trait<Cargo>();
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!allowSpawn)
				return;

			allowSpawn = Info.AllowRespawn;

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				foreach (var actor in Info.Actors)
				{
					var passenger = self.World.Map.Rules.Actors[actor].TraitInfoOrDefault<PassengerInfo>();

					if (passenger == null || !cargo.Info.Types.Contains(passenger.CargoType) || !cargo.HasSpace(passenger.Weight))
						return;

					var a = w.CreateActor(actor, new TypeDictionary
					{
						new ParentActorInit(self),
						new LocationInit(self.Location),
						new OwnerInit(self.Owner),
						new FactionInit(faction),
					});

					w.Remove(a);
					cargo.Load(self, a);
				}
			});
		}
	}
}
