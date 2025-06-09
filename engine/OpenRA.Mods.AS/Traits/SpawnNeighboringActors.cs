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
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor places other actors around itself, which keep connected as in they get removed when the parent is sold or destroyed.")]
	public class SpawnNeighboringActorsInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[ActorReference]
		[Desc("Types of actors to place. If multiple are defined, a random one will be selected for each actor spawned.")]
		public readonly HashSet<string> ActorTypes = new();

		[FieldLoader.Require]
		[Desc("Locations to spawn the actors relative to the origin (top-left for buildings) of this actor.")]
		public readonly CVec[] Locations = Array.Empty<CVec>();

		public override object Create(ActorInitializer init) { return new SpawnNeighboringActors(this, init.Self); }
	}

	public class SpawnNeighboringActors : ConditionalTrait<SpawnNeighboringActorsInfo>, INotifyKilled, INotifyOwnerChanged, INotifyActorDisposing, INotifySold
	{
		readonly List<Actor> actors = new();

		public SpawnNeighboringActors(SpawnNeighboringActorsInfo info, Actor self)
			: base(info) { }

		public void SpawnActors(Actor self)
		{
			if (IsTraitDisabled)
				return;

			foreach (var offset in Info.Locations)
			{
				self.World.AddFrameEndTask(w =>
				{
					var actorType = Info.ActorTypes.Random(self.World.SharedRandom).ToLowerInvariant();
					var cell = self.Location + offset;

					var actor = w.CreateActor(true, actorType, new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new LocationInit(cell)
					});

					actors.Add(actor);
				});
			}
		}

		public void RemoveActors()
		{
			foreach (var actor in actors)
				actor.Dispose();

			actors.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			foreach (var actor in actors)
				actor.ChangeOwnerSync(newOwner);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			RemoveActors();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			RemoveActors();
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			RemoveActors();
		}

		protected override void TraitEnabled(Actor self)
		{
			SpawnActors(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			RemoveActors();
		}
	}
}
