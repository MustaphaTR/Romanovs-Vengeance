#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.AS.Widgets.Logic
{
	public enum ASCreditsState
	{
		Engine,
		AS,
		Mod
	}

	public class ASCreditsLogic : ChromeLogic
	{
		readonly ScrollPanelWidget scrollPanel;
		readonly LabelWidget template;

		readonly string modLines;
		readonly string engineLines;
		readonly string asEngineLines;
		ASCreditsState tabState;

		[ObjectCreator.UseCtor]
		public ASCreditsLogic(Widget widget, ModData modData, Action onExit)
		{
			var panel = widget.Get("CREDITS_PANEL");

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};

			engineLines = ParseLines(File.OpenRead(Platform.ResolvePath("./AUTHORS")));
			asEngineLines = ParseLines(File.OpenRead(Platform.ResolvePath("./AUTHORS.AS")));

			var tabContainer = panel.Get("TAB_CONTAINER");
			var modTab = tabContainer.Get<ButtonWidget>("MOD_TAB");
			modTab.IsHighlighted = () => tabState == ASCreditsState.Mod;
			modTab.OnClick = () => ShowCredits(ASCreditsState.Mod);

			var engineTab = tabContainer.Get<ButtonWidget>("ENGINE_TAB");
			engineTab.IsHighlighted = () => tabState == ASCreditsState.Engine;
			engineTab.OnClick = () => ShowCredits(ASCreditsState.Engine);

			var asTab = tabContainer.Get<ButtonWidget>("ASENGINE_TAB");
			asTab.IsHighlighted = () => tabState == ASCreditsState.AS;
			asTab.OnClick = () => ShowCredits(ASCreditsState.AS);

			scrollPanel = panel.Get<ScrollPanelWidget>("CREDITS_DISPLAY");
			template = scrollPanel.Get<LabelWidget>("CREDITS_TEMPLATE");

			// Make space to show the tabs
			tabContainer.IsVisible = () => true;
			scrollPanel.Bounds.Y += tabContainer.Bounds.Height;
			scrollPanel.Bounds.Height -= tabContainer.Bounds.Height;

			var hasModCredits = modData.Manifest.Contains<ModCredits>();
			if (hasModCredits)
			{
				var modCredits = modData.Manifest.Get<ModCredits>();
				modLines = ParseLines(modData.DefaultFileSystem.Open(modCredits.ModCreditsFile));
				modTab.GetText = () => modCredits.ModTabTitle;
			}

			if (hasModCredits)
				ShowCredits(ASCreditsState.Mod);
			else
				ShowCredits(ASCreditsState.AS);
		}

		void ShowCredits(ASCreditsState credits)
		{
			tabState = credits;

			scrollPanel.RemoveChildren();
			var font = Game.Renderer.Fonts[template.Font];

			string lines;
			if (credits == ASCreditsState.Engine)
				lines = engineLines;
			else if (credits == ASCreditsState.AS)
				lines = asEngineLines;
			else
				lines = modLines;

			var label = (LabelWidget)template.Clone();
			label.GetText = () => lines;
			label.IncreaseHeightToFitCurrentText();
			scrollPanel.AddChild(label);
		}

		static string ParseLines(Stream file)
		{
			return file.ReadAllText().Replace(Environment.NewLine, "\n").Replace("\t", "    ").Replace("*", "\u2022");
		}
	}
}
