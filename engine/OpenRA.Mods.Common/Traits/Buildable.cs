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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class BuildableInfo : TraitInfo<Buildable>
	{
		[Desc("The prerequisite names that must be available before this can be built.",
			"This can be prefixed with ! to invert the prerequisite (disabling production if the prerequisite is available)",
			"and/or ~ to hide the actor from the production palette if the prerequisite is not available.",
			"Prerequisites are granted by actors with the ProvidesPrerequisite trait.")]
		public readonly string[] Prerequisites = [];

		[Desc("Production queue(s) that can produce this.")]
		public readonly HashSet<string> Queue = [];

		[Desc("Override the production structure type (from the Production Produces list) that this unit should be built at.")]
		public readonly string BuildAtProductionType = null;

		[Desc("Disable production when there are more than this many of this actor on the battlefield. Set to 0 to disable.")]
		public readonly int BuildLimit = 0;

		[Desc("Build this many of the actor at once.")]
		public readonly int BuildAmount = 1;

		[Desc("Force a specific faction variant, overriding the faction of the producing actor.")]
		public readonly string ForceFaction = null;

		[Desc("Show a tooltip when hovered over my icon.")]
		public readonly bool ShowTooltip = true;

		[SequenceReference]
		[Desc("Sequence of the actor that contains the icon.")]
		public readonly string Icon = "icon";

		[PaletteReference(nameof(IconPaletteIsPlayerPalette))]
		[Desc("Palette used for the production icon.")]
		public readonly string IconPalette = "chrome";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IconPaletteIsPlayerPalette = false;

		[Desc("Base build time in frames (-1 indicates to use the unit's Value).")]
		public readonly int BuildDuration = -1;

		[Desc("Percentage modifier to apply to the build duration.")]
		public readonly int BuildDurationModifier = 60;

		[Desc("Sort order for the production palette. Smaller numbers are presented earlier.")]
		public readonly int BuildPaletteOrder = 9999;

		[Desc("Place the icon on the slot number of BuildPaletteOwner, even if there are free slots before it.")]
		public readonly bool ForceIconLocation = false;

		[Desc("Text shown in the production tooltip.")]
		[FluentReference(optional: true)]
		public readonly string Description;

		[NotificationReference("Speech")]
		[Desc("Notification played when production is complete.",
			"The filename of the audio is defined per faction in notifications.yaml.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification displayed when production is complete.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string ReadyTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when you can't queue another actor",
			"when the queue length limit is exceeded.",
			"The filename of the audio is defined per faction in notifications.yaml.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string LimitedAudio = null;

		[Desc("Notification displayed when you can't queue another actor",
			"when the queue length limit is exceeded.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string LimitedTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when you can't place a building.",
			"The filename of the audio is defined per faction in notifications.yaml.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string CannotPlaceAudio = null;

		[Desc("Notification displayed when you can't place a building.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string CannotPlaceTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when user clicks on the build palette icon.",
			"The filename of the audio is defined per faction in notifications.yaml.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string QueuedAudio = null;

		[Desc("Notification displayed when user clicks on the build palette icon.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string QueuedTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when player right-clicks on the build palette icon.",
			"The filename of the audio is defined per faction in notifications.yaml.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string OnHoldAudio = null;

		[Desc("Notification displayed when player right-clicks on the build palette icon.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string OnHoldTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when player right-clicks on a build palette icon that is already on hold.",
			"The filename of the audio is defined per faction in notifications.yaml.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string CancelledAudio = null;

		[Desc("Notification displayed when player right-clicks on a build palette icon that is already on hold.",
			"Defaults to what is set for the Queue actor built from.")]
		public readonly string CancelledTextNotification = null;

		public int GetBuildPaletteOrder(ActorInfo ai, ProductionQueue queue)
		{
			var paletteOrder = BuildPaletteOrder;
			if (queue == null)
				return paletteOrder;

			var modifiers = ai.TraitInfos<IBuildPaletteOrderModifierInfo>()
				.Select(t => t.GetBuildPaletteOrderModifier(queue.TechTree, queue.Info.Type));
			foreach (var modifier in modifiers)
				paletteOrder += modifier;

			return paletteOrder;
		}

		public static BuildableInfo GetTraitForQueue(ActorInfo ai, string queue)
		{
			var buildables = ai.TraitInfos<BuildableInfo>();
			if (!string.IsNullOrEmpty(queue))
			{
				foreach (var bi in buildables)
					if (bi.Queue.Contains(queue))
						return bi;

				return null;
			}

			return buildables.FirstOrDefault();
		}

		public static string GetInitialFaction(ActorInfo ai, string defaultFaction)
		{
			return GetTraitForQueue(ai, null)?.ForceFaction ?? defaultFaction;
		}
	}

	public class Buildable { }
}
