#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class GrantConditionOnTileSetInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[FieldLoader.Require]
		[Desc("Tile set names to trigger the condition.")]
		public readonly string[] TileSets = { };

		public object Create(ActorInitializer init) { return new GrantConditionOnTileSet(init, this); }
	}

	public class GrantConditionOnTileSet : INotifyCreated
	{
		readonly GrantConditionOnTileSetInfo info;
		readonly TileSet tileSet;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnTileSet(ActorInitializer init, GrantConditionOnTileSetInfo info)
		{
			this.info = info;
			tileSet = init.World.Map.Rules.TileSet;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();

			if (info.TileSets.Contains(tileSet.Name))
				conditionToken = conditionManager.GrantCondition(self, info.Condition);
		}
	}
}
