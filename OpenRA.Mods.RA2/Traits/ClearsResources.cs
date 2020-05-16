#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Removes all the resources from the map when enabled.")]
	public class ClearsResourcesInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new ClearsResources(this, init.Self); }
	}

	public class ClearsResources : ConditionalTrait<ClearsResourcesInfo>
	{
		ResourceLayer resLayer;
		ResourceRenderer resRenderer;
		PPos[] allCells;

		public ClearsResources(ClearsResourcesInfo info, Actor self)
			: base(info)
		{
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			resRenderer = self.World.WorldActor.Trait<ResourceRenderer>();
			allCells = self.World.Map.ProjectedCellBounds.ToArray();
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var cell in allCells)
			{
				var pos = ((MPos)cell).ToCPos(self.World.Map);
				resLayer.Destroy(pos);
				resRenderer.UpdateRenderedSprite(pos, RendererCellContents.Empty);
			}
		}
	}
}
