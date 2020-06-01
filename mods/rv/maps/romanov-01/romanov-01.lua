--[[
   Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
   This file is part of OpenRA, which is free software. It is made
   available to you under the terms of the GNU General Public License
   as published by the Free Software Foundation, either version 3 of
   the License, or (at your option) any later version. For more
   information, see COPYING.
]]

APillBox1WarningZone = { CPos.New(104, -9), CPos.New(105, -9), CPos.New(105, -10), CPos.New(105, -12), CPos.New(106, -10), CPos.New(106, -11), CPos.New(106, -12) }
PrisonZone = { CPos.New(68, -16), CPos.New(69, -16), CPos.New(68, -17), CPos.New(69, -17), CPos.New(68, -18), CPos.New(69, -18), CPos.New(68, -19), CPos.New(69, -19), CPos.New(66, -20), CPos.New(67, -20), CPos.New(68, -20), CPos.New(69, -20), CPos.New(70, -20), CPos.New(71, -20), CPos.New(66, -25), CPos.New(67, -21), CPos.New(71, -21), CPos.New(67, -22), CPos.New(71, -22), CPos.New(67, -23), CPos.New(71, -23), CPos.New(67, -24), CPos.New(71, -24), CPos.New(67, -25), CPos.New(68, -25), CPos.New(69, -25), CPos.New(70, -25), CPos.New(71, -25) }

PatrolWPs1 = { APatrolWP1A.Location, APatrolWP1B.Location }
PatrolWPs2 = { APatrolWP2A.Location, APatrolWP2B.Location }
PatrolWPs3 = { APatrolWP3A.Location, APatrolWP3B.Location }
PatrolWPs4 = { APatrolWP4A.Location, APatrolWP4B.Location }
PatrolWPs5 = { APatrolWP5A.Location, APatrolWP5B.Location }

SendStartingParadrops = function()
	paracamera.Destroy()
	local units = powerproxy.ActivateParatroopers(StartingParadropPoint.CenterPosition, 32)
	powerproxy.Destroy()
end

SetUpPatrols = function()
	ADog1 = Actor.Create("dog", true, { Owner = allies, Location = PatrolWPs1[1] })
	ADog2 = Actor.Create("dog", true, { Owner = allies, Location = PatrolWPs1[1] })
	ADog3 = Actor.Create("dog", true, { Owner = allies, Location = PatrolWPs2[1] })
	ADog4 = Actor.Create("dog", true, { Owner = allies, Location = PatrolWPs2[1] })
	AGI1  = Actor.Create("e1" , true, { Owner = allies, Location = PatrolWPs3[1] })
	AGI2  = Actor.Create("e1" , true, { Owner = allies, Location = PatrolWPs4[1] })
	ADog5 = Actor.Create("dog", true, { Owner = allies, Location = PatrolWPs3[1] })
	ADog6 = Actor.Create("dog", true, { Owner = allies, Location = PatrolWPs4[1] })
	AGI3  = Actor.Create("e1" , true, { Owner = allies, Location = PatrolWPs5[1] })

	Patrol2B(ADog1, PatrolWPs1, DateTime.Seconds( 8))
	Patrol2B(ADog2, PatrolWPs1, DateTime.Seconds( 8))
	Patrol2B(ADog3, PatrolWPs2, DateTime.Seconds( 8))
	Patrol2B(ADog4, PatrolWPs2, DateTime.Seconds( 8))
	Patrol2B(AGI1 , PatrolWPs3, DateTime.Seconds( 8))
	Patrol2B(AGI2 , PatrolWPs4, DateTime.Seconds( 8))
	Patrol2B(ADog5, PatrolWPs3, DateTime.Seconds( 9))
	Patrol2B(ADog6, PatrolWPs4, DateTime.Seconds( 9))
	Patrol2B(AGI3 , PatrolWPs5, DateTime.Seconds(12))
end

SetUpWarnings = function()
	Trigger.OnEnteredFootprint(APillBox1WarningZone, function(a, id)
		local camera = Actor.Create("camera.5c0", true, { Owner = soviets, Location = APillBox1.Location })
		Media.PlaySoundNotification(soviets, "Warning")

		Trigger.RemoveFootprintTrigger(id)
		Trigger.AfterDelay(DateTime.Seconds(4), function()
			camera.Destroy()
		end)
	end)
end

PrisonGuardsAndRomanov = function()
	
end

Intro = function()
	LockViewpoint(StartingParadropPoint.CenterPosition)

	SendStartingParadrops()

	Trigger.AfterDelay(DateTime.Seconds(2), function()
		Media.PlaySpeechNotification(soviets, "EstablishingBattlefieldcontrolStandBy")

		Trigger.AfterDelay(DateTime.Seconds(4), function()
			Media.DisplayMessage("A small group of infantry has landed to south of the prison.", "Zofia", HSLColor.Red)

			Trigger.AfterDelay(DateTime.Seconds(4), function()
				Media.DisplayMessage("Unfortunately, this is all we could spare. You need to be careful.", "Zofia", HSLColor.Red)

				Trigger.AfterDelay(DateTime.Seconds(4), function()
					UnlockViewpoint()
					MoveViewpoint(StartingParadropPoint.CenterPosition, Prison.CenterPosition, DateTime.Seconds(2), 0, true)
					local camera = Actor.Create("camera.5c0", true, { Owner = soviets, Location = Prison.Location + CVec.New(1,1) })

					Trigger.AfterDelay(DateTime.Seconds(3), function()
						Media.DisplayMessage("Premier is being held here.", "Zofia", HSLColor.Red)

						Trigger.AfterDelay(DateTime.Seconds(4), function()
							UnlockViewpoint()
							MoveViewpoint(Prison.CenterPosition, APTower2.CenterPosition, DateTime.Seconds(1), 0, true)
							camera.Destroy()
							camera = Actor.Create("camera.6c0", true, { Owner = soviets, Location = APTower2.Location })

							Trigger.AfterDelay(DateTime.Seconds(2), function()
								Media.DisplayMessage("There is no way we can enter anywhere close to the main entrance.", "Zofia", HSLColor.Red)

								Trigger.AfterDelay(DateTime.Seconds(4), function()
									UnlockViewpoint()
									MoveViewpoint(APTower2.CenterPosition, IntroBackdoor.CenterPosition, DateTime.Seconds(2), 0, true)
									camera.Destroy()
									camera = Actor.Create("camera.8c0", true, { Owner = soviets, Location = IntroBackdoor.Location })

									Trigger.AfterDelay(DateTime.Seconds(4), function()
										Media.DisplayMessage("We need to sneak from behind.", "Zofia", HSLColor.Red)

										Trigger.AfterDelay(DateTime.Seconds(4), function()
											UnlockViewpoint()
											MoveViewpoint(IntroBackdoor.CenterPosition, StartingParadropPoint.CenterPosition, DateTime.Seconds(2), 0, false)
											camera.Destroy()

											Trigger.AfterDelay(DateTime.Seconds(2), function()
												Media.PlaySpeechNotification(soviets, "BattleControlOnline")
											end)
										end)
									end)
								end)
							end)
						end)
					end)
				end)
			end)
		end)
	end)
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

	allies = Player.GetPlayer("Allies")
	soviets = Player.GetPlayer("Soviets")

	allies.Cash = 10000

	InitObjectives(soviets)
	RomanovPrisoners = soviets.AddPrimaryObjective("Free Premier Romanov from Prison.")
	RomanovSurvive = soviets.AddPrimaryObjective("Romanov must survive.")
	allies.AddPrimaryObjective("Do not let Romanov escape alive.")

	Intro()
	SetUpPatrols()
	SetUpWarnings()
end
