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
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Modifies the build palette order of this actor for a specific queue or when a prerequisite is granted.")]
	public class BuildPaletteOrderModifierInfo : TraitInfo<BuildPaletteOrderModifier>, IBuildPaletteOrderModifierInfo
	{
		[Desc("Additive modifier to apply.")]
		public readonly int Modifier = 1;

		[Desc("Only apply this order change if owner has these prerequisites.")]
		public readonly string[] Prerequisites = Array.Empty<string>();

		[Desc("Queues that this order will apply.")]
		public readonly HashSet<string> Queue = new();

		int IBuildPaletteOrderModifierInfo.GetBuildPaletteOrderModifier(TechTree techTree, string queue)
		{
			if ((Queue.Count == 0 || Queue.Contains(queue)) && (Prerequisites.Length == 0 || techTree.HasPrerequisites(Prerequisites)))
				return Modifier;

			return 0;
		}
	}

	public class BuildPaletteOrderModifier { }
}
