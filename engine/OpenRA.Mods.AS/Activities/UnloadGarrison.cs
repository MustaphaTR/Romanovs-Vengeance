#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class UnloadGarrison : Activity
	{
		readonly Actor self;
		readonly Garrisonable garrison;
		readonly INotifyUnloadCargo[] notifiers;
		readonly bool unloadAll;
		readonly Aircraft aircraft;
		readonly Mobile mobile;
		readonly bool assignTargetOnFirstRun;
		readonly WDist unloadRange;

		Target destination;
		bool takeOffAfterUnload;

		public UnloadGarrison(Actor self, WDist unloadRange, bool unloadAll = true)
			: this(self, Target.Invalid, unloadRange, unloadAll)
		{
			assignTargetOnFirstRun = true;
		}

		public UnloadGarrison(Actor self, Target destination, WDist unloadRange, bool unloadAll = true)
		{
			this.self = self;
			garrison = self.Trait<Garrisonable>();
			notifiers = self.TraitsImplementing<INotifyUnloadCargo>().ToArray();
			this.unloadAll = unloadAll;
			aircraft = self.TraitOrDefault<Aircraft>();
			mobile = self.TraitOrDefault<Mobile>();
			this.destination = destination;
			this.unloadRange = unloadRange;
		}

		public (CPos Cell, SubCell SubCell)? ChooseExitSubCell(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			return garrison.CurrentAdjacentCells()
				.Shuffle(self.World.SharedRandom)
				.Select(c => ((CPos Cell, SubCell SubCell)?)(c, pos.GetAvailableSubCell(c)))
				.FirstOrDefault(s => s.Value.SubCell != SubCell.Invalid);
		}

		IEnumerable<CPos> BlockedExitCells(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			// Find the cells that are blocked by transient actors
			return garrison.CurrentAdjacentCells()
				.Where(c => pos.CanEnterCell(c, null, BlockedByActor.All) != pos.CanEnterCell(c, null, BlockedByActor.None));
		}

		protected override void OnFirstRun(Actor self)
		{
			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);

			// Move to the target destination
			if (aircraft != null)
			{
				// Queue the activity even if already landed in case self.Location != destination
				QueueChild(new Land(self, destination, unloadRange));
				takeOffAfterUnload = !aircraft.AtLandAltitude;
			}
			else if (mobile != null)
			{
				var cell = self.World.Map.Clamp(this.self.World.Map.CellContaining(destination.CenterPosition));
				QueueChild(new Move(self, cell, unloadRange));
			}

			QueueChild(new Wait(garrison.Info.BeforeUnloadDelay));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || garrison.IsEmpty())
				return true;

			if (garrison.CanUnload())
			{
				foreach (var inu in notifiers)
					inu.Unloading(self);

				var actor = garrison.Peek();
				var spawn = self.CenterPosition;

				var exitSubCell = ChooseExitSubCell(actor);
				if (exitSubCell == null)
				{
					self.NotifyBlocker(BlockedExitCells(actor));

					Queue(new Wait(10));
					return false;
				}

				garrison.Unload(self);
				self.World.AddFrameEndTask(w =>
				{
					if (actor.Disposed)
						return;

					var move = actor.Trait<IMove>();
					var pos = actor.Trait<IPositionable>();

					pos.SetPosition(actor, exitSubCell.Value.Cell, exitSubCell.Value.SubCell);
					pos.SetCenterPosition(actor, spawn);

					actor.CancelActivity();
					w.Add(actor);
				});
			}

			if (!unloadAll || !garrison.CanUnload())
			{
				if (garrison.Info.AfterUnloadDelay > 0)
					QueueChild(new Wait(garrison.Info.AfterUnloadDelay, false));

				if (takeOffAfterUnload)
					QueueChild(new TakeOff(self));

				return true;
			}

			return false;
		}
	}
}
