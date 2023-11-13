--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

local players = { }
mcvs = { "amcv", "smcv", "pcv", "tany", "boris", "yurix", "vlkv" }

LeaderTypes =
{
	america = "pres.regicide",
	england = "pres.regicide",
	france = "pres.regicide",
	germany = "pres.regicide",
	korea = "pres.regicide",
	japan = "pres.regicide",
	belarus = "pres.regicide",
	poland = "pres.regicide",
	ukraine = "pres.regicide",
	aussie = "pres.regicide",
	china = "pres.regicide",
	turkey = "pres.regicide",
	canada = "pres.regicide",
	russia = "rmnv.regicide",
	iraq = "rmnv.regicide",
	vietnam = "rmnv.regicide",
	cuba = "rmnv.regicide",
	libya = "rmnv.regicide",
	chile = "rmnv.regicide",
	mexico = "rmnv.regicide",
	mongolia = "rmnv.regicide",
	psicorps = "yuripr.regicide",
	psinepal = "yuripr.regicide",
	psitrans = "yuripr.regicide",
	psisouth = "yuripr.regicide",
	psimoon = "yuripr.regicide",
	transcaucus = "myak.regicide",
	turkmen = "myak.regicide",
	tuva = "myak.regicide",
	russianfed = "myak.regicide",
	serbia = "myak.regicide"
}

TickRegicide = function()
	if neutral.HasPrerequisites( {"global-regicide"} ) then
		local teams_still_in = { }
		local teams_still_in_count = 0
		for i,player in pairs(players) do
			if player.alive == true then
				if player.object.HasNoRequiredUnits() then
					player.alive = false
				else
					if player.object.Team == 0 then
						teams_still_in_count = teams_still_in_count + 1
					elseif not Utils.Any(teams_still_in, function(team) return team == player.object.Team end) then
						teams_still_in[#teams_still_in + 1] = player.object.Team

						teams_still_in_count = teams_still_in_count + 1
					end
				end
			end
		end

		if teams_still_in_count <= 1 then
			for i,player in pairs(players) do
				if player.alive then
					player.object.MarkCompletedObjective(player.objective)
				end
			end
		end
	end
end

WorldLoadedRegicide = function()
	neutral = Player.GetPlayer('Neutral')

	if neutral.HasPrerequisites( {"global-regicide"} ) then
		for _,player in pairs(Player.GetPlayers(function(p) return not p.IsNonCombatant end)) do
			players[player.InternalName] = {
				object = player,
				leader = Actor.Create(LeaderTypes[player.Faction], true, { Owner = player, Facing = Angle.SouthEast, Location = player.GetActorsByTypes(mcvs)[1].Location + CVec.New(3,3) }),
				objective = player.AddPrimaryObjective("Keep your leader alive, while killing enemy ones."),
				alive = true
			}

			Trigger.OnKilled(players[player.InternalName].leader, function()
				player.MarkFailedObjective(players[player.InternalName].objective)
			end)
		end
	end
end
