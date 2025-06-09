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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.AS.Widgets.Logic
{
	public class ProductionTooltipLogicCA : ChromeLogic
	{
		[FluentReference("prerequisites")]
		const string Requires = "label-requires";

		[ObjectCreator.UseCtor]
		public ProductionTooltipLogicCA(Widget widget, TooltipContainerWidget tooltipContainer, Player player, Func<ProductionIcon> getTooltipIcon)
		{
			var world = player.World;
			var mapRules = world.Map.Rules;
			var pm = player.PlayerActor.TraitOrDefault<PowerManager>();
			var pr = player.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Actor != null &&
				BuildableInfo.GetTraitForQueue(getTooltipIcon().Actor, getTooltipIcon().ProductionQueue?.Info.Type).ShowTooltip;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var powerLabel = widget.Get<LabelWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");
			var armorTypeLabel = widget.Get<LabelWidget>("ARMORTYPE");
			var armorTypeIcon = widget.Get<ImageWidget>("ARMORTYPE_ICON");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var timeIcon = widget.Get<ImageWidget>("TIME_ICON");
			var costLabel = widget.Get<LabelWidget>("COST");
			var costIcon = widget.Get<ImageWidget>("COST_ICON");
			var descLabel = widget.Get<LabelWidget>("DESC");
			/* var strengthsLabel = widget.Get<LabelWidget>("STRENGTHS");
			var weaknessesLabel = widget.Get<LabelWidget>("WEAKNESSES");
			var attributesLabel = widget.Get<LabelWidget>("ATTRIBUTES"); */

			var iconMargin = timeIcon.Bounds.X;

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));

			ActorInfo lastActor = null;
			var lastHotkey = Hotkey.Invalid;
			var lastPowerState = pm == null ? PowerState.Normal : pm.PowerState;
			var descLabelY = descLabel.Bounds.Y;
			var descLabelPadding = descLabel.Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();
				var actor = tooltipIcon?.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey?.GetValue() ?? Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey && (pm == null || pm.PowerState == lastPowerState))
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				var name = tooltip != null ? FluentProvider.GetMessage(tooltip.Name) : actor.Name;
				var buildable = BuildableInfo.GetTraitForQueue(actor, tooltipIcon.ProductionQueue?.Info.Type);

				var cost = 0;
				if (tooltipIcon.ProductionQueue != null)
					cost = tooltipIcon.ProductionQueue.GetProductionCost(actor);
				else
				{
					var valued = actor.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						cost = valued.Cost;
				}

				nameLabel.Text = name;

				var nameSize = font.Measure(name);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				armorTypeLabel = GetArmorTypeLabel(armorTypeLabel, actor);
				/* var tooltipExtras = actor.TraitInfos<TooltipExtrasInfo>().FirstOrDefault(info => info.IsStandard);

				if (tooltipExtras != null)
				{
					strengthsLabel.Text = tooltipExtras.Strengths.Replace("\\n", "\n");
					weaknessesLabel.Text = tooltipExtras.Weaknesses.Replace("\\n", "\n");
					attributesLabel.Text = tooltipExtras.Attributes.Replace("\\n", "\n");
				}
				else
				{
					strengthsLabel.Text = "";
					weaknessesLabel.Text = "";
					attributesLabel.Text = "";
				} */

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = $"({hotkey.DisplayString()})";

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.Text = hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites.Select(a => ActorName(mapRules, a))
					.Where(s => !s.StartsWith('~') && !s.StartsWith('!')).Select(s => FluentProvider.GetMessage(s)).ToList();

				var requiresSize = int2.Zero;
				if (prereqs.Count > 0)
				{
					requiresLabel.Text = FluentProvider.GetMessage(Requires, "prerequisites", prereqs.JoinWith(", "));
					requiresSize = requiresFont.Measure(requiresLabel.Text);
					requiresLabel.Visible = true;
					descLabel.Bounds.Y = descLabelY + requiresLabel.Bounds.Height + descLabel.Bounds.X / 2;
				}
				else
				{
					requiresLabel.Visible = false;
					descLabel.Bounds.Y = descLabelY;
				}

				var buildTime = tooltipIcon.ProductionQueue == null ? 0 : tooltipIcon.ProductionQueue.GetBuildTime(actor, buildable);
				var timeModifier = pm != null && pm.PowerState != PowerState.Normal ? tooltipIcon.ProductionQueue.Info.LowPowerModifier : 100;

				timeLabel.Text = formatBuildTime.Update(buildTime * timeModifier / 100);
				timeLabel.TextColor =
					(pm != null && pm.PowerState != PowerState.Normal && tooltipIcon.ProductionQueue.Info.LowPowerModifier > 100)
						? Color.Red
						: Color.White;
				var timeSize = font.Measure(timeLabel.Text);
				costLabel.IsVisible = () => cost != 0;
				costIcon.IsVisible = () => cost != 0;

				costLabel.Text = cost.ToString(NumberFormatInfo.CurrentInfo);
				costLabel.GetColor = () => pr.Cash + pr.Resources >= cost ? Color.White : Color.Red;
				var costSize = font.Measure(costLabel.Text);

				var powerSize = new int2(0, 0);
				var power = 0;
				var armorTypeSize = armorTypeLabel.Text != "" ? font.Measure(armorTypeLabel.Text) : new int2(0, 0);
				armorTypeIcon.Visible = armorTypeSize.Y > 0;

				if (pm != null)
				{
					power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
					powerLabel.Text = power.ToString(NumberFormatInfo.CurrentInfo);
					powerLabel.GetColor = () => (pm.PowerProvided - pm.PowerDrained >= -power || power > 0)
						? Color.White : Color.Red;
					powerLabel.Visible = power != 0;
					powerIcon.Visible = power != 0;
					powerSize = font.Measure(powerLabel.Text);
				}

				if (armorTypeLabel.Text != "" && power != 0)
					armorTypeIcon.Bounds.Y = armorTypeLabel.Bounds.Y = powerLabel.Bounds.Bottom;
				else
					armorTypeIcon.Bounds.Y = armorTypeLabel.Bounds.Y = timeLabel.Bounds.Bottom;

				var extrasSpacing = descLabel.Bounds.X / 2;

				var desc = string.IsNullOrEmpty(buildable.Description) ? "" : FluentProvider.GetMessage(buildable.Description);
				descLabel.Text = desc;
				var descSize = descFont.Measure(descLabel.Text);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y;

				/* var strengthsSize = strengthsLabel.Text != "" ? descFont.Measure(strengthsLabel.Text) : new int2(0, 0);
				var weaknessesSize = weaknessesLabel.Text != "" ? descFont.Measure(weaknessesLabel.Text) : new int2(0, 0);
				var attributesSize = attributesLabel.Text != "" ? descFont.Measure(attributesLabel.Text) : new int2(0, 0);

				strengthsLabel.Bounds.Y = descLabel.Bounds.Bottom + extrasSpacing;
				weaknessesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + extrasSpacing;
				attributesLabel.Bounds.Y = descLabel.Bounds.Bottom + strengthsSize.Y + weaknessesSize.Y + extrasSpacing;

				descLabel.Bounds.Height += strengthsSize.Y + weaknessesSize.Y + attributesSize.Y + descLabelPadding + extrasSpacing;

				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X, strengthsSize.X, weaknessesSize.X, attributesSize.X }.Aggregate(Math.Max); */
				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X }.Aggregate(Math.Max);
				var rightWidth = new[] { powerSize.X, timeSize.X, costSize.X, armorTypeSize.X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = powerIcon.Bounds.X = costIcon.Bounds.X = armorTypeIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = armorTypeLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				// Set the bottom margin to match the left margin
				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

				// Set the bottom margin to match the top margin
				var rightHeight = armorTypeIcon.Bounds.Bottom + costIcon.Bounds.Top;

				widget.Bounds.Height = Math.Max(leftHeight, rightHeight);

				lastActor = actor;
				lastHotkey = hotkey;
				if (pm != null)
					lastPowerState = pm.PowerState;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out var ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return actorTooltip.Name;
			}

			return a;
		}

		static LabelWidget GetArmorTypeLabel(LabelWidget armorTypeLabel, ActorInfo actor)
		{
			var armor = actor.TraitInfos<ArmorInfo>().FirstOrDefault();
			armorTypeLabel.Text = armor != null ? armor.Type : "";

			/* if (armorTypeLabel.Text != "" && actor.HasTraitInfo<AircraftInfo>())
				armorTypeLabel.Text = "Aircraft";

			// hard coded, specific to CA - find a better way to set user-friendly names and colors for armor types
			switch (armorTypeLabel.Text)
			{
				case "None":
					armorTypeLabel.Text = "Infantry";
					armorTypeLabel.TextColor = Color.ForestGreen;
					break;

				case "Light":
					armorTypeLabel.TextColor = Color.MediumPurple;
					break;

				case "Heavy":
					armorTypeLabel.TextColor = Color.Firebrick;
					break;

				case "Concrete":
					armorTypeLabel.Text = "Defense";
					armorTypeLabel.TextColor = Color.RoyalBlue;
					break;

				case "Wood":
					armorTypeLabel.Text = "Building";
					armorTypeLabel.TextColor = Color.Peru;
					break;

				case "Brick":
					armorTypeLabel.Text = "Wall";
					armorTypeLabel.TextColor = Color.RosyBrown;
					break;

				case "Aircraft":
					armorTypeLabel.TextColor = Color.SkyBlue;
					break;

				default:
					armorTypeLabel.Text = "";
					break;
			} */

			return armorTypeLabel;
		}
	}
}
