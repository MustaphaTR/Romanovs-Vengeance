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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Flags]
	enum EngineerAction { CaptureActor = 1, RepairBase = 2, RepairBridge = 4 }

	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI traditional cnc engineer logic. Only consider closest target.",
		"Check only one of the enabled logics (capture, repair building and repair bridge) per tick.")]
	public sealed class CncEngineerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Actor types that used for engineer.",
			"Leave this empty to disable this bot module.")]
		public readonly HashSet<string> EngineerActorTypes = new();

		[Desc("Actor types that can be targeted for capturing (via `Captures`).",
			"Leave this empty to disable capture check.")]
		public readonly HashSet<string> CapturableActorTypes = new();

		[Desc("Player relationships that capturers should attempt to target.")]
		public readonly PlayerRelationship CapturableRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;

		[Desc("Actor types that can be targeted for engineer repairing (via `InstantlyRepairs`).",
			"Leave this empty to disable repair building check.")]
		public readonly HashSet<string> RepairableActorTypes = new();

		[Desc("Engineer repair actor when at this damage state.")]
		public readonly DamageState RepairableDamageState = DamageState.Heavy;

		[Desc("Actor types that can be targeted for bridge repairing (via `RepairsBridges`).",
			"Leave this empty to disable repair bridge check.")]
		public readonly HashSet<string> RepairableHutActorTypes = new();

		[Desc("Minimum delay (in ticks) between trying to giving out order for engineer.")]
		public readonly int AssignRoleDelay = 120;

		public override object Create(ActorInitializer init) { return new CncEngineerManagerBotModule(init.Self, this); }
	}

	public class CncEngineerManagerBotModule : ConditionalTrait<CncEngineerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsIdle;
		readonly Predicate<Actor> unitCannotBeOrderedOrIsBusy;
		readonly Predicate<Actor> unitCannotBeOrdered;

		// Units that the bot already knows about and has given a capture order. Any unit not on this list needs to be given a new order.
		readonly List<UnitWposWrapper> activeEngineers = new();
		readonly List<Actor> stuckEngineers = new();
		readonly EngineerAction[] enabledEngineerActions = Array.Empty<EngineerAction>();
		int minAssignRoleDelayTicks;
		int currentAction;

		public CncEngineerManagerBotModule(Actor self, CncEngineerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			if (world.Type == WorldType.Editor)
				return;

			unitCannotBeOrdered = a => a == null || a.Owner != player || a.IsDead || !a.IsInWorld;
			unitCannotBeOrderedOrIsIdle = a => unitCannotBeOrdered(a) || a.IsIdle;
			unitCannotBeOrderedOrIsBusy = a => unitCannotBeOrdered(a) || !(a.IsIdle || a.CurrentActivity is FlyIdle);

			var engineerActions = new List<EngineerAction>();
			if (info.CapturableActorTypes.Count > 0)
				engineerActions.Add(EngineerAction.CaptureActor);
			if (info.RepairableActorTypes.Count > 0)
				engineerActions.Add(EngineerAction.RepairBase);
			if (info.RepairableHutActorTypes.Count > 0)
				engineerActions.Add(EngineerAction.RepairBridge);

			enabledEngineerActions = engineerActions.ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			minAssignRoleDelayTicks = world.LocalRandom.Next(0, Info.AssignRoleDelay);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (--minAssignRoleDelayTicks <= 0)
			{
				minAssignRoleDelayTicks = Info.AssignRoleDelay;

				activeEngineers.RemoveAll(u => unitCannotBeOrderedOrIsIdle(u.Actor));
				stuckEngineers.RemoveAll(a => unitCannotBeOrdered(a));
				for (var i = 0; i < activeEngineers.Count; i++)
				{
					var engineer = activeEngineers[i];
					if (engineer.Actor.CurrentActivity.ChildActivity != null
						&& engineer.Actor.CurrentActivity.ChildActivity.ActivityType == ActivityType.Move
						&& engineer.Actor.CenterPosition == engineer.WPos)
					{
						stuckEngineers.Add(engineer.Actor);
						bot.QueueOrder(new Order("Stop", engineer.Actor, false));
						activeEngineers.RemoveAt(i);
						i--;
					}

					engineer.WPos = engineer.Actor.CenterPosition;
				}

				switch (enabledEngineerActions[currentAction])
				{
					case EngineerAction.RepairBase:
						QueueRepairBuildingOrders(bot);
						break;
					case EngineerAction.CaptureActor:
						QueueCaptureOrders(bot);
						break;
					case EngineerAction.RepairBridge:
						QueueRepairBridgeOrders(bot);
						break;
				}

				currentAction = (currentAction + 1) % enabledEngineerActions.Length;
			}
		}

		void QueueCaptureOrders(IBot bot)
		{
			if (Info.EngineerActorTypes.Count == 0 || player.WinState != WinState.Undefined)
				return;

			var capturers = world.ActorsHavingTrait<Captures>()
				.Where(a => Info.EngineerActorTypes.Contains(a.Info.Name) && a.Owner == player && !unitCannotBeOrderedOrIsBusy(a) && !stuckEngineers.Contains(a))
				.Select(a => new TraitPair<CaptureManager>(a, a.TraitOrDefault<CaptureManager>()))
				.Where(tp => tp.Trait != null)
				.ToArray();

			if (capturers.Length == 0)
				return;

			var targets = world.ActorsHavingTrait<Capturable>().Where(a => Info.CapturableActorTypes.Contains(a.Info.Name)).ToArray();
			if (targets.Length == 0)
				return;

			var capturerSent = false;
			foreach (var capturer in capturers)
			{
				foreach (var target in targets.OrderBy(a => (capturer.Actor.Location - a.Location).LengthSquared))
				{
					var captureManager = target.TraitOrDefault<CaptureManager>();
					if (captureManager == null)
						continue;

					if (!capturer.Trait.CanTarget(captureManager))
						continue;

					if (!AIUtils.PathExist(capturer.Actor, target.Location, target))
						continue;

					bot.QueueOrder(new Order("CaptureActor", capturer.Actor, Target.FromActor(target), true));
					AIUtils.BotDebug("AI ({0}): Ordered {1} to capture {2}", player.ClientIndex, capturer.Actor, target);
					activeEngineers.Add(new UnitWposWrapper(capturer.Actor));
					capturerSent = true;
					break;
				}

				if (capturerSent)
					break;
			}
		}

		void QueueRepairBuildingOrders(IBot bot)
		{
			if (Info.EngineerActorTypes.Count == 0 || player.WinState != WinState.Undefined)
				return;

			var repairers = world.ActorsHavingTrait<InstantlyRepairs>()
				.Where(a => Info.EngineerActorTypes.Contains(a.Info.Name) && a.Owner == player && !unitCannotBeOrderedOrIsBusy(a) && !stuckEngineers.Contains(a))
				.ToArray();

			if (repairers.Length == 0)
				return;

			var targets = world.ActorsHavingTrait<InstantlyRepairable>().Where(target =>
			{
				if (!Info.RepairableActorTypes.Contains(target.Info.Name))
					return false;

				if (target.Owner.RelationshipWith(player) != PlayerRelationship.Ally)
					return false;

				var health = target.TraitOrDefault<IHealth>();

				if (health == null || health.DamageState < Info.RepairableDamageState)
					return false;

				return true;
			}).ToArray();

			if (targets.Length == 0)
				return;

			var repairerSent = false;
			foreach (var r in repairers)
			{
				foreach (var target in targets.OrderBy(a => (r.Location - a.Location).LengthSquared))
				{
					if (!AIUtils.PathExist(r, target.Location, target))
						continue;

					bot.QueueOrder(new Order("InstantRepair", r, Target.FromActor(target), true));
					AIUtils.BotDebug("AI ({0}): Ordered {1} to Repair {2}", player.ClientIndex, r, target);
					activeEngineers.Add(new UnitWposWrapper(r));
					repairerSent = true;
					break;
				}

				if (repairerSent)
					break;
			}
		}

		void QueueRepairBridgeOrders(IBot bot)
		{
			if (Info.EngineerActorTypes.Count == 0 || player.WinState != WinState.Undefined)
				return;

			var brigdeRepairers = world.ActorsHavingTrait<RepairsBridges>()
				.Where(a => Info.EngineerActorTypes.Contains(a.Info.Name) && a.Owner == player && !unitCannotBeOrderedOrIsBusy(a) && !stuckEngineers.Contains(a))
				.ToArray();

			if (brigdeRepairers.Length == 0)
				return;

			// There is not many bridge actors in the map, we can use List here.
			var targets = world.ActorsWithTrait<BridgeHut>().Where(at => Info.RepairableHutActorTypes.Contains(at.Actor.Info.Name)
				&& at.Trait.BridgeDamageState >= DamageState.Dead).Select(at => at.Actor).ToList();

			targets.AddRange(world.ActorsWithTrait<LegacyBridgeHut>().Where(at => Info.RepairableHutActorTypes.Contains(at.Actor.Info.Name)
				&& at.Trait.BridgeDamageState >= DamageState.Dead).Select(at => at.Actor));

			if (targets.Count == 0)
				return;

			var repairerSent = false;
			foreach (var r in brigdeRepairers)
			{
				foreach (var target in targets.OrderBy(a => (r.Location - a.Location).LengthSquared))
				{
					if (!AIUtils.PathExist(r, target.Location, target))
						continue;

					bot.QueueOrder(new Order("RepairBridge", r, Target.FromActor(target), true));
					AIUtils.BotDebug("AI ({0}): Ordered {1} to repair bridge hut {2}", player.ClientIndex, r, target);
					activeEngineers.Add(new UnitWposWrapper(r));
					repairerSent = true;
					break;
				}

				if (repairerSent)
					break;
			}
		}
	}
}
