--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

VeteranUnits = { Ivan }
EliteUnits = { Vladimir }

V3Path = { RoadBWP1.Location, RoadBWP2.Location, RoadBWP3.Location, RoadBWP4.Location, RoadBWP5.Location, RoadBWP6.Location, RoadBWP7.Location, RoadBWP8.Location, RoadBWP9.Location, RoadAWP7.Location, RoadAWP6.Location, RoadAWP5.Location, RoadAWP4.Location, RoadAWP3.Location, RoadAWP2.Location, RoadAWP1.Location }
FlakPath = { RoadAWP1.Location, RoadAWP2.Location, RoadAWP3.Location, RoadAWP4.Location, RoadAWP5.Location, RoadAWP6.Location, RoadAWP7.Location, RoadAWP8.Location, RoadAWP9.Location, RoadAWP10.Location, RoadAWP11.Location, RoadAWP12.Location, RoadAWP13.Location, RoadAWP14.Location, RoadAWP15.Location, RoadAWP16.Location, RoadAWP17.Location, RoadAWP18.Location, RoadAWP19.Location, RoadAWP20.Location, RoadAWP21.Location }

DoggoPatrolPath = { DoggoPatrolA.Location, DoggoPatrolB.Location, DoggoPatrolC.Location, DoggoPatrolD.Location }
FlakPatrolPath = { FlakPatrolA.Location, FlakPatrolB.Location }

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

SendActor = function(unit, waypoints, delay)
	local actor = Actor.Create(unit, true, { Owner = soviets, Location = waypoints[1] })
	for _,waypoint in pairs(waypoints) do 
		actor.Move(waypoint)
	end
	actor.Destroy()

	Trigger.AfterDelay(delay, function()
		SendActor(unit, waypoints, delay)
	end)
end

ticks = 750
speed = 7

Tick = function()
	ticks = ticks + 1

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(40960 * math.sin(t), 29440 * math.cos(t), 0)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")
	viewportOrigin = Camera.Position

	DeployChoppers()
	GiveVeterancy()

	local dog1 = Actor.Create("dog", true, { Owner = soviets, Location = DoggoPatrolPath[1], SubCell = 1 })
	local dog2 = Actor.Create("dog", true, { Owner = soviets, Location = DoggoPatrolPath[1], SubCell = 2 })
	local doge2 = Actor.Create("e2", true, { Owner = soviets, Location = DoggoPatrolPath[1], SubCell = 3 })
	Patrol4B(dog1, DoggoPatrolPath, DateTime.Seconds(12))
	Patrol4B(dog2, DoggoPatrolPath, DateTime.Seconds(12))
	Patrol4B(doge2, DoggoPatrolPath, DateTime.Seconds(12))

	local flak1 = Actor.Create("htk", true, { Owner = soviets, Location = FlakPatrolPath[1] })
	Patrol2B(flak1, FlakPatrolPath, DateTime.Seconds(5))

	SendActor("v3", V3Path, DateTime.Seconds(90))
	Trigger.AfterDelay(DateTime.Seconds(20), function()
		SendActor("htk", FlakPath, DateTime.Seconds(60))
	end)
end
