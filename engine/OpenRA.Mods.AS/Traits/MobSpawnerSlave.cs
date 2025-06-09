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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
 * Needs base engine modification. (Becaus MobSpawner.cs mods it)
 */

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Can be slaved to a Mob spawner.")]
	public class MobSpawnerSlaveInfo : BaseSpawnerSlaveInfo
	{
		public override object Create(ActorInitializer init) { return new MobSpawnerSlave(this); }
	}

	public class MobSpawnerSlave : BaseSpawnerSlave, INotifySelected
	{
		public IMove[] Moves { get; private set; }
		public IPositionable Positionable { get; private set; }

		MobSpawnerMaster spawnerMaster;

		public bool IsMoving()
		{
			return Array.Exists(Moves, m => m.IsTraitEnabled()
				&& (m.CurrentMovementTypes.HasFlag(MovementType.Horizontal) || m.CurrentMovementTypes.HasFlag(MovementType.Vertical)));
		}

		public MobSpawnerSlave(MobSpawnerSlaveInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);

			Moves = self.TraitsImplementing<IMove>().ToArray();

			var positionables = self.TraitsImplementing<IPositionable>().ToList();
			if (positionables.Count != 1)
				throw new InvalidOperationException($"Actor {self} has multiple (or no) traits implementing IPositionable.");

			Positionable = positionables[0];
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			this.spawnerMaster = spawnerMaster as MobSpawnerMaster;
		}

		public void Move(Actor self, CPos location)
		{
			// And tell attack bases to stop attacking.
			if (Moves.Length == 0)
				return;

			foreach (var mv in Moves)
				if (mv.IsTraitEnabled())
				{
					self.QueueActivity(mv.MoveTo(location, 2));
					break;
				}
		}

		public void AttackMove(Actor self, CPos location)
		{
			// And tell attack bases to stop attacking.
			if (Moves.Length == 0)
				return;

			foreach (var mv in Moves)
				if (mv.IsTraitEnabled())
				{
					// Must cancel before queueing as the master's attack move order is
					// issued multiple times on multiple points along the attack move path.
					self.CancelActivity();
					self.QueueActivity(new AttackMoveActivity(self, () => mv.MoveTo(location, 1)));
					break;
				}
		}

		void INotifySelected.Selected(Actor self)
		{
			if (spawnerMaster.Info.SlavesHaveFreeWill)
				return;

			// I'm assuming these guys are selectable, both slave and the nexus.
			// self.World.Selection.Remove(self.World, self); No need to remove when you don't wee the selection decoration.
			// -SelectionDecorations: is all you need.
			// Also use RejectsOrder if necessary.
			self.World.Selection.Add(Master);
		}
	}
}
