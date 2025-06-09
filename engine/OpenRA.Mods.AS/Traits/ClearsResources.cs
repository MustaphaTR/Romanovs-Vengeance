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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Removes all the resources from the map when enabled.")]
	public class ClearsResourcesInfo : ConditionalTraitInfo
	{
		[Desc("Resource types to remove with this trait.", "If empty, all resource types will be removed.")]
		public readonly HashSet<string> ResourceTypes = new();

		public override object Create(ActorInitializer init) { return new ClearsResources(this, init.Self); }
	}

	public class ClearsResources : ConditionalTrait<ClearsResourcesInfo>
	{
		readonly IResourceLayer resourceLayer;
		readonly ResourceRenderer resourceRenderer;
		readonly PPos[] allCells;

		public ClearsResources(ClearsResourcesInfo info, Actor self)
			: base(info)
		{
			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();
			resourceRenderer = self.World.WorldActor.Trait<ResourceRenderer>();
			allCells = self.World.Map.ProjectedCells.ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			var removeAllTypes = Info.ResourceTypes.Count == 0;

			foreach (var cell in allCells)
			{
				var pos = ((MPos)cell).ToCPos(self.World.Map);
				var cellContents = resourceLayer.GetResource(pos);

				if (removeAllTypes || Info.ResourceTypes.Contains(cellContents.Type))
				{
					resourceLayer.ClearResources(pos);
					resourceRenderer.UpdateRenderedSprite(pos, RendererCellContents.Empty);
				}
			}
		}
	}
}
