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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	public class PlaceBuildingPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("The building to be placed.")]
		public readonly Dictionary<int, string> Buildings = new();

		[Desc("Force a specific faction variant, overriding the faction of the producing actor.")]
		public readonly string ForceFaction = null;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play if building placement is not possible.")]
		public readonly string CannotPlaceNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification to display if building placement is not possible.")]
		public readonly string CannotPlaceTextNotification = null;

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[SequenceReference]
		[Desc("Sequence to play for granting actor when activated.",
			"This requires the actor to have the WithSpriteBody trait or one of its derivatives.")]
		public readonly string Sequence = "active";

		public readonly string EffectImage = null;

		[SequenceReference(nameof(EffectImage), allowNullImage: true)]
		public readonly string EffectSequence = null;

		[PaletteReference]
		public readonly string EffectPalette = null;

		public readonly string FootprintImage = "overlay";

		[SequenceReference(nameof(FootprintImage))]
		public readonly string FootprintSequence = "target-select";

		public override object Create(ActorInitializer init) { return new PlaceBuildingPower(init.Self, this); }
	}

	public class PlaceBuildingPower : SupportPower, IRenderModifier
	{
		readonly PlaceBuildingPowerInfo info;
		WorldRenderer wr;

		public PlaceBuildingPower(Actor self, PlaceBuildingPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new PlaceBuildingPowerTarget(Self.World, wr, order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var level = GetLevel();
			if (level == 0)
				return;

			base.Activate(self, order, manager);
			PlayLaunchSounds();

			var position = order.Target.CenterPosition;
			if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
				self.World.Add(new SpriteEffect(position, self.World, info.EffectImage, info.EffectSequence, info.EffectPalette));

			var wsb = self.TraitOrDefault<WithSpriteBody>();
			if (wsb != null && wsb.DefaultAnimation.HasSequence(info.Sequence))
				wsb.PlayCustomAnimation(self, info.Sequence);

			Game.Sound.Play(SoundType.World, info.OnFireSound, position);

			var w = self.World;
			var actorInfo = self.World.Map.Rules.Actors[info.Buildings.First(c => c.Key == level).Value];
			var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();
			var faction = info.ForceFaction ?? self.Owner.Faction.InternalName;
			var targetLocation = w.Map.CellContaining(position);

			if (!self.World.CanPlaceBuilding(targetLocation, actorInfo, buildingInfo, null)
				|| !buildingInfo.IsCloseEnoughToBase(self.World, order.Player, actorInfo, Self, targetLocation))
				return;

			var building = w.CreateActor(actorInfo.Name, new TypeDictionary
			{
				new LocationInit(targetLocation),
				new OwnerInit(order.Player),
				new FactionInit(faction),
				new PlaceBuildingInit()
			});

			var pos = building.CenterPosition;
			if (buildingInfo.AudibleThroughFog || (!w.ShroudObscures(pos) && !w.FogObscures(pos)))
				foreach (var s in buildingInfo.BuildSounds)
					Game.Sound.Play(SoundType.World, s, pos, buildingInfo.SoundVolume);
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r) { this.wr = wr; return r; }
		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds) { return bounds; }

		sealed class PlaceBuildingPowerTarget : OrderGenerator
		{
			readonly string worldDefaultCursor = ChromeMetrics.Get<string>("WorldDefaultCursor");

			readonly PlaceBuildingPower power;
			readonly SupportPowerManager manager;
			readonly Dictionary<int, ActorInfo> actorInfos = new();
			readonly Dictionary<int, IPlaceBuildingPreview> previews = new();
			readonly string order;
			readonly IResourceLayer resourceLayer;
			readonly Viewport viewport;

			public PlaceBuildingPowerTarget(World world, WorldRenderer wr, string order, SupportPowerManager manager, PlaceBuildingPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
				resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
				viewport = wr.Viewport;

				foreach (var pair in power.info.Buildings)
				{
					var actorInfo = world.Map.Rules.Actors[pair.Value];
					actorInfos.Add(pair.Key, actorInfo);

					var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();
					var previewGeneratorInfo = actorInfo.TraitInfoOrDefault<IPlaceBuildingPreviewGeneratorInfo>();
					if (previewGeneratorInfo != null)
					{
						var faction = power.info.ForceFaction ?? power.Self.Owner.Faction.InternalName;

						var td = new TypeDictionary()
						{
							new FactionInit(faction),
							new OwnerInit(power.Self.Owner),
						};

						foreach (var api in actorInfo.TraitInfos<IActorPreviewInitInfo>())
							foreach (var o in api.ActorPreviewInits(actorInfo, ActorPreviewType.PlaceBuilding))
								td.Add(o);

						previews.Add(pair.Key, previewGeneratorInfo.CreatePreview(wr, actorInfo, td));
					}
				}
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				var level = power.GetLevel();
				if (level == 0)
					yield break;

				var ai = actorInfos.First(c => c.Key == level).Value;
				var bi = ai.TraitInfo<BuildingInfo>();
				var preview = previews.First(c => c.Key == level).Value;
				var topLeft = TopLeft(preview);
				if (world.CanPlaceBuilding(topLeft, ai, bi, null))
					yield return new Order(order, manager.Self, Target.FromCell(world, topLeft), false) { SuppressVisualFeedback = true };
				else
				{
					var owner = power.Self.Owner;
					Game.Sound.PlayNotification(world.Map.Rules, owner, "Speech", power.info.CannotPlaceNotification, owner.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(owner, power.info.CannotPlaceTextNotification);
				}
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			CPos TopLeft(IPlaceBuildingPreview preview)
			{
				var offsetPos = Viewport.LastMousePos;
				if (preview != null)
					offsetPos += preview.TopLeftScreenOffset;

				return viewport.ViewToWorld(offsetPos);
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
			{
				var level = power.GetLevel();
				if (level == 0)
					yield break;

				var actorInfo = actorInfos.First(c => c.Key == level).Value;
				var buildingInfo = actorInfo.TraitInfo<BuildingInfo>();
				var preview = previews.First(c => c.Key == level).Value;
				var topLeft = TopLeft(preview);
				var footprint = new Dictionary<CPos, PlaceBuildingCellType>();
				var isCloseEnough = buildingInfo.IsCloseEnoughToBase(world, world.LocalPlayer, actorInfo, power.Self, topLeft);
				foreach (var t in buildingInfo.Tiles(topLeft))
					footprint.Add(
						t,
						PlaceBuildingOrderGenerator.MakeCellType(
							isCloseEnough &&
							world.IsCellBuildable(t, topLeft, actorInfo, buildingInfo) &&
							(resourceLayer == null || resourceLayer.GetResource(t).Type == null)));

				foreach (var p in preview.Render(wr, topLeft, footprint))
					yield return p;
			}

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return worldDefaultCursor;
			}
		}
	}
}
