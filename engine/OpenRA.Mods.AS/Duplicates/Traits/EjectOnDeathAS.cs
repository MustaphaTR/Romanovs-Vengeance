#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Eject a ground soldier or a paratrooper while in the air. Carries over the veterancy level.")]
	public class EjectOnDeathASInfo : EjectOnDeathInfo
	{
		[Desc("Only spawn the pilot when there is a veterancy to carry over?")]
		public readonly bool SpawnOnlyWhenPromoted = true;

		public new object Create(ActorInitializer init) { return new EjectOnDeathAS(this); }
	}

	class EjectOnDeathAS : ConditionalTrait<EjectOnDeathInfo>, INotifyKilled
	{
		readonly EjectOnDeathASInfo info;

		public EjectOnDeathAS(EjectOnDeathASInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (self.Owner.WinState == WinState.Lost || !self.World.Map.Contains(self.Location))
				return;

			var r = self.World.SharedRandom.Next(1, 100);

			if (r <= 100 - Info.SuccessRate)
				return;

			var cp = self.CenterPosition;
			var inAir = !self.IsAtGroundLevel();
			if ((inAir && !Info.EjectInAir) || (!inAir && !Info.EjectOnGround))
				return;

			var ge = self.TraitOrDefault<GainsExperience>();
			if ((ge == null || ge.Level == 0) && info.SpawnOnlyWhenPromoted)
				return;

			var pilot = self.World.CreateActor(false, Info.PilotActor.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner), new LocationInit(self.Location) });

			var pilotPositionable = pilot.TraitOrDefault<IPositionable>();
			var pilotCell = self.Location;
			var pilotSubCell = pilotPositionable.GetAvailableSubCell(pilotCell);
			if (pilotSubCell == SubCell.Invalid)
			{
				if (!Info.AllowUnsuitableCell)
				{
					pilot.Dispose();
					return;
				}

				pilotSubCell = SubCell.Any;
			}

			if (inAir)
			{
				self.World.AddFrameEndTask(w =>
				{
					pilotPositionable.SetPosition(pilot, pilotCell, pilotSubCell);
					w.Add(pilot);

					var dropPosition = pilot.CenterPosition + new WVec(0, 0, self.CenterPosition.Z - pilot.CenterPosition.Z);
					pilotPositionable.SetCenterPosition(pilot, dropPosition);
					pilot.QueueActivity(new Parachute(pilot));
				});

				Game.Sound.Play(SoundType.World, Info.ChuteSound, cp);
			}
			else
			{
				self.World.AddFrameEndTask(w =>
				{
					w.Add(pilot);
					pilotPositionable.SetPosition(pilot, pilotCell, pilotSubCell);
					pilot.QueueActivity(false, new Nudge(pilot));
				});
			}
		}
	}
}
