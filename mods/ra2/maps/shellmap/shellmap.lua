--[[
   Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

TankGIs = { TankGI1, TankGI2, TankGI3 }
VetGIs = { VetGI1, VetGI2 }

DeployGIs = function()
	local gis = allies.GetActorsByType("e1")
	Utils.Do(gis, function(gi)
		gi.GrantCondition("deployed")
	end)
end

GiveVeterancy = function()
	Utils.Do(VetGIs, function(VetGI)
		VetGI.GrantCondition("rank-veteran")
	end)
	
	General.GrantCondition("rank-veteran")
	General.GrantCondition("rank-veteran")
	General.GrantCondition("rank-veteran")
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
	viewportOrigin = Camera.Position
	
	DeployGIs();
	GiveVeterancy();

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
end
