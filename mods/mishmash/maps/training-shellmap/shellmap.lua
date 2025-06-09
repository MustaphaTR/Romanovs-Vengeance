--[[
   Copyright (c) The OpenRA Developers and Contributors
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

TankGIs = { TankGI1, TankGI2, TankGI3, TankGGI1 }
WallGGIs = { WallGGI1, WallGGI2, WallGGI3, WallGGI4, WallGGI5 }
VeteranUnits = { VetGI1, VetGI2 }
EliteUnits = { General }

DoggoPatrol1 = { Doggo1WP1.Location, Doggo1WP2.Location }
DoggoPatrol2 = { Doggo2WP1.Location, Doggo2WP2.Location }
RobotPatrol1 = { Robot1WP1.Location, Robot1WP2.Location }

CargoPlaneWP = { CargoPlaneEntry.Location, CargoPlaneExit.Location }

PonyTypes = { "rainbow_dash", "derpy_hooves" }
PonyPath = { PonyEntry.Location, PonyExit.Location }

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

SendPonies = function(unit_id)
	local pony = Actor.Create(PonyTypes[unit_id], true, { Owner = neutral, Location = PonyPath[1] })
	pony.Move(PonyPath[2]);
	pony.Destroy();

	Trigger.AfterDelay(DateTime.Minutes(1), function()
		SendPonies((unit_id % #PonyTypes) + 1);
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

SendCargoPlane = function(unit, waypoints, delay)
	local actor = Actor.Create(unit, true, { Owner = allies, Location = waypoints[1] })
	actor.Move(waypoints[2])
	actor.Destroy()

	Trigger.AfterDelay(delay, function()
		SendCargoPlane(unit, waypoints, delay)
	end)
end

ticks = 1250
speed = 5

Tick = function()
	ticks = ticks + 1

	local t = (ticks + 45) % (360 * speed) * (math.pi / 180) / speed;
	Camera.Position = viewportOrigin + WVec.New(19200 * math.sin(t), 20480 * math.cos(t), 0)
end

WorldLoaded = function()
	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")
	neutral = Player.GetPlayer("Neutral")
	viewportOrigin = Camera.Position

	GiveVeterancy();

	if DateTime.CurrentMonth == 10 and DateTime.CurrentDay == 10 then -- October 10th
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			SendPonies(1)
		end)
	end

	Utils.Do(TankGIs, function(tankgi)
		tankgi.Attack(DerelictTank)
	end)

	Trigger.AfterDelay(20, function()
		Sniper1.Attack(RifleRange1)
	end)
	Trigger.AfterDelay(40, function()
		Sniper2.Attack(RifleRange2)
	end)
	Trigger.AfterDelay(60, function()
		Sniper3.Attack(RifleRange3)
	end)

	local dog1 = Actor.Create("dog", true, { Owner = allies, Location = DoggoPatrol1[1], SubCell = 1 })
	local dog2 = Actor.Create("dog", true, { Owner = allies, Location = DoggoPatrol1[2], SubCell = 2 })
	local dog3 = Actor.Create("dog", true, { Owner = allies, Location = DoggoPatrol2[1], SubCell = 1 })
	local dog4 = Actor.Create("dog", true, { Owner = allies, Location = DoggoPatrol2[2], SubCell = 2 })
	PatrolB(dog1, DoggoPatrol1, DateTime.Seconds(7))
	PatrolA(dog2, DoggoPatrol1, DateTime.Seconds(7))
	PatrolB(dog3, DoggoPatrol2, DateTime.Seconds(7))
	PatrolA(dog4, DoggoPatrol2, DateTime.Seconds(7))

	local robo1 = Actor.Create("robo", true, { Owner = allies, Location = RobotPatrol1[1] })
	PatrolB(robo1, RobotPatrol1, DateTime.Seconds(5))

	Trigger.AfterDelay(DateTime.Seconds(15), function()
		SendCargoPlane("alplane", CargoPlaneWP, DateTime.Seconds(90))
	end)
end
