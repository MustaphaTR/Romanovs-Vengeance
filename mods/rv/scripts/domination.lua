--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local distance_to_flag = 2048
local check_interval_ticks = 25
local points_per_interval = 1
local target_points = 350

local flags
local neutral
local players = {}
local in_play = false

GetFlagHolder = function(flag)
	local owner = nil
	for j,actor in pairs(Map.ActorsInCircle(flag.CenterPosition, WDist.New(distance_to_flag))) do
		if not actor.Owner.IsNonCombatant and actor.IsAtGroundLevel() and actor ~= flag then
			if owner == nil then
				owner = actor.Owner
			elseif actor.Owner ~= owner then
				owner = nil
				break
			end
		end
	end

	return owner
end

EachInterval = function()
	local buffer = "Flags: "
	local first = true
	for i,flag in pairs(flags) do
		local holder = GetFlagHolder(flag)
		local name
		if holder ~= nil then
			flag.Owner = holder
			name = holder.Name
			players[holder.InternalName].points = players[holder.InternalName].points + points_per_interval
		else
			name = 'Neutral'
			flag.Owner = neutral
		end
		if first then
			first = false
		else
			buffer = buffer .. ' | '
		end
		buffer = buffer .. name
	end

	local winner = nil
	local players_still_in = 0
	buffer = buffer .. "\n\nFirst to " .. target_points
	for i,player in pairs(players) do
		buffer = buffer .. " | " .. player.object.Name .. ": " .. player.points
		if player.alive == true then
			if player.points >= target_points then
				winner = i
			end
			if player.object.HasNoRequiredUnits() then
				player.alive = false
			else
				players_still_in = players_still_in + 1
			end
		end

		DominationText = buffer
		if player.IsLocalPlayer then
			UserInterface.SetMissionText(CommandersPowerText .. DominationText, TextColors[player.InternalName])
		end
	end

	if players_still_in <= 1 then
		for i,player in pairs(players) do
			if player.alive then
				player.object.MarkCompletedObjective(player.objective)
			end
		end
	end

	if winner ~= nil then
		for i,player in pairs(players) do
			if i == winner then
				player.object.MarkCompletedObjective(player.objective)
			else
				player.object.MarkFailedObjective(player.objective)
			end
		end

		in_play = false
	end
end

TickDomination = function()
	if in_play and DateTime.GameTime % check_interval_ticks == 0 then
		EachInterval()
	end
end

WorldLoadedDomination = function()
	neutral = Player.GetPlayer('Neutral')

	if neutral.HasPrerequisites( {"global-domination"} ) then
		flags = neutral.GetActorsByType('cadmfgl')

		if #flags > 0 then
			target_points = target_points * #flags

			for j,player in pairs(Player.GetPlayers(nil)) do
				if not player.IsNonCombatant then
					players[player.InternalName] = {
						object = player,
						points = 0,
						objective = player.AddPrimaryObjective("Get " .. target_points .. " points or destroy all enemy forces."),
						alive = true
					}
				end
			end

			in_play = true
		end
	end
end
