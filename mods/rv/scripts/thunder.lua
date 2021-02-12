--[[
   Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

TickThunder = function()
	if (Utils.RandomInteger(1, 200) == 10) then
		local delay = Utils.RandomInteger(1, 10)
		Lighting.Flash("LightningStrike", delay)
		Trigger.AfterDelay(delay, function()
			Media.PlaySound("thunder" .. Utils.RandomInteger(1,6) .. ".aud")
		end)
	end
	if (Utils.RandomInteger(1, 200) == 10) then
		Media.PlaySound("thunder-ambient.aud")
	end
end
