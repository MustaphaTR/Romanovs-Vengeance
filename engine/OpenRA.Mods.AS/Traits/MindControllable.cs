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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can be mind controlled by other actors.")]
	public class MindControllableInfo : PausableConditionalTraitInfo
	{
		[Desc("Condition to grant when under mindcontrol.")]
		[GrantedConditionReference]
		public readonly string Condition = null;

		[Desc("The sound played when the mindcontrol is revoked.")]
		public readonly string[] RevokeControlSounds = Array.Empty<string>();

		[Desc("Do the sounds play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Volume the RevokeControlSounds played at.")]
		public readonly float Volume = 1f;

		[Desc("Map player to transfer this actor to if the owner lost the game.")]
		public readonly string FallbackOwner = "Creeps";

		public override object Create(ActorInitializer init) { return new MindControllable(this); }
	}

	public class MindControllable : PausableConditionalTrait<MindControllableInfo>, INotifyKilled, INotifyActorDisposing, INotifyOwnerChanged, INotifyTransform
	{
		readonly MindControllableInfo info;
		Player creatorOwner;
		bool controlChanging;
		Actor oldSelf = null;

		int token = Actor.InvalidConditionToken;

		public Actor Master { get; private set; }

		// HACK: used for pass EXP to master or other thing when actor use Explodes attack.
		public Actor MasterWhenDie { get; private set; }

		public MindControllable(MindControllableInfo info)
			: base(info)
		{
			this.info = info;
		}

		public void LinkMaster(Actor self, Actor master)
		{
			self.CancelActivity();

			if (Master == null)
				creatorOwner = self.Owner;

			controlChanging = true;

			var oldOwner = self.Owner;
			self.ChangeOwner(master.Owner);

			UnlinkMaster(self, Master);
			Master = master;

			if (token == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.Condition))
				token = self.GrantCondition(Info.Condition);

			if (master.Owner == creatorOwner)
				UnlinkMaster(self, master);

			self.World.AddFrameEndTask(_ => controlChanging = false);
		}

		public void UnlinkMaster(Actor self, Actor master)
		{
			if (master == null)
				return;

			self.World.AddFrameEndTask(_ =>
			{
				if (master.IsDead || master.Disposed)
					return;

				master.Trait<MindController>().UnlinkSlave(master, self);
			});

			Master = null;

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		public void RevokeMindControl(Actor self)
		{
			self.CancelActivity();

			controlChanging = true;

			if (creatorOwner.WinState == WinState.Lost)
				self.ChangeOwner(Array.Find(self.World.Players, p => p.InternalName == info.FallbackOwner));
			else
				self.ChangeOwner(creatorOwner);

			UnlinkMaster(self, Master);

			if (info.RevokeControlSounds.Length > 0)
			{
				var pos = self.CenterPosition;
				if (info.AudibleThroughFog || (!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos)))
					Game.Sound.Play(SoundType.World, info.RevokeControlSounds.Random(self.World.SharedRandom), pos, info.Volume);
			}

			self.World.AddFrameEndTask(_ => controlChanging = false);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			MasterWhenDie = Master;
			UnlinkMaster(self, Master);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			UnlinkMaster(self, Master);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!controlChanging)
				UnlinkMaster(self, Master);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (Master != null)
				RevokeMindControl(self);
		}

		void TransferMindControl(Actor self, MindControllable mc)
		{
			Master = mc.Master;
			creatorOwner = mc.creatorOwner;
			controlChanging = mc.controlChanging;

			if (token == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.Condition))
				token = self.GrantCondition(Info.Condition);
		}

		void INotifyTransform.BeforeTransform(Actor self) { oldSelf = self; }
		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self)
		{
			if (Master != null)
			{
				var mc = self.TraitOrDefault<MindControllable>();
				if (mc != null)
				{
					mc.TransferMindControl(self, this);
					if (oldSelf != null)
						Master.Trait<MindController>().TransformSlave(oldSelf, self);
				}
				else
					self.ChangeOwner(creatorOwner);
			}
		}
	}
}
