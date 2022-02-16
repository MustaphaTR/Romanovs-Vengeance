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

TreeWaypoints = { TreeWP1.Location, TreeWP2.Location, TreeWP3.Location, TreeWP4.Location, TreeWP5.Location, TreeWP6.Location, TreeWP7.Location, TreeWP8.Location, TreeWP9.Location, TreeWP10.Location, TreeWP11.Location, TreeWP12.Location, TreeWP13.Location, TreeWP14.Location, TreeWP15.Location, TreeWP16.Location, TreeWP17.Location, TreeWP18.Location, TreeWP19.Location, TreeWP20.Location, TreeWP21.Location, TreeWP22.Location, TreeWP23.Location, TreeWP24.Location, TreeWP25.Location, TreeWP26.Location, TreeWP27.Location, TreeWP28.Location, TreeWP29.Location, TreeWP30.Location, TreeWP31.Location, TreeWP32.Location, TreeWP33.Location, TreeWP34.Location, TreeWP35.Location, TreeWP36.Location, TreeWP37.Location, TreeWP38.Location, TreeWP39.Location, TreeWP40.Location, TreeWP41.Location, TreeWP42.Location, TreeWP43.Location, TreeWP44.Location, TreeWP45.Location, TreeWP46.Location, TreeWP47.Location, TreeWP48.Location, TreeWP49.Location, TreeWP50.Location, TreeWP51.Location, TreeWP52.Location, TreeWP53.Location, TreeWP54.Location, TreeWP55.Location, TreeWP56.Location, TreeWP57.Location, TreeWP58.Location, TreeWP59.Location, TreeWP60.Location, TreeWP61.Location, TreeWP62.Location, TreeWP63.Location, TreeWP64.Location, TreeWP65.Location, TreeWP66.Location, TreeWP67.Location, TreeWP68.Location, TreeWP69.Location, TreeWP70.Location, TreeWP71.Location, TreeWP72.Location, TreeWP73.Location, TreeWP74.Location }

LazarusPatrol1 = { Lazarus1WP1.Location, Lazarus1WP2.Location }
LazarusPatrol2 = { Lazarus2WP1.Location, Lazarus2WP2.Location }
GatlingPatrol1 = { Gatling1WP1.Location, Gatling1WP2.Location }
MastermindPatrol1 = { Mastermind1WP1.Location, Mastermind1WP2.Location }
MastermindPatrol2 = { Mastermind2WP1.Location, Mastermind2WP2.Location }
DiscPatrol1 = { Disc1WP1.Location, Disc1WP2.Location }
SDoggoPatrol1 = { SDogWP1.Location, SDogWP2.Location, SDogWP3.Location, SDogWP4.Location }

CivilianTypes = { "civ1", "civ2", "civ3", "civa", "civb", "civc", "civsfm", "civsf", "civstm" }
CivilianHousesEast = { EastHouse1, EastHouse2, EastHouse3, EastHouse4, EastHouse5, EastHouse6, EastHouse7, EastHouse8 }
CivilianHousesWest = { WestHouse1, WestHouse2, WestHouse3, WestHouse4, WestHouse5 }

IFVTypes = { "fv.flamer", "fv.virus", "fv.init", "fv.hijacker", "fv.ivan", "fv.tesla", "fv.iron", "fv.yuri", "fv.gatling", "fv.dog" }

ProduceCivilians = function(house)
	local delay = Utils.RandomInteger(0, 200)
	
	Trigger.AfterDelay(delay, function()
		house.Produce(Utils.Random(CivilianTypes))
	end)
end

ProduceTrees = function()
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		AWarFactory.Produce("mgtk")
	end)
end

DeployChoppers = function()
	local choppers = soviets.GetActorsByType("schp")
	Utils.Do(choppers, function(chopper)
		chopper.GrantCondition("deployed")
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

Patrol2A = function(unit, waypoints, delay)
	unit.Move(waypoints[1])
	Trigger.AfterDelay(delay, function()
		Patrol2B(unit, waypoints, delay)
	end)
end

Patrol2B = function(unit, waypoints, delay)
	unit.Move(waypoints[2])
	Trigger.AfterDelay(delay, function()
		Patrol2A(unit, waypoints, delay)
	end)
end

Patrol4A = function(unit, waypoints, delay)
	unit.Move(waypoints[1])
	Trigger.AfterDelay(delay, function()
		Patrol4D(unit, waypoints, delay)
	end)
end

Patrol4B = function(unit, waypoints, delay)
	unit.Move(waypoints[2])
	Trigger.AfterDelay(delay, function()
		Patrol4A(unit, waypoints, delay)
	end)
end

Patrol4C = function(unit, waypoints, delay)
	unit.Move(waypoints[3])
	Trigger.AfterDelay(delay, function()
		Patrol4B(unit, waypoints, delay)
	end)
end

Patrol4D = function(unit, waypoints, delay)
	unit.Move(waypoints[4])
	Trigger.AfterDelay(delay, function()
		Patrol4C(unit, waypoints, delay)
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
	AirstrikeProxy.TargetAirstrike(AirstrikeTarget.CenterPosition, Angle.East)

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

	DeployChoppers();
	GiveVeterancy();

	local lazarus1 = Actor.Create("yhtnk", true, { Owner = psicorps, Location = LazarusPatrol1[1] })
	local lazarus2 = Actor.Create("yhtnk", true, { Owner = psicorps, Location = LazarusPatrol2[1] })
	Patrol2A(lazarus1, LazarusPatrol1, DateTime.Seconds(7))
	Patrol2B(lazarus2, LazarusPatrol2, DateTime.Seconds(6))

	local gatling1 = Actor.Create("ytnk", true, { Owner = psicorps, Location = GatlingPatrol1[1] })
	Patrol2A(gatling1, GatlingPatrol1, DateTime.Seconds(10))

	local mastermind1 = Actor.Create("mind", true, { Owner = psicorps, Location = MastermindPatrol1[1], Facing = Angle.SouthEast })
	local mastermind2 = Actor.Create("mind", true, { Owner = psicorps, Location = MastermindPatrol2[1], Facing = Angle.NorthWest })
	Patrol2A(mastermind1, MastermindPatrol1, DateTime.Seconds(12))
	Patrol2A(mastermind2, MastermindPatrol2, DateTime.Seconds(12))

	local disc1 = Actor.Create("disk", true, { Owner = psicorps, Location = DiscPatrol1[1] })
	Patrol2B(disc1, DiscPatrol1, DateTime.Seconds(15))

	local sdoggo1 = Actor.Create("dog", true, { Owner = soviets, Location = SDoggoPatrol1[1] })
	local sdoggo2 = Actor.Create("dog", true, { Owner = soviets, Location = SDoggoPatrol1[1] })
	Patrol4A(sdoggo1, SDoggoPatrol1, DateTime.Seconds(5))
	Patrol4A(sdoggo2, SDoggoPatrol1, DateTime.Seconds(5))

	Utils.Do(CivilianHousesEast, function(house)
		ProduceCivilians(house)

		Trigger.OnProduction(house, function(a, civ)
			civ.DeliverCash(PGrinderEast)

			Trigger.AfterDelay(400, function()
				ProduceCivilians(house)
			end)
		end)
	end)

	Utils.Do(CivilianHousesWest, function(house)
		ProduceCivilians(house)

		Trigger.OnProduction(house, function(a, civ)
			civ.DeliverCash(PGrinderWest)

			Trigger.AfterDelay(400, function()
				ProduceCivilians(house)
			end)
		end)
	end)

	ProduceTrees()
	Trigger.OnProduction(AWarFactory, function(a, mirage)
		local location = Utils.Random(TreeWaypoints)
		mirage.Move(location)

		TreeWaypoints = Utils.Where(TreeWaypoints, function(wp) return location ~= wp end)
		if #TreeWaypoints > 0 then
			Trigger.AfterDelay(Actor.BuildTime("mgtk"), function()
				ProduceTrees()
			end)
		end
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
