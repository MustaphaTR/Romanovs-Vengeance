--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

VeteranUnits = { }
EliteUnits = {  }

IFVPath = { RoadAWP1.Location, RoadAWP2.Location, RoadAWP3.Location, RoadAWP4.Location, RoadAWP5.Location, RoadAWP6.Location, RoadAWP7.Location, RoadAWP8.Location, RoadAWP9.Location, RoadAWP10.Location, RoadAWP11.Location, RoadAWP12.Location, RoadAWP13.Location, RoadAWP14.Location, RoadAWP15.Location, RoadAWP16.Location, RoadAWP17.Location }

LazarusPatrol1 = { Lazarus1WP1.Location, Lazarus1WP2.Location }
LazarusPatrol2 = { Lazarus2WP1.Location, Lazarus2WP2.Location }
GatlingPatrol1 = { Gatling1WP1.Location, Gatling1WP2.Location }

CivilianTypes = { "civ1", "civ2", "civ3", "civa", "civb", "civc", "civsfm", "civsf", "civstm" }
CivilianHousesEast = { EastHouse1, EastHouse2, EastHouse3, EastHouse4, EastHouse5, EastHouse6, EastHouse7, EastHouse8 }
CivilianHousesWest = { WestHouse1, WestHouse2, WestHouse3, WestHouse4, WestHouse5 }

IFVTypes = { "fv.flamer", "fv.virus", "fv.init", "fv.hijacker", "fv.ivan", "fv.tesla", "fv.iron", "fv.yuri", "fv.gatling" }

ProduceCivilians = function(house)
	local delay = Utils.RandomInteger(0, 200)
	
	Trigger.AfterDelay(delay, function()
		house.Produce(Utils.Random(CivilianTypes))
	end)
end

DeployGIs = function()
	local gis = allies.GetActorsByType("e1")
	local ggis = allies.GetActorsByType("ggi")
	Utils.Do(gis, function(gi)
		gi.GrantCondition("deployed")
	end)
	Utils.Do(ggis, function(ggi)
		ggi.GrantCondition("deployed")
	end)
end

GiveVeterancy = function()
	Utils.Do(VeteranUnits, function(VeteranUnit)
		VeteranUnit.GrantCondition("rank-veteran")
	end)

	Utils.Do(EliteUnits, function(EliteUnit)
		EliteUnit.GrantCondition("rank-veteran")
		EliteUnit.GrantCondition("rank-veteran")
		EliteUnit.GrantCondition("rank-veteran")
	end)
end

PatrolA = function(unit, waypoints, delay)
	unit.Move(waypoints[1])
	Trigger.AfterDelay(delay, function()
		PatrolB(unit, waypoints, delay)
	end)
end

PatrolB = function(unit, waypoints, delay)
	unit.Move(waypoints[2])
	Trigger.AfterDelay(delay, function()
		PatrolA(unit, waypoints, delay)
	end)
end

SendActor = function(units, waypoints, owner, delay)
	local unit = Utils.Random(units)
	local actor = Actor.Create(unit, true, { Owner = owner, Location = waypoints[1] })
	for _,waypoint in pairs(waypoints) do 
		actor.Move(waypoint)
	end
	actor.Destroy()

	Trigger.AfterDelay(delay, function()
		SendActor(units, waypoints, owner, delay)
	end)
end

SendAirstrike = function(delay)
	AirstrikeProxy.SendAirstrike(AirstrikeTarget.CenterPosition, false, 192)

	Trigger.AfterDelay(delay, function()
		SendAirstrike(delay)
	end)
end

ticks = 0
speed = 6

Tick = function()
	ticks = ticks + 1

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(30960 * math.sin(t), 51520 * math.cos(t), 0)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")
	psicorps = Player.GetPlayer("PsiCorps")
	viewportOrigin = Camera.Position

	DeployGIs();
	GiveVeterancy();

	local lazarus1 = Actor.Create("yhtnk", true, { Owner = psicorps, Location = LazarusPatrol1[1] })
	local lazarus2 = Actor.Create("yhtnk", true, { Owner = psicorps, Location = LazarusPatrol2[1] })
	PatrolA(lazarus1, LazarusPatrol1, DateTime.Seconds(7))
	PatrolB(lazarus2, LazarusPatrol2, DateTime.Seconds(6))

	local gatling1 = Actor.Create("ytnk", true, { Owner = psicorps, Location = GatlingPatrol1[1] })
	PatrolA(gatling1, GatlingPatrol1, DateTime.Seconds(10))

	Utils.Do(CivilianHousesEast, function(house)
		ProduceCivilians(house)

		Trigger.OnProduction(house, function(a, civ)
			civ.DeliverCash(grinder)

			Trigger.AfterDelay(400, function()
				ProduceCivilians(house, PGrinderEast)
			end)
		end)
	end)

	Utils.Do(CivilianHousesWest, function(house)
		ProduceCivilians(house)

		Trigger.OnProduction(house, function(a, civ)
			civ.DeliverCash(PGrinderWest)

			Trigger.AfterDelay(400, function()
				ProduceCivilians(house, grinder)
			end)
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(6), function()
		SendActor(IFVTypes, IFVPath, allies, DateTime.Seconds(90))
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			SendActor(IFVTypes, IFVPath, allies, DateTime.Seconds(90))
		end)
	end)

	Trigger.AfterDelay(DateTime.Seconds(55), function()
		SendAirstrike(DateTime.Seconds(90))
	end)
end
