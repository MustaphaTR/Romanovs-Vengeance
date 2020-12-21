--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local target_time = 250
local timer = target_time

local beacon
local beacon_owner
local neutral
local players = {}
local in_play = false

EachKotHInterval = function()
	local buffer = "\n\nPsychic Beacon is offline."

	if beacon_owner ~= beacon.Owner then
		timer = target_time

		beacon_owner = beacon.Owner
	end

	if beacon_owner ~= neutral then
		timer = timer - 1

		buffer = "\n\nPsychic Beacon is held by: " .. beacon_owner.Name .. "\nIt'll activate in: " .. Utils.FormatTime(timer)
	end

	for i,player in pairs(players) do
		KotHText = buffer
		if player.IsLocalPlayer then
			UserInterface.SetMissionText(CommandersPowerText .. DominationText .. KotHText, TextColors[player.InternalName])
		end
	end

	if timer <= 0 then
		for i,player in pairs(players) do
			if player.object == beacon_owner then
				player.object.MarkCompletedObjective(player.objective)
			else
				player.object.MarkFailedObjective(player.objective)
			end
		end

		in_play = false
	end
end

TickKotH = function()
	if in_play then
		EachKotHInterval()
	end
end

WorldLoadedKotH = function()
	neutral = Player.GetPlayer('Neutral')
	beacon_owner = neutral

	if neutral.HasPrerequisites( {"global-koth"} ) then
		local beacons = neutral.GetActorsByType('capsyb.koth')

		if #beacons > 0 then
			beacon = beacons[1]

			for j,player in pairs(Player.GetPlayers(nil)) do
				if not player.IsNonCombatant then
					players[player.InternalName] = {
						object = player,
						objective = player.AddPrimaryObjective("Hold the Psychic Beacon for " .. Utils.FormatTime(target_time) .. " or destroy all enemy forces.")
					}
				end
			end

			in_play = true
		end
	end
end
