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
using System.Linq;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("A map generator that clears a map.")]
	public sealed class ClearMapGeneratorInfo : TraitInfo<ClearMapGenerator>, IEditorMapGeneratorInfo, IEditorToolInfo
	{
		[FieldLoader.Require]
		[Desc("Human-readable name this generator uses.")]
		[FluentReference]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Internal id for this map generator.")]
		public readonly string Type = null;

		[FluentReference]
		[Desc("The title to use for generated maps.")]
		public readonly string MapTitle = "label-random-map";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MAP_GENERATOR_TOOL_PANEL";

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(FluentReferencesLoader))]
		[FluentReference]
		public readonly List<string> FluentReferences = null;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;
		string IMapGeneratorInfo.MapTitle => MapTitle;

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		static List<string> FluentReferencesLoader(MiniYaml my)
		{
			return new MapGeneratorSettings(null, my.NodeWithKey("Settings").Value)
				.Options.SelectMany(o => o.GetFluentReferences()).ToList();
		}

		public IMapGeneratorSettings GetSettings()
		{
			return new MapGeneratorSettings(this, Settings);
		}

		public Map Generate(ModData modData, MapGenerationArgs args)
		{
			var random = new MersenneTwister();
			var terrainInfo = modData.DefaultTerrainInfo[args.Tileset];

			var map = new Map(modData, terrainInfo, args.Size);
			var maxTerrainHeight = map.Grid.MaximumTerrainHeight;
			var tl = new PPos(1, 1 + maxTerrainHeight);
			var br = new PPos(args.Size.Width - 1, args.Size.Height + maxTerrainHeight - 1);
			map.SetBounds(tl, br);

			if (!Exts.TryParseUshortInvariant(args.Settings.NodeWithKey("Tile").Value.Value, out var tileType))
				throw new YamlException("Illegal tile type");

			var tile = new TerrainTile(tileType, 0);
			if (!terrainInfo.TryGetTerrainInfo(tile, out var _))
				throw new MapGenerationException("Illegal tile type");

			// If the default terrain tile is part of a PickAny template, pick
			// a random tile index. Otherwise, just use the default tile.
			Func<TerrainTile> tilePicker;
			if (map.Rules.TerrainInfo is ITemplatedTerrainInfo templatedTerrainInfo &&
				templatedTerrainInfo.Templates.TryGetValue(tileType, out var template) &&
				template.PickAny)
			{
				tilePicker = () => new TerrainTile(tileType, (byte)random.Next(0, template.TilesCount));
			}
			else
			{
				tilePicker = () => tile;
			}

			foreach (var cell in map.AllCells)
			{
				var mpos = cell.ToMPos(map);
				map.Tiles[mpos] = tilePicker();
				map.Resources[mpos] = new ResourceTile(0, 0);
				map.Height[mpos] = 0;
			}

			map.PlayerDefinitions = new MapPlayers(map.Rules, 0).ToMiniYaml();
			map.ActorDefinitions = [];

			return map;
		}

		string IEditorToolInfo.Label => Name;
		string IEditorToolInfo.PanelWidget => PanelWidget;
	}

	public class ClearMapGenerator { /* we're only interested in the Info */ }
}
