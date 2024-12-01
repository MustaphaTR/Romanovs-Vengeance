--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

CommandersPowerText = ""
DominationText = ""
KotHText = ""

CPModifier = Map.LobbyOption("cpmodifier")

Tick = function()
	if CPModifier ~= "disabled" then
		TickCommandersPowers()
	end

	TickDomination()
	TickKotH()
	TickRegicide()
end

WorldLoaded = function()
	WorldLoadedCommandersPowers()
	WorldLoadedDomination()
	WorldLoadedKotH()
	WorldLoadedRegicide()
end
