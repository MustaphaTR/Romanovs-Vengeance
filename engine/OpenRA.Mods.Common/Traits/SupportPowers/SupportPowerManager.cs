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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Attach this to the player actor.")]
	public class SupportPowerManagerInfo : TraitInfo, Requires<DeveloperModeInfo>, Requires<TechTreeInfo>
	{
		public override object Create(ActorInitializer init) { return new SupportPowerManager(init); }
	}

	public class SupportPowerManager : ITick, IResolveOrder, ITechTreeElement
	{
		public readonly Actor Self;
		public readonly Dictionary<string, SupportPowerInstance> Powers = [];
		public readonly HashSet<string> ObtainedSupportPower = [string.Empty];

		public readonly DeveloperMode DevMode;
		public readonly TechTree TechTree;
		public readonly Lazy<RadarPings> RadarPings;

		public SupportPowerManager(ActorInitializer init)
		{
			Self = init.Self;
			DevMode = Self.Trait<DeveloperMode>();
			TechTree = Self.Trait<TechTree>();
			RadarPings = Exts.Lazy(Self.World.WorldActor.TraitOrDefault<RadarPings>);

			init.World.ActorAdded += ActorAdded;
			init.World.ActorRemoved += ActorRemoved;
		}

		static string MakeKey(SupportPower sp)
		{
			return sp.Info.AllowMultiple ? sp.Info.OrderName + "_" + sp.Self.ActorID : sp.Info.OrderName;
		}

		void ActorAdded(Actor a)
		{
			if (a.Owner != Self.Owner)
				return;

			foreach (var t in a.TraitsImplementing<SupportPower>())
			{
				var key = MakeKey(t);

				var hasKey = Powers.ContainsKey(key);
				if (!hasKey)
					Powers.Add(key, t.CreateInstance(key, this));

				Powers[key].Instances.Add(t);

				if (!hasKey)
				{
					foreach (var prerequisite in t.Info.Prerequisites)
					{
						var techKey = key + prerequisite.Key;
						TechTree.Add(techKey, prerequisite.Value, 0, this);
					}

					TechTree.Update();
				}
			}
		}

		void ActorRemoved(Actor a)
		{
			if (a.Owner != Self.Owner || !a.Info.HasTraitInfo<SupportPowerInfo>())
				return;

			foreach (var t in a.TraitsImplementing<SupportPower>())
			{
				var key = MakeKey(t);
				Powers[key].Instances.Remove(t);

				if (Powers[key].Instances.Count == 0)
				{
					Powers.Remove(key);

					foreach (var prerequisite in t.Info.Prerequisites)
					{
						var techKey = key + prerequisite.Key;
						TechTree.Remove(techKey);
					}

					TechTree.Update();
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			foreach (var power in Powers.Values)
				power.Tick();
		}

		public void ResolveOrder(Actor self, Order order)
		{
			// order.OrderString is the key of the support power
			if (Powers.TryGetValue(order.OrderString, out var sp))
				sp.Activate(order);
		}

		static readonly SupportPowerInstance[] NoInstances = [];

		public IEnumerable<SupportPowerInstance> GetPowersForActor(Actor a)
		{
			if (Powers.Count == 0 || a.Owner != Self.Owner || !a.Info.HasTraitInfo<SupportPowerInfo>())
				return NoInstances;

			return a.TraitsImplementing<SupportPower>()
				.Select(t => Powers[MakeKey(t)])
				.Where(p => p.Instances.Any(i => !i.IsTraitDisabled && i.Self == a));
		}

		void ITechTreeElement.PrerequisitesAvailable(string key)
		{
			if (!Powers.TryGetValue(key.Remove(key.Length - 1), out var sp))
				return;

			sp.CheckPrerequisites(false);
		}

		void ITechTreeElement.PrerequisitesUnavailable(string key)
		{
			if (!Powers.TryGetValue(key.Remove(key.Length - 1), out var sp))
				return;

			sp.CheckPrerequisites(false);
		}

		void ITechTreeElement.PrerequisitesItemHidden(string key) { }
		void ITechTreeElement.PrerequisitesItemVisible(string key) { }
	}

	public class SupportPowerInstance
	{
		protected readonly SupportPowerManager Manager;

		public readonly string Key;

		public readonly List<SupportPower> Instances = [];
		public readonly int TotalTicks;

		protected int remainingSubTicks;
		public int RemainingTicks => remainingSubTicks / 100;
		public bool Active { get; private set; }
		public bool Disabled =>
			Manager.Self.Owner.WinState == WinState.Lost ||
			(!prereqsAvailable && !Manager.DevMode.AllTech) ||
			!instancesEnabled ||
			oneShotFired;

		public SupportPowerInfo Info { get { return Instances.Select(i => i.Info).FirstOrDefault(); } }
		public bool Ready => Active && RemainingTicks == 0;

		bool instancesEnabled;
		bool prereqsAvailable = true;
		bool oneShotFired;
		protected bool notifiedCharging;
		bool notifiedReady;

		public void ResetTimer()
		{
			remainingSubTicks = TotalTicks * 100;
		}

		public SupportPowerInstance(string key, SupportPowerInfo info, SupportPowerManager manager)
		{
			Key = key;
			TotalTicks = info.ChargeInterval;

			var supportpowerID = info.StartFullyChargedForTheFirstTime ? info.Names[info.Names.Keys.Min()] + info.OrderName : string.Empty;
			if (!manager.ObtainedSupportPower.Contains(supportpowerID))
			{
				remainingSubTicks = 0;
				manager.ObtainedSupportPower.Add(supportpowerID);
			}
			else
				remainingSubTicks = info.StartFullyCharged ? 0 : TotalTicks * 100;

			Manager = manager;
		}

		public void CheckPrerequisites(bool disable)
		{
			if (disable)
				prereqsAvailable = false;
			else
				prereqsAvailable = GetLevel() != 0;
		}

		public virtual void Tick()
		{
			instancesEnabled = Instances.Any(i => !i.IsTraitDisabled);
			if (!instancesEnabled)
				remainingSubTicks = TotalTicks * 100;

			Active = !Disabled && Instances.Any(i => !i.IsTraitPaused);
			if (!Active)
				return;

			var power = Instances[0];
			if (Manager.DevMode.FastCharge && remainingSubTicks > 2500)
				remainingSubTicks = 2500;

			if (remainingSubTicks > 0)
				remainingSubTicks = (remainingSubTicks - 100).Clamp(0, TotalTicks * 100);

			if (!notifiedCharging)
			{
				power.Charging(power.Self, Key);
				notifiedCharging = true;
			}

			if (RemainingTicks == 0 && !notifiedReady)
			{
				power.Charged(power.Self, Key);
				notifiedReady = true;
			}
		}

		public virtual void Target()
		{
			if (!Ready)
				return;

			var power = Instances.FirstOrDefault(i => !i.IsTraitPaused);

			if (power == null)
				return;

			if (!HasSufficientFunds(power))
				return;

			Game.Sound.PlayToPlayer(SoundType.UI, Manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(power.Self.World.Map.Rules, power.Self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, power.Self.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(power.Self.Owner, Info.SelectTargetTextNotification);

			power.SelectTarget(power.Self, Key, Manager);
		}

		public virtual void Activate(Order order)
		{
			if (!Ready)
				return;

			var power = Instances.Where(i => !i.IsTraitPaused && !i.IsTraitDisabled)
				.MinByOrDefault(a =>
				{
					if (a.Self.OccupiesSpace == null || order.Target.Type == TargetType.Invalid)
						return 0;

					return (a.Self.CenterPosition - order.Target.CenterPosition).HorizontalLengthSquared;
				});

			if (power == null)
				return;

			if (!HasSufficientFunds(power, true))
				return;

			// Note: order.Subject is the *player* actor
			power.Activate(power.Self, order, Manager);
			remainingSubTicks = TotalTicks * 100;
			notifiedCharging = notifiedReady = false;

			if (Info.OneShot)
			{
				CheckPrerequisites(true);
				oneShotFired = true;
			}
		}

		bool HasSufficientFunds(SupportPower power, bool activate = false)
		{
			if (power.Info.Cost != 0)
			{
				var player = Manager.Self;
				var pr = player.Trait<PlayerResources>();
				if (pr.Cash + pr.Resources < power.Info.Cost)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player.Owner, "Speech",
						pr.Info.InsufficientFundsNotification, player.Owner.Faction.InternalName);
					return false;
				}

				if (activate)
					pr.TakeCash(power.Info.Cost);
			}

			return true;
		}

		public int GetLevel()
		{
			if (Info == null)
				return 0;

			var availables = Info.Prerequisites.Where(p => Manager.TechTree.HasPrerequisites(p.Value));
			var level = availables.Any() ? availables.Max(p => p.Key) : 0;

			return Manager.DevMode.AllTech ? Info.Prerequisites.Max(p => p.Key) : level;
		}

		public virtual string IconOverlayTextOverride()
		{
			return null;
		}

		public virtual string TooltipTimeTextOverride()
		{
			return null;
		}
	}

	public class SelectGenericPowerTarget : OrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly SupportPowerInfo info;
		readonly MouseButton expectedButton;

		public string OrderKey { get; }

		public SelectGenericPowerTarget(string order, SupportPowerManager manager, SupportPowerInfo info, MouseButton button)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.manager = manager;
			OrderKey = order;
			this.info = info;
			expectedButton = button;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == expectedButton && world.Map.Contains(cell))
				yield return new Order(OrderKey, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.TryGetValue(OrderKey, out var p) || !p.Active || !p.Ready)
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }
		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? info.Cursor : info.BlockedCursor;
		}
	}
}
