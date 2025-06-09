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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	class EnterTeleportNetwork : Enter
	{
		readonly string type;

		public EnterTeleportNetwork(Actor self, Target target, string type)
			: base(self, target, Color.Yellow)
		{
			this.type = type;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			return targetActor.IsValidTeleportNetworkUser(self);
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			// entered the teleport network canal but the entrance is dead immediately.
			if (targetActor.IsDead || self.IsDead)
				return;

			// Find the primary teleport network exit.
			var pri = targetActor.Owner.PlayerActor.TraitsImplementing<TeleportNetworkManager>().First(x => x.Type == type).PrimaryActor;

			var exitinfo = pri.Info.TraitInfo<ExitInfo>();
			var rp = pri.TraitOrDefault<RallyPoint>();

			var exit = CPos.Zero; // spawn point
			var exitLocations = new List<CPos>(); // dest to move (cell pos)
			var dest = Target.Invalid; // destination to move (in Target)

			if (pri.OccupiesSpace != null)
			{
				exit = pri.Location + exitinfo.ExitCell;
				var spawn = pri.CenterPosition + exitinfo.SpawnOffset;
				var to = self.World.Map.CenterOfCell(exit);

				WAngle initialFacing;
				if (!exitinfo.Facing.HasValue)
				{
					var delta = to - spawn;
					if (delta.HorizontalLengthSquared == 0)
						initialFacing = WAngle.Zero;
					else
						initialFacing = delta.Yaw;

					var fi = self.TraitOrDefault<IFacing>();
					if (fi != null)
						fi.Facing = initialFacing;
				}

				exitLocations = rp != null ? rp.Path : new List<CPos> { exit };
				dest = Target.FromCell(self.World, exitLocations.Last());
			}

			// Teleport myself to primary actor.
			self.Trait<IPositionable>().SetPosition(self, exit);

			// Cancel all activities (like PortableChrono does)
			self.CancelActivity();

			// Issue attack move to the rally point.
			self.World.AddFrameEndTask(w =>
			{
				var move = self.TraitOrDefault<IMove>();
				if (move != null)
				{
					// Exit delay is ignored.
					if (rp != null)
						foreach (var cell in rp.Path)
							self.QueueActivity(new AttackMoveActivity(
								self, () => move.MoveTo(cell, 1, targetLineColor: Color.OrangeRed)));
					else
						foreach (var cell in exitLocations)
							self.QueueActivity(new AttackMoveActivity(
								self, () => move.MoveTo(cell, 1, targetLineColor: Color.OrangeRed)));
				}
			});
		}
	}
}
