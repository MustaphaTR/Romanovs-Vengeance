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

namespace OpenRA.Mods.Common.Traits
{
	public class GrantExternalConditionPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly Dictionary<int, string> Conditions = new();

		[FieldLoader.Require]
		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly Dictionary<int, int> Durations = new();

		[FieldLoader.Require]
		[Desc("Size of the footprint of the affected area.")]
		public readonly Dictionary<int, CVec> Dimensions = new();

		[FieldLoader.Require]
		[Desc("Actual footprint. Cells marked as x will be affected.")]
		public readonly Dictionary<int, string> Footprints = new();

		[Desc("Sound to instantly play at the targeted area.")]
		public readonly string OnFireSound = null;

		[Desc("Player relationships which condition can be applied to.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

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

		public override object Create(ActorInitializer init) { return new GrantExternalConditionPower(init.Self, this); }
	}

	public class GrantExternalConditionPower : SupportPower
	{
		readonly GrantExternalConditionPowerInfo info;
		readonly Dictionary<int, char[]> footprints = new();

		public GrantExternalConditionPower(Actor self, GrantExternalConditionPowerInfo info)
			: base(self, info)
		{
			this.info = info;
			foreach (var pair in info.Footprints)
				footprints.Add(pair.Key, pair.Value.Where(c => !char.IsWhiteSpace(c)).ToArray());
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectConditionTarget(Self.World, order, manager, this);
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

			foreach (var a in UnitsInRange(self.World.Map.CellContaining(position)))
				a.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == info.Conditions.First(c => c.Key == level).Value && t.CanGrantCondition(self))
					?.GrantCondition(a, self, info.Durations.First(d => d.Key == level).Value);
		}

		public IEnumerable<Actor> UnitsInRange(CPos xy)
		{
			var units = new HashSet<Actor>();
			var level = GetLevel();
			if (level == 0)
				return units;

			var tiles = CellsMatching(xy, footprints.First(f => f.Key == level).Value, info.Dimensions.First(d => d.Key == level).Value);
			foreach (var t in tiles)
				foreach (var a in Self.World.ActorMap.GetActorsAt(t))
					units.Add(a);

			var condition = info.Conditions.First(c => c.Key == level).Value;
			return units.Where(a =>
			{
				if (!info.ValidRelationships.HasRelationship(Self.Owner.RelationshipWith(a.Owner)))
					return false;

				return a.TraitsImplementing<ExternalCondition>()
					.Any(t => t.Info.Condition == condition && t.CanGrantCondition(Self));
			});
		}

		sealed class SelectConditionTarget : OrderGenerator
		{
			readonly GrantExternalConditionPower power;
			readonly Dictionary<int, char[]> footprints = new();
			readonly Dictionary<int, CVec> dimensions;
			readonly Sprite tile;
			readonly float alpha;
			readonly SupportPowerManager manager;
			readonly string order;

			public SelectConditionTarget(World world, string order, SupportPowerManager manager, GrantExternalConditionPower power)
			{
				// Clear selection if using Left-Click Orders
				if (Game.Settings.Game.UseClassicMouseStyle)
					manager.Self.World.Selection.Clear();

				this.manager = manager;
				this.order = order;
				this.power = power;
				foreach (var pair in power.info.Footprints)
					footprints.Add(pair.Key, pair.Value.Where(c => !char.IsWhiteSpace(c)).ToArray());

				dimensions = power.info.Dimensions;

				var sequence = world.Map.Sequences.GetSequence(power.info.FootprintImage, power.info.FootprintSequence);
				tile = sequence.GetSprite(0);
				alpha = sequence.GetAlpha(0);
			}

			protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == MouseButton.Left && power.UnitsInRange(cell).Any())
					yield return new Order(order, manager.Self, Target.FromCell(world, cell), false) { SuppressVisualFeedback = true };
			}

			protected override void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
					world.CancelInputMode();
			}

			protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

			protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
			{
				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				foreach (var unit in power.UnitsInRange(xy))
				{
					var decorations = unit.TraitsImplementing<ISelectionDecorations>().FirstEnabledTraitOrDefault();
					if (decorations != null)
						foreach (var d in decorations.RenderSelectionAnnotations(unit, wr, Color.Red))
							yield return d;
				}
			}

			protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world)
			{
				var level = power.GetLevel();
				if (level == 0)
					yield break;

				var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
				var pal = wr.Palette(TileSet.TerrainPaletteInternalName);

				foreach (var t in power.CellsMatching(xy, footprints.First(f => f.Key == level).Value, dimensions.First(d => d.Key == level).Value))
					yield return new SpriteRenderable(tile, wr.World.Map.CenterOfCell(t), WVec.Zero, -511, pal, 1f, alpha, float3.Ones, TintModifiers.IgnoreWorldTint, true);
			}

			protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
			{
				return power.UnitsInRange(cell).Any() ? power.info.Cursor : power.info.BlockedCursor;
			}
		}
	}
}
