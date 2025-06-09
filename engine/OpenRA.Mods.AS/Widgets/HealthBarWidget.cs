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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.AS.Widgets
{
	public class HealthBarWidget : Widget
	{
		public string Background = "progressbar-bg";
		public string EmptyHealthBar = "progressbar-thumb-empty";
		public string RedHealthBar = "progressbar-thumb-red";
		public string YellowHealthBar = "progressbar-thumb-yellow";
		public string GreenHealthBar = "progressbar-thumb-green";
		public Size BarMargin = new(2, 2);
		public int HealthDivisor = 1;

		public Func<IHealth> GetHealth = () => null;
		IHealth health;

		public Func<float> GetScale = () => 1f;
		float scale;

		LabelWidget label;
		bool labelChecked;

		public HealthBarWidget() { }

		protected HealthBarWidget(HealthBarWidget other)
			: base(other)
		{
			Background = other.Background;
			EmptyHealthBar = other.EmptyHealthBar;
			RedHealthBar = other.RedHealthBar;
			YellowHealthBar = other.YellowHealthBar;
			GreenHealthBar = other.GreenHealthBar;
			BarMargin = other.BarMargin;
			HealthDivisor = other.HealthDivisor;

			GetHealth = other.GetHealth;
			health = other.health;
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			WidgetUtils.DrawPanel(Background, rb);

			var percentage = GetPercentage();
			var bar = GetBar(percentage);

			var minBarWidth = ChromeProvider.GetMinimumPanelSize(bar).Width;
			var maxBarWidth = rb.Width - BarMargin.Width * 2;
			var barWidth = percentage * maxBarWidth / 100;
			barWidth = Math.Max(barWidth, minBarWidth);

			var barRect = new Rectangle(rb.X + BarMargin.Width, rb.Y + BarMargin.Height, barWidth, rb.Height - 2 * BarMargin.Height);
			WidgetUtils.DrawPanel(bar, barRect);
		}

		public override void Tick()
		{
			health = GetHealth();
			scale = GetScale();

			if (!labelChecked)
			{
				label = GetOrNull<LabelWidget>("HEALTH_LABEL");
				if (label != null)
					label.GetText = () => GetText();

				labelChecked = true;
			}
		}

		string GetBar(int percentage)
		{
			var bar = EmptyHealthBar;
			if (health != null)
			{
				if (percentage <= 25)
					return RedHealthBar;
				else if (percentage <= 50)
					return YellowHealthBar;
				else
					return GreenHealthBar;
			}

			return bar;
		}

		int GetPercentage()
		{
			if (health == null)
				return 0;

			var healthValue = health.HP;
			var maxHealthValue = health.MaxHP;
			return 100 - (int)((float)(maxHealthValue - healthValue) / maxHealthValue * 100);
		}

		string GetText()
		{
			if (health == null)
				return "";

			var healthValue = Math.Round(health.HP * scale / HealthDivisor, 0);
			var maxHealthValue = Math.Round(health.MaxHP * scale / HealthDivisor, 0);
			return healthValue.ToString(NumberFormatInfo.CurrentInfo) + " / " + maxHealthValue.ToString(NumberFormatInfo.CurrentInfo);
		}

		public override Widget Clone() { return new HealthBarWidget(this); }
	}
}
