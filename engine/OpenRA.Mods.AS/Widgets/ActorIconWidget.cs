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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.AS.Widgets
{
	public class BasicUnit
	{
		public readonly Actor Actor;
		public readonly ActorInfo ActorInfo;
		public readonly Tooltip[] Tooltips;
		public readonly TooltipDescription[] TooltipDescriptions;
		public readonly BuildableInfo BuildableInfo;

		public BasicUnit(Actor actor, ActorInfo actorInfo)
		{
			Actor = actor;
			ActorInfo = actorInfo ?? actor.Info;

			Tooltips = actor?.TraitsImplementing<Tooltip>().ToArray();
			TooltipDescriptions = actor?.TraitsImplementing<TooltipDescription>().ToArray();
			BuildableInfo = ActorInfo.TraitInfos<BuildableInfo>().FirstOrDefault();
		}
	}

	public class ActorIconWidget : Widget
	{
		public readonly int2 IconSize;
		public readonly int2 IconPos;
		public readonly float IconScale = 1f;
		public readonly string NoIconImage = "icon";
		public readonly string NoIconSequence = "xxicon";
		public readonly string NoIconPalette = "chrome";
		public readonly string DefaultIconImage = "icon";
		public readonly string DefaultIconSequence = "xxicon";
		public readonly string DefaultIconPalette = "chrome";
		public readonly string DisabledOverlayImage = "clock";
		public readonly string DisabledOverlaySequence = "idle";
		public readonly string DisabledOverlayPalette = "chrome";

		public readonly string TooltipTemplate = "ACTOR_ICON_TOOLTIP";
		public readonly string TooltipContainer;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		public BasicUnit TooltipUnit { get; private set; }
		public Func<BasicUnit> GetTooltipUnit;

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;
		Animation icon;
		readonly Animation disabledOverlay;
		ActorStatValues stats;
		readonly Lazy<TooltipContainerWidget> tooltipContainer;

		Player player;
		readonly World world;
		/* readonly float2 iconOffset;*/

		public Func<Actor> GetActor = () => null;
		Actor actor = null;

		public Func<ActorInfo> GetActorInfo = () => null;
		ActorInfo actorInfo = null;

		public Func<bool> GetDisabled = () => false;
		bool isDisabled = false;

		string currentPalette;
		bool currentPaletteIsPlayerPalette;
		readonly ISelection selection;

		[ObjectCreator.UseCtor]
		public ActorIconWidget(ModData modData, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			selection = world.WorldActor.Trait<ISelection>();

			/*iconOffset = 0.5f * IconSize.ToFloat2() + IconPos;*/

			currentPalette = NoIconPalette;
			currentPaletteIsPlayerPalette = false;
			icon = new Animation(world, NoIconImage);
			icon.Play(NoIconSequence);

			GetTooltipUnit = () => TooltipUnit;
			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			disabledOverlay = new Animation(world, DisabledOverlayImage);
			disabledOverlay.PlayFetchIndex(DisabledOverlaySequence, () => 0);
		}

		protected ActorIconWidget(ActorIconWidget other)
			: base(other)
		{
			modData = other.modData;
			world = other.world;
			worldRenderer = other.worldRenderer;
			selection = other.selection;

			IconSize = other.IconSize;
			IconPos = other.IconPos;
			IconScale = other.IconScale;
			NoIconImage = other.NoIconImage;
			NoIconSequence = other.NoIconSequence;
			NoIconPalette = other.NoIconPalette;
			DefaultIconImage = other.DefaultIconImage;
			DefaultIconSequence = other.DefaultIconSequence;
			DefaultIconPalette = other.DefaultIconPalette;
			DisabledOverlayImage = other.DisabledOverlayImage;
			DisabledOverlaySequence = other.DisabledOverlaySequence;
			DisabledOverlayPalette = other.DisabledOverlayPalette;

			ClickSound = other.ClickSound;
			ClickDisabledSound = other.ClickDisabledSound;

			icon = other.icon;
			disabledOverlay = other.disabledOverlay;

			TooltipUnit = other.TooltipUnit;
			GetTooltipUnit = () => TooltipUnit;

			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;

			tooltipContainer = Exts.Lazy(() =>
				Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
		}

		public void RefreshIcons()
		{
			actor = GetActor();
			actorInfo = GetActorInfo();
			isDisabled = GetDisabled();
			if ((actor == null || !actor.IsInWorld || actor.IsDead || actor.Disposed) && actorInfo == null)
			{
				currentPalette = NoIconPalette;
				currentPaletteIsPlayerPalette = false;
				icon = new Animation(world, NoIconImage);
				icon.Play(NoIconSequence);
				player = null;
				TooltipUnit = null;
				stats = null;
				return;
			}

			if (actorInfo == null)
			{
				player = actor.Owner;
				var rs = actor.TraitOrDefault<RenderSprites>();
				if (rs == null)
				{
					currentPalette = DefaultIconPalette;
					currentPaletteIsPlayerPalette = false;
					icon = new Animation(world, DefaultIconImage);
					icon.Play(DefaultIconSequence);
					return;
				}

				stats = actor.TraitOrDefault<ActorStatValues>();
				if (!string.IsNullOrEmpty(stats.Icon))
				{
					currentPaletteIsPlayerPalette = stats.IconPaletteIsPlayerPalette;
					currentPalette = currentPaletteIsPlayerPalette ? stats.IconPalette + player.InternalName : stats.IconPalette;
					icon = new Animation(world, stats.DisguiseImage ?? rs.GetImage(actor));
					icon.Play(stats.Icon);
				}
				else
				{
					currentPalette = DefaultIconPalette;
					currentPaletteIsPlayerPalette = false;
					icon = new Animation(world, DefaultIconImage);
					icon.Play(DefaultIconSequence);
				}

				TooltipUnit = new BasicUnit(actor, stats.TooltipActor);
			}
			else
			{
				var rsi = actorInfo.TraitInfoOrDefault<RenderSpritesInfo>();
				var bi = actorInfo.TraitInfos<BuildableInfo>().FirstOrDefault();
				if (rsi == null || bi == null)
				{
					currentPalette = DefaultIconPalette;
					currentPaletteIsPlayerPalette = false;
					icon = new Animation(world, DefaultIconImage);
					icon.Play(DefaultIconSequence);
					return;
				}

				currentPaletteIsPlayerPalette = bi.IconPaletteIsPlayerPalette;
				currentPalette = currentPaletteIsPlayerPalette ? bi.IconPalette + player.InternalName : bi.IconPalette;
				icon = new Animation(world, rsi.GetImage(actorInfo, "default"));
				icon.Play(bi.Icon);

				TooltipUnit = new BasicUnit(null, actorInfo);
			}
		}

		public override void Draw()
		{
			Game.Renderer.EnableAntialiasingFilter();

			if (icon.Image != null)
				WidgetUtils.DrawSpriteCentered(icon.Image, worldRenderer.Palette(currentPalette), IconPos + 0.5f * IconSize.ToFloat2() + RenderBounds.Location, IconScale);

			if (stats != null)
			{
				foreach (var iconOverlay in stats.IconOverlays.Where(io => !io.IsTraitDisabled))
				{
					var palette = iconOverlay.Info.IsPlayerPalette ? iconOverlay.Info.Palette + player.InternalName : iconOverlay.Info.Palette;
					WidgetUtils.DrawSpriteCentered(
						iconOverlay.Sprite, worldRenderer.Palette(palette), IconPos + 0.5f * IconSize.ToFloat2() +
							RenderBounds.Location + iconOverlay.GetOffset(IconSize, IconScale), IconScale);
				}
			}

			if (isDisabled)
				WidgetUtils.DrawSpriteCentered(
					disabledOverlay.Image, worldRenderer.Palette(DisabledOverlayPalette), IconPos + 0.5f * IconSize.ToFloat2() +
						RenderBounds.Location, IconScale);

			Game.Renderer.DisableAntialiasingFilter();
		}

		public override void Tick()
		{
			RefreshIcons();
		}

		public override void MouseEntered()
		{
			if (TooltipContainer != null)
			{
				tooltipContainer.Value.SetTooltip(TooltipTemplate,
					new WidgetArgs() { { "player", world.LocalPlayer }, { "getTooltipUnit", GetTooltipUnit }, { "world", world } });
			}
		}

		public override void MouseExited()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				if (actor == null)
				{
					Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickDisabledSound, null);
				}
				else
				{
					if (mi.Button == MouseButton.Left)
					{
						if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
						{
							foreach (var selected in selection.Actors)
							{
								if (TooltipUnit.ActorInfo.Name != selected.Info.Name)
									selection.Remove(selected);
							}
						}
						else if (mi.Modifiers.HasModifier(Modifiers.Shift))
						{
							selection.Clear();
							selection.Add(actor);
						}
						else
						{
							worldRenderer.Viewport.Center(actor.CenterPosition);
						}

						Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
					}
					else if (mi.Button == MouseButton.Right)
					{
						selection.Remove(actor);
						if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
						{
							foreach (var selected in selection.Actors)
							{
								if (TooltipUnit.ActorInfo.Name == selected.Info.Name)
									selection.Remove(selected);
							}
						}

						Game.Sound.PlayNotification(world.Map.Rules, null, "Sounds", ClickSound, null);
					}
				}
			}

			return true;
		}

		public override Widget Clone() { return new ActorIconWidget(this); }
	}
}
