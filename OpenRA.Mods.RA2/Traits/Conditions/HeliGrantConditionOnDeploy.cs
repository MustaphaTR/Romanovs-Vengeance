#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.RA2.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Grants a condition when a deploy order is issued." +
		"Can be paused with the granted condition to disable undeploying.")]
	public class HeliGrantConditionOnDeployInfo : PausableConditionalTraitInfo, Requires<AircraftInfo>, IEditorActorOptions
	{
		[GrantedConditionReference]
		[Desc("The condition to grant while the actor is undeployed.")]
		public readonly string UndeployedCondition = null;

		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition to grant after deploying and revoke before undeploying.")]
		public readonly string DeployedCondition = null;

		[Desc("The terrain types that this actor can deploy on. Leave empty to allow any.")]
		public readonly HashSet<string> AllowedTerrainTypes = new HashSet<string>();

		[Desc("Can this actor deploy on slopes?")]
		public readonly bool CanDeployOnRamps = false;

		[Desc("Does this actor need to synchronize it's deployment with other actors?")]
		public readonly bool SynchronizeDeployment = false;

		[Desc("Cursor to display when able to (un)deploy the actor.")]
		public readonly string DeployCursor = "deploy";

		[Desc("Cursor to display when unable to (un)deploy the actor.")]
		public readonly string DeployBlockedCursor = "deploy-blocked";

		[SequenceReference]
		[Desc("Animation to play for deploying.")]
		public readonly string DeployAnimation = null;

		[Desc("Apply (un)deploy animations to sprite bodies with these names.")]
		public readonly string[] BodyNames = { "body" };

		[Desc("Facing that the actor must face before deploying. Set to -1 to deploy regardless of facing.")]
		public readonly int Facing = -1;

		[Desc("Sound to play when deploying.")]
		public readonly string DeploySound = null;

		[Desc("Sound to play when undeploying.")]
		public readonly string UndeploySound = null;

		[Desc("Should the aircraft automatically take off after undeploying?")]
		public readonly bool TakeOffOnUndeploy = true;

		[VoiceReference]
		public readonly string DeployVoice = "Action";

		[VoiceReference]
		public readonly string UndeployVoice = "Action";

		[Desc("Display order for the deployed checkbox in the map editor")]
		public readonly int EditorDeployedDisplayOrder = 4;

		IEnumerable<EditorActorOption> IEditorActorOptions.ActorOptions(ActorInfo ai, World world)
		{
			yield return new EditorActorCheckbox("Deployed", EditorDeployedDisplayOrder,
				actor =>
				{
					var init = actor.Init<DeployStateInit>();
					if (init != null)
						return init.Value(world) == DeployState.Deployed;

					return false;
				},
				(actor, value) =>
				{
					actor.ReplaceInit(new DeployStateInit(value ? DeployState.Deployed : DeployState.Undeployed));
				});
		}

		public override object Create(ActorInitializer init) { return new HeliGrantConditionOnDeploy(init, this); }
	}

	public class HeliGrantConditionOnDeploy : PausableConditionalTrait<HeliGrantConditionOnDeployInfo>, IResolveOrder, IIssueOrder,
		INotifyDeployComplete, IIssueDeployOrder, IOrderVoice
	{
		readonly Actor self;
		readonly bool checkTerrainType;
		readonly bool canTurn;

		DeployState deployState;
		ConditionManager conditionManager;
		WithSpriteBody[] wsbs;
		int deployedToken = ConditionManager.InvalidConditionToken;
		int undeployedToken = ConditionManager.InvalidConditionToken;

		public DeployState DeployState { get { return deployState; } }

		public HeliGrantConditionOnDeploy(ActorInitializer init, HeliGrantConditionOnDeployInfo info)
			: base(info)
		{
			self = init.Self;
			checkTerrainType = info.AllowedTerrainTypes.Count > 0;
			canTurn = self.Info.HasTraitInfo<IFacingInfo>();
			if (init.Contains<DeployStateInit>())
				deployState = init.Get<DeployStateInit, DeployState>();
		}

		protected override void Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			wsbs = self.TraitsImplementing<WithSpriteBody>().Where(w => Info.BodyNames.Contains(w.Info.Name)).ToArray();

			switch (deployState)
			{
				case DeployState.Undeployed:
					OnUndeployCompleted();
					break;
				case DeployState.Deploying:
					if (canTurn)
						self.Trait<IFacing>().Facing = Info.Facing;

					Deploy(true);
					break;
				case DeployState.Deployed:
					if (canTurn)
						self.Trait<IFacing>().Facing = Info.Facing;

					OnDeployCompleted();
					break;
				case DeployState.Undeploying:
					if (canTurn)
						self.Trait<IFacing>().Facing = Info.Facing;

					Undeploy(true);
					break;
			}
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new DeployOrderTargeter("HeliGrantConditionOnDeploy", 5,
						() => IsCursorBlocked() ? Info.DeployBlockedCursor : Info.DeployCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "HeliGrantConditionOnDeploy")
			{
				var gcodorder = new Order(order.OrderID, self, queued);

				if (Info.SynchronizeDeployment)
				{
					var actors = self.World.Selection.Actors.Select(x => x.ActorID.ToString());
					gcodorder.TargetString = string.Join(",", actors);
				}

				return gcodorder;
			}

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			var gcodorder = new Order("HeliGrantConditionOnDeploy", self, queued);
			if (Info.SynchronizeDeployment)
			{
				var actors = self.World.Selection.Actors.Select(x => x.ActorID.ToString());
				gcodorder.TargetString = string.Join(",", actors);
			}

			return gcodorder;
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self) { return !IsTraitPaused && !IsTraitDisabled; }

		bool IsGroupDeployNeeded(Actor self, string actorString)
		{
			if (string.IsNullOrEmpty(actorString))
				return false;

			var actorIDs = actorString.Split(',').Select(x => { uint result; uint.TryParse(x, out result); return result; });
			var actors = self.World.Actors.Where(x => x.IsInWorld && !x.IsDead && actorIDs.Contains(x.ActorID));

			foreach (var a in actors)
			{
				var gcod = a.TraitOrDefault<HeliGrantConditionOnDeploy>();
				if (gcod != null && gcod.DeployState != DeployState.Deployed)
					return true;
			}

			return false;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (order.OrderString != "HeliGrantConditionOnDeploy" || deployState == DeployState.Deploying || deployState == DeployState.Undeploying)
				return;

			if (Info.SynchronizeDeployment && deployState == DeployState.Deployed && IsGroupDeployNeeded(self, order.TargetString))
				return;

			if (!order.Queued)
				self.CancelActivity();

			if (deployState == DeployState.Deployed)
				self.QueueActivity(new HeliUndeployForGrantedCondition(self, this));
			else if (deployState == DeployState.Undeployed)
				self.QueueActivity(new HeliDeployForGrantedCondition(self, this));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "HeliGrantConditionOnDeploy" ? GetVoiceLine() : null;
		}

		string GetVoiceLine()
		{
			if (deployState == DeployState.Deployed)
				return Info.UndeployVoice;

			return Info.DeployVoice;
		}

		bool IsCursorBlocked()
		{
			if (IsTraitPaused)
				return true;

			return !IsValidTerrain(self.Location) && (deployState != DeployState.Deployed);
		}

		public bool IsValidTerrain(CPos location)
		{
			return IsValidTerrainType(location) && IsValidRampType(location);
		}

		bool IsValidTerrainType(CPos location)
		{
			if (!self.World.Map.Contains(location))
				return false;

			if (!checkTerrainType)
				return true;

			var terrainType = self.World.Map.GetTerrainInfo(location).Type;

			return Info.AllowedTerrainTypes.Contains(terrainType);
		}

		bool IsValidRampType(CPos location)
		{
			if (Info.CanDeployOnRamps)
				return true;

			var ramp = 0;
			if (self.World.Map.Contains(location))
			{
				var tile = self.World.Map.Tiles[location];
				var ti = self.World.Map.Rules.TileSet.GetTileInfo(tile);
				if (ti != null)
					ramp = ti.RampType;
			}

			return ramp == 0;
		}

		void INotifyDeployComplete.FinishedDeploy(Actor self)
		{
			OnDeployCompleted();
		}

		void INotifyDeployComplete.FinishedUndeploy(Actor self)
		{
			OnUndeployCompleted();
		}

		/// <summary>Play deploy sound and animation.</summary>
		public void Deploy() { Deploy(false); }
		void Deploy(bool init)
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (!init && deployState != DeployState.Undeployed)
				return;

			if (!IsValidTerrain(self.Location))
				return;

			if (!string.IsNullOrEmpty(Info.DeploySound))
				Game.Sound.Play(SoundType.World, Info.DeploySound, self.CenterPosition);

			// Revoke condition that is applied while undeployed.
			if (!init)
				OnDeployStarted();

			var wsb = wsbs.FirstEnabledTraitOrDefault();

			// If there is no animation to play just grant the upgrades that are used while deployed.
			// Alternatively, play the deploy animation and then grant the upgrades.
			if (string.IsNullOrEmpty(Info.DeployAnimation) || wsb == null)
			{
				Game.Debug("wsb is null.");
				OnDeployCompleted();
			}
			else
				wsb.PlayCustomAnimation(self, Info.DeployAnimation, OnDeployCompleted);
		}

		/// <summary>Play undeploy sound and animation and after that revoke the condition.</summary>
		public void Undeploy() { Undeploy(false); }
		void Undeploy(bool init)
		{
			// Something went wrong, most likely due to deploy order spam and the fact that this is a delayed action.
			if (!init && deployState != DeployState.Deployed)
				return;

			if (!string.IsNullOrEmpty(Info.UndeploySound))
				Game.Sound.Play(SoundType.World, Info.UndeploySound, self.CenterPosition);

			var wsb = wsbs.FirstEnabledTraitOrDefault();

			if (!init)
				OnUndeployStarted();

			if (string.IsNullOrEmpty(Info.DeployAnimation) || wsb == null)
				OnUndeployCompleted();
			else
				wsb.PlayCustomAnimationBackwards(self, Info.DeployAnimation, OnUndeployCompleted);
		}

		void OnDeployStarted()
		{
			if (undeployedToken != ConditionManager.InvalidConditionToken)
				undeployedToken = conditionManager.RevokeCondition(self, undeployedToken);

			deployState = DeployState.Deploying;
		}

		void OnDeployCompleted()
		{
			if (conditionManager != null && !string.IsNullOrEmpty(Info.DeployedCondition) && deployedToken == ConditionManager.InvalidConditionToken)
				deployedToken = conditionManager.GrantCondition(self, Info.DeployedCondition);

			deployState = DeployState.Deployed;
		}

		void OnUndeployStarted()
		{
			if (deployedToken != ConditionManager.InvalidConditionToken)
				deployedToken = conditionManager.RevokeCondition(self, deployedToken);

			deployState = DeployState.Deploying;
		}

		void OnUndeployCompleted()
		{
			if (conditionManager != null && !string.IsNullOrEmpty(Info.UndeployedCondition) && undeployedToken == ConditionManager.InvalidConditionToken)
				undeployedToken = conditionManager.GrantCondition(self, Info.UndeployedCondition);

			if (Info.TakeOffOnUndeploy)
				self.QueueActivity(new Fly(self, Target.FromCell(self.World, self.Location)));

			deployState = DeployState.Undeployed;
		}
	}
}
