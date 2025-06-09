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
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SupportPowerTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SupportPowerTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Func<SupportPowersWidget.SupportPowerIcon> getTooltipIcon,
			World world, PlayerResources playerResources)
		{
			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Power.Info != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var costLabel = widget.Get<LabelWidget>("COST");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			var hotkeyFont = Game.Renderer.Fonts[hotkeyLabel.Font];
			var timeFont = Game.Renderer.Fonts[timeLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var costFont = Game.Renderer.Fonts[costLabel.Font];
			var baseHeight = widget.Bounds.Height;
			var timeOffset = timeLabel.Bounds.X;
			var costOffset = costLabel.Bounds.X;

			SupportPowerInstance lastPower = null;
			var lastHotkey = Hotkey.Invalid;
			var lastRemainingSeconds = 0;

			tooltipContainer.BeforeRender = () =>
			{
				var icon = getTooltipIcon();
				if (icon == null || icon.Power == null || icon.Power.Instances.Count == 0)
					return;

				var sp = icon.Power;
				var level = sp.GetLevel();
				if (level == 0)
					return;

				// HACK: This abuses knowledge of the internals of WidgetUtils.FormatTime
				// to efficiently work when the label is going to change, requiring a panel relayout
				var remainingSeconds = (int)Math.Ceiling(sp.RemainingTicks * world.Timestep / 1000f);

				var hotkey = icon.Hotkey?.GetValue() ?? Hotkey.Invalid;
				if (sp == lastPower && hotkey == lastHotkey && lastRemainingSeconds == remainingSeconds)
					return;

				var cost = sp.Info.Cost;
				var costString = FluentProvider.GetMessage(costLabel.Text) + cost.ToString(NumberFormatInfo.CurrentInfo);
				costLabel.GetText = () => costString;
				costLabel.GetColor = () => playerResources.Cash + playerResources.Resources >= cost
					? Color.White : Color.Red;
				costLabel.Visible = cost != 0;
				var costSize = costFont.Measure(costString);

				var nameText = FluentProvider.GetMessage(sp.Info.Names.First(ld => ld.Key == level).Value);
				nameLabel.GetText = () => nameText;
				var nameSize = nameFont.Measure(nameText);

				var descText = FluentProvider.GetMessage(sp.Info.Descriptions.First(ld => ld.Key == level).Value);
				descLabel.GetText = () => descText;
				var descSize = descFont.Measure(descText);

				var timeText = sp.TooltipTimeTextOverride();
				if (timeText == null)
				{
					var remaining = WidgetUtils.FormatTime(sp.RemainingTicks, world.Timestep);
					var total = WidgetUtils.FormatTime(sp.Info.ChargeInterval, world.Timestep);
					timeText = $"{remaining} / {total}";
				}

				timeLabel.GetText = () => timeText;
				var timeSize = timeFont.Measure(timeText);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();
				if (hotkeyLabel.Visible)
				{
					var hotkeyText = $"({hotkey.DisplayString()})";

					hotkeyWidth = hotkeyFont.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.GetText = () => hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var timeWidth = timeSize.X;
				var costWidth = costSize.X;
				var topWidth = nameSize.X + hotkeyWidth + timeWidth + timeOffset;

				if (cost != 0)
					topWidth += costWidth + costOffset;

				widget.Bounds.Width = 2 * nameLabel.Bounds.X + Math.Max(topWidth, descSize.X);
				widget.Bounds.Height = baseHeight + descSize.Y;
				timeLabel.Bounds.X = widget.Bounds.Width - nameLabel.Bounds.X - timeWidth;

				if (cost != 0)
				{
					timeLabel.Bounds.X -= costWidth + costOffset;
					costLabel.Bounds.X = widget.Bounds.Width - nameLabel.Bounds.X - costWidth;
				}

				lastPower = sp;
				lastHotkey = hotkey;
				lastRemainingSeconds = remainingSeconds;
			};

			timeLabel.GetColor = () => getTooltipIcon() != null && !getTooltipIcon().Power.Active
				? Color.Red : Color.White;
		}
	}
}
