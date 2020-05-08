--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Seconds = 0
CPModifier = Map.LobbyOption("cpmodifier")

if CPModifier == "one" then
	PointsPerRank = { 1, 1, 1, 1, 1 }
elseif CPModifier == "normal" then
	PointsPerRank = { 1, 1, 1, 1, 3 }
elseif CPModifier == "two" then
	PointsPerRank = { 2, 2, 2, 2, 2 }
elseif CPModifier == "double" then
	PointsPerRank = { 2, 2, 2, 2, 6 }
elseif CPModifier == "three" then
	PointsPerRank = { 3, 3, 3, 3, 3 }
elseif CPModifier == "triple" then
	PointsPerRank = { 3, 3, 3, 3, 9 }
else
	PointsPerRank = { 5, 0, 15, 0, 5 }
end

PointActorExists = { }
Points = { }
HasPointsActors =  { }
Levels = { }
TextColors ={ }

Ranks =
{
	america = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	england = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	france = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	germany = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	korea = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	japan = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	belarus = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	poland = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	ukraine = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	aussie = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	china = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	turkey = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	canada = { "Lieutenant", "Captain", "Major", "Colonel", "General" },
	russia = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	iraq = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	vietnam = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	cuba = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	libya = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	chile = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	mexico = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	psicorps = { "Consultant", "Adept", "Overseer", "Elite", "Mastermind" },
	psinepal = { "Consultant", "Adept", "Overseer", "Elite", "Mastermind" },
	psitrans = { "Consultant", "Adept", "Overseer", "Elite", "Mastermind" },
	psisouth = { "Consultant", "Adept", "Overseer", "Elite", "Mastermind" },
	psimoon = { "Consultant", "Adept", "Overseer", "Elite", "Mastermind" },
	transcaucus = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	turkmen = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	tuva = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	russianfed = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" },
	serbia = { "Leytenant", "Kapitan", "Mayor", "Polkovnik", "General" }
}
RankXPs = { 0, 800, 1500, 2500, 5000 }

ReducePoints = function(player)
	Trigger.OnProduction(player.GetActorsByType("player")[1], function()
		if Points[player.InternalName] > 0 then
			Points[player.InternalName] = Points[player.InternalName] - 1
		end
	end)
end

TickGeneralsPowers = function()
	localPlayerIsNull = true;
	for _,player in pairs(players) do
		if player.IsLocalPlayer then
			localPlayerIsNull = false;
			if Levels[player.InternalName] < 4 then
				UserInterface.SetMissionText("Current Rank: " .. Ranks[player.Faction][Levels[player.InternalName] + 1] .. "\nCommander's Points: " .. Points[player.InternalName] .. "\nProgress to Next Rank: " .. player.Experience - RankXPs[Levels[player.InternalName] + 1] .. "/" .. RankXPs[Levels[player.InternalName] + 2] - RankXPs[Levels[player.InternalName] + 1] .. "", TextColors[player.InternalName])
			else 
				UserInterface.SetMissionText("Current Rank: " .. Ranks[player.Faction][Levels[player.InternalName] + 1] .. "\nCommander's Points: " .. Points[player.InternalName] .. "", TextColors[player.InternalName])
			end
		end

		if localPlayerIsNull then
			UserInterface.SetMissionText("", player.Color)
		end

		if Points[player.InternalName] > 0 and not PointActorExists[player.InternalName] then
			HasPointsActors[player.InternalName] = Actor.Create("hack.has_points", true, { Owner = player })

			PointActorExists[player.InternalName] = true
		end

		if not (Points[player.InternalName] > 0) and PointActorExists[player.InternalName] and HasPointsActors[player.InternalName] ~= nil then
			HasPointsActors[player.InternalName].Destroy()

			PointActorExists[player.InternalName] = false
		end

		if player.Experience >= RankXPs[2] and not (Levels[player.InternalName] > 0) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[2]

			Media.PlaySpeechNotification(player, "YouHavePromoted")
		end

		if player.Experience >= RankXPs[3] and not (Levels[player.InternalName] > 1) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[3]

			Media.PlaySpeechNotification(player, "YouHavePromoted")
			Actor.Create("hack.rank_3", true, { Owner = player })
		end

		if player.Experience >= RankXPs[4] and not (Levels[player.InternalName] > 2) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[4]

			Media.PlaySpeechNotification(player, "YouHavePromoted")
		end

		if player.Experience >= RankXPs[5] and not (Levels[player.InternalName] > 3) then
			Levels[player.InternalName] = Levels[player.InternalName] + 1
			Points[player.InternalName] = Points[player.InternalName] + PointsPerRank[5]

			Media.PlaySpeechNotification(player, "YouHavePromoted")
			Actor.Create("hack.rank_5", true, { Owner = player })
		end
	end
end

Second = function()
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Seconds = Seconds + 1

		for _,player in pairs(players) do
			if Points[player.InternalName] > 0 and Seconds % 2 == 0 then
				TextColors[player.InternalName] = HSLColor.White
			else
				TextColors[player.InternalName] = player.Color
			end
		end

		Second()
	end)
end

SetUpDefaults = function()
	for _,player in pairs(players) do
		PointActorExists[player.InternalName] = false
		Points[player.InternalName] = PointsPerRank[1]
		HasPointsActors[player.InternalName] = nil
		Levels[player.InternalName] = 0
		TextColors[player.InternalName] = HSLColor.White
	end
end

Tick = function()
	if GPModifier ~= "disabled" then
		TickGeneralsPowers()
	end
end

WorldLoaded = function()
	players = Player.GetPlayers(function(p) return not p.IsNonCombatant end)
	SetUpDefaults()

	if CPModifier ~= "disabled" then
		for _,player in pairs(players) do
			ReducePoints(player)
		end

		Second()
	end
end
