#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.AS.Widgets.Logic
{
	class CollapsableWidgetLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public CollapsableWidgetLogic(Widget widget)
		{
			var closeButton = widget.Get<ButtonWidget>("CLOSE_BUTTON");
			var openButton = widget.Get<ButtonWidget>("OPEN_BUTTON");
			var closedBackground = widget.GetOrNull<BackgroundWidget>("CLOSED_BACKGROUND");
			var openedBackground = widget.Get<BackgroundWidget>("OPENED_BACKGROUND");

			closeButton.OnClick = () =>
			{
				openButton.Visible = true;
				openedBackground.Visible = false;
				closeButton.Visible = false;
				if (closedBackground != null)
					closedBackground.Visible = true;
			};

			openButton.OnClick = () =>
			{
				openButton.Visible = false;
				openedBackground.Visible = true;
				closeButton.Visible = true;
				if (closedBackground != null)
					closedBackground.Visible = false;
			};
		}
	}
}
