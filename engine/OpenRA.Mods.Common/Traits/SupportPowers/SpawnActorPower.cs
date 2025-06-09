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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns an actor that stays for a limited amount of time.")]
	public class SpawnActorPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("Actors to spawn for each level.")]
		public readonly Dictionary<int, string> Actors = new();

		[Desc("Amount of time to keep the actor alive in ticks. Value < 0 means this actor will not remove itself.")]
		public readonly int LifeTime = 250;

		[Desc("Only allow this to be spawned on this terrain.")]
		public readonly string[] Terrain = null;

		public readonly bool AllowUnderShroud = true;

		public readonly string DeploySound = null;

		public readonly string EffectImage = null;

		[SequenceReference(nameof(EffectImage), allowNullImage: true)]
		public readonly string EffectSequence = null;

		[PaletteReference(nameof(EffectPaletteIsPlayerPalette))]
		public readonly string EffectPalette = null;

		public readonly bool EffectPaletteIsPlayerPalette = false;

		public readonly Dictionary<int, WDist> TargetCircleRanges;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;
		public readonly float TargetCircleWidth = 1;
		public readonly Color TargetCircleBorderColor = Color.FromArgb(96, Color.Black);
		public readonly float TargetCircleBorderWidth = 3;

		public override object Create(ActorInitializer init) { return new SpawnActorPower(init.Self, this); }
	}

	public class SpawnActorPower : SupportPower
	{
		public new readonly SpawnActorPowerInfo Info;

		public SpawnActorPower(Actor self, SpawnActorPowerInfo info)
			: base(self, info)
		{
			Info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var level = GetLevel();
			if (level == 0)
				return;

			var position = order.Target.CenterPosition;
			var cell = self.World.Map.CellContaining(position);

			if (!Validate(self.World, Info, cell))
				return;

			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();
				Game.Sound.Play(SoundType.World, Info.DeploySound, position);

				if (!string.IsNullOrEmpty(Info.EffectSequence) && !string.IsNullOrEmpty(Info.EffectPalette))
				{
					var palette = Info.EffectPalette;
					if (Info.EffectPaletteIsPlayerPalette)
						palette += self.Owner.InternalName;

					w.Add(new SpriteEffect(position, w, Info.EffectImage, Info.EffectSequence, palette));
				}

				var actor = w.CreateActor(Info.Actors.First(a => a.Key == level).Value,
				[
					new LocationInit(cell),
					new OwnerInit(self.Owner),
				]);

				if (Info.LifeTime > -1)
				{
					actor.QueueActivity(new Wait(Info.LifeTime));
					actor.QueueActivity(new RemoveSelf());
				}
			});
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(manager.Self.Owner, Info.SelectTargetTextNotification);

			self.World.OrderGenerator = new SelectSpawnActorPowerTarget(order, manager, this, MouseButton.Left);
		}

		public bool Validate(World world, SpawnActorPowerInfo info, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (!info.AllowUnderShroud && world.ShroudObscures(cell))
				return false;

			if (info.Terrain != null && !info.Terrain.Contains(world.Map.GetTerrainInfo(cell).Type))
				return false;

			return true;
		}
	}

	public class SelectSpawnActorPowerTarget : OrderGenerator
	{
		readonly SpawnActorPower power;
		readonly SpawnActorPowerInfo info;
		readonly SupportPowerManager manager;
		readonly MouseButton expectedButton;

		public string OrderKey { get; }

		public SelectSpawnActorPowerTarget(string order, SupportPowerManager manager, SpawnActorPower power, MouseButton button)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.manager = manager;
			this.power = power;
			OrderKey = order;
			expectedButton = button;

			info = power.Info;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();

			if (!power.Validate(world, info, cell))
				yield break;

			if (mi.Button == expectedButton)
				yield return new Order(OrderKey, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(OrderKey))
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var level = power.GetLevel();
			if (level == 0)
				yield break;

			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

			if (power.Info.TargetCircleRanges != null && power.Info.TargetCircleRanges.Count > 0)
			{
				yield return new RangeCircleAnnotationRenderable(
					world.Map.CenterOfCell(xy),
					power.Info.TargetCircleRanges[level],
					0,
					power.Info.TargetCircleUsePlayerColor ? power.Self.Owner.Color : power.Info.TargetCircleColor,
					power.Info.TargetCircleWidth,
					power.Info.TargetCircleBorderColor,
					power.Info.TargetCircleBorderWidth);
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return power.Validate(world, info, cell) ? info.Cursor : info.BlockedCursor;
		}
	}
}
