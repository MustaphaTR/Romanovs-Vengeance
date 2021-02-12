--[[
   Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

Difficulty = Map.LobbyOption("difficulty")

IdleHunt = function(actor)
	if actor.HasProperty("Hunt") and not actor.IsDead then
		Trigger.OnIdle(actor, actor.Hunt)
	end
end

InitObjectives = function(player)
	Trigger.OnObjectiveAdded(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective")
	end)

	Trigger.OnObjectiveCompleted(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed")
	end)
	Trigger.OnObjectiveFailed(player, function(p, id)
		Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed")
	end)

	Trigger.OnPlayerLost(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Lose")
		end)
	end)
	Trigger.OnPlayerWon(player, function()
		Trigger.AfterDelay(DateTime.Seconds(1), function()
			Media.PlaySpeechNotification(player, "Win")
		end)
	end)
end

-- Patrolling

Patrol2A = function(unit, waypoints, delay)
	if unit.IsDead then
		return
	end

	unit.Move(waypoints[1])
	Trigger.AfterDelay(delay, function()
		Patrol2B(unit, waypoints, delay)
	end)
end

Patrol2B = function(unit, waypoints, delay)
	if unit.IsDead then
		return
	end

	unit.Move(waypoints[2])
	Trigger.AfterDelay(delay, function()
		Patrol2A(unit, waypoints, delay)
	end)
end

Patrol4A = function(unit, waypoints, delay)
	if unit.IsDead then
		return
	end

	unit.Move(waypoints[1])
	Trigger.AfterDelay(delay, function()
		Patrol4D(unit, waypoints, delay)
	end)
end

Patrol4B = function(unit, waypoints, delay)
	if unit.IsDead then
		return
	end

	unit.Move(waypoints[2])
	Trigger.AfterDelay(delay, function()
		Patrol4A(unit, waypoints, delay)
	end)
end

Patrol4C = function(unit, waypoints, delay)
	if unit.IsDead then
		return
	end

	unit.Move(waypoints[3])
	Trigger.AfterDelay(delay, function()
		Patrol4B(unit, waypoints, delay)
	end)
end

Patrol4D = function(unit, waypoints, delay)
	if unit.IsDead then
		return
	end

	unit.Move(waypoints[4])
	Trigger.AfterDelay(delay, function()
		Patrol4C(unit, waypoints, delay)
	end)
end

-- Camera Stuff

CameraLocked = false
LockViewpoint = function(location)
	CameraLocked = true

	Camera.Position = location
	Trigger.AfterDelay(1, function()
		KeepViewpoint(location)
	end)
end

KeepViewpoint = function(location)
	if not CameraLocked then
		return
	end

	Camera.Position = location
	Trigger.AfterDelay(1, function()
		KeepViewpoint(location)
	end)
end

UnlockViewpoint = function()
	CameraLocked = false
end

MoveViewpoint = function(start, finish, numberOfFrames, currentFrame, lock)
	Camera.Position = WPos.New(((finish.X - start.X) * (currentFrame / numberOfFrames) + start.X), ((finish.Y - start.Y) * (currentFrame / numberOfFrames) + start.Y), 0)

	if currentFrame >= numberOfFrames then
		if lock then
			LockViewpoint(finish)
		end

		return
	end

	Trigger.AfterDelay(1, function()
		MoveViewpoint(start, finish, numberOfFrames, currentFrame + 1, lock)
	end)
end

-- Used for the AI:

IdlingUnits = { }
Attacking = { }
HoldProduction = { }

SetupAttackGroup = function(owner, size)
	local units = { }

	for i = 0, size, 1 do
		if #IdlingUnits[owner] == 0 then
			return units
		end

		local number = Utils.RandomInteger(1, #IdlingUnits[owner] + 1)

		if IdlingUnits[owner][number] and not IdlingUnits[owner][number].IsDead then
			units[i] = IdlingUnits[owner][number]
			table.remove(IdlingUnits[owner], number)
		end
	end

	return units
end

SendAttack = function(owner, size)
	if Attacking[owner] then
		return
	end
	Attacking[owner] = true
	HoldProduction[owner] = true

	local units = SetupAttackGroup(owner, size)
	Utils.Do(units, IdleHunt)

	Trigger.OnAllRemovedFromWorld(units, function()
		Attacking[owner] = false
		HoldProduction[owner] = false
	end)
end

DefendActor = function(unit, defendingPlayer, defenderCount)
	Trigger.OnDamaged(unit, function(self, attacker)
		if unit.Owner ~= defendingPlayer then
			return
		end

		-- Don't try to attack spiceblooms
		if attacker and attacker.Type == "spicebloom" then
			return
		end

		if Attacking[defendingPlayer] then
			return
		end
		Attacking[defendingPlayer] = true

		local Guards = SetupAttackGroup(defendingPlayer, defenderCount)

		if #Guards <= 0 then
			Attacking[defendingPlayer] = false
			return
		end

		Utils.Do(Guards, function(unit)
			if not self.IsDead then
				unit.AttackMove(self.Location)
			end
			IdleHunt(unit)
		end)

		Trigger.OnAllRemovedFromWorld(Guards, function() Attacking[defendingPlayer] = false end)
	end)
end

ProtectMiner = function(unit, owner, defenderCount)
	DefendActor(unit, owner, defenderCount)
end

RepairBuilding = function(owner, actor, modifier)
	Trigger.OnDamaged(actor, function(building)
		if building.Owner == owner and building.Health < building.MaxHealth * modifier then
			building.StartBuildingRepairs()
		end
	end)
end

DefendAndRepairBase = function(owner, baseBuildings, modifier, defenderCount)
	Utils.Do(baseBuildings, function(actor)
		if actor.IsDead then
			return
		end

		DefendActor(actor, owner, defenderCount)
		RepairBuilding(owner, actor, modifier)
	end)
end

ProduceUnits = function(player, factory, delay, toBuild, attackSize, attackThresholdSize)
	if factory.IsDead or factory.Owner ~= player then
		return
	end

	if HoldProduction[player] then
		Trigger.AfterDelay(DateTime.Seconds(10), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)
		return
	end

	player.Build(toBuild(), function(unit)
		IdlingUnits[player][#IdlingUnits[player] + 1] = unit[1]
		Trigger.AfterDelay(delay(), function() ProduceUnits(player, factory, delay, toBuild, attackSize, attackThresholdSize) end)

		if #IdlingUnits[player] >= attackThresholdSize then
			SendAttack(player, attackSize)
		end
	end)
end
