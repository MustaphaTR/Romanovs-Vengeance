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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.AS.UtilityCommands
{
	class ExtractLegacyRulesValues : IUtilityCommand
	{
		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		string IUtilityCommand.Name { get { return "--extract-from-ini"; } }

		IniFile rulesIni;
		string[] tags;

		[Desc("RULES.INI", "Rule tags", "Extract listed tags from a TS/RA2 INI to a YAML format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			rulesIni = new IniFile(File.Open(args[1], FileMode.Open));
			tags = args[2].Split(',');

			var technoTypes = rulesIni.GetSection("BuildingTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Buildings");
			Console.WriteLine();
			ImportValues(technoTypes);

			technoTypes = rulesIni.GetSection("InfantryTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Infantry");
			Console.WriteLine();
			ImportValues(technoTypes);

			technoTypes = rulesIni.GetSection("VehicleTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Vehicles");
			Console.WriteLine();
			ImportValues(technoTypes);

			technoTypes = rulesIni.GetSection("AircraftTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Aircraft");
			Console.WriteLine();
			ImportValues(technoTypes);
		}

		void ImportValues(IEnumerable<string> technoTypes)
		{
			foreach (var technoType in technoTypes)
			{
				var rulesSection = rulesIni.GetSection(technoType, allowFail: true);
				if (rulesSection == null)
					continue;

				Console.WriteLine(rulesSection.Name + ":");

				foreach (var tag in tags)
				{
					var results = rulesSection.Where(x => x.Key.StartsWith(tag, StringComparison.Ordinal));
					foreach (var result in results)
					{
						if (!string.IsNullOrEmpty(result.Key))
							Console.WriteLine("\t" + result.Key + ": " + result.Value);
					}
				}

				Console.WriteLine();
			}
		}
	}
}
