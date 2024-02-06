## Player
options-tech-level =
   .one = 1
   .two = 2
   .three = 3
   .four = 4
   .five = 5

checkbox-redeployable-mcvs =
   .label = Redeployable MCVs
   .description = Allow undeploying Construction Yard

checkbox-free-minimap =
   .label = Free Minimap
   .description = Minimap is active without a building to enable it

checkbox-limit-super-weapons =
   .label = Limit Super Weapons
   .description = Only 1 of each super weapon can be built by a player

checkbox-tech-build-area =
   .label = Tech Build Area
   .description = Allow building placement around tech structures

checkbox-instant-capture =
   .label = Instant Capture
   .description = Engineers can enter a building without waiting to capture

checkbox-multiqueue =
   .label = MultiQueue
   .description = Each production facility can produce individually

checkbox-upgrades-option =
   .label = Upgrades
   .description = Enables researching upgrades that improve existing units

checkbox-domination-option =
   .label = Domination
   .description = Control the flags on the map to win

checkbox-megawealth-option =
   .label = Megawealth
   .description = Removes all the Ore on the map and makes the economy dependent on Oil Derricks

checkbox-show-owner-name =
   .label = Show Owner Name
   .description = Show name and flag of the owner of a unit on its tooltip

checkbox-sudden-death =
   .label = Sudden Death
   .description = Players can't build another MCV and get defeated when they lose it

checkbox-king-of-the-hill =
   .label = King of the Hill
   .description = Capture and hold the Psychic Beacon on the map to win

checkbox-regicide =
   .label = Regicide
   .description = Kill enemy leader to win the game

notification-insufficient-funds = Insufficient funds.
notification-new-construction-options = New construction options.
notification-cannot-deploy-here = Cannot deploy here.
notification-low-power = Low power.
notification-base-under-attack = Our base is under attack.
notification-ally-under-attack = Our ally is under attack.
notification-ore-miner-under-attack = Ore miner under attack.
notification-insufficient-silos = Insufficient silos.

## World
dropdown-cpmodifier =
   .label = CP Per Rank
   .description = Commander's Points you get when you rank up, per rank.

options-cpmodifier =
   .disabled = 0, 0, 0, 0, 0
   .one = 1, 1, 1, 1, 1
   .normal = 1, 1, 1, 1, 3
   .two = 2, 2, 2, 2, 2
   .double = 2, 2, 2, 2, 6
   .three = 3, 3, 3, 3, 3
   .triple = 3, 3, 3, 3, 5
   .all = 4, 0, 11, 0, 2

options-starting-units =
   .no-bases = No Bases
   .mcv-only = MCV Only
   .mcv-and-dog = MCV and Dog
   .light-support = Light Support
   .medium-support = Medium Support
   .heavy-support = Heavy Support
   .unholy-alliance = Unholy Alliance

## Defaults
notification-unit-lost = Unit lost.
notification-unit-promoted = Unit promoted.
notification-primary-building-selected = Primary building selected.
notification-building-captured = Building captured.
notification-tech-building-captured = Tech building captured.
notification-tech-building-lost = Tech building lost.

## Structures
notification-construction-complete = Construction complete.
notification-unit-ready = Unit ready.
notification-upgrade-complete = Upgrade complete.
notification-unable-to-build-more = Unable to build more.
notification-unable-to-comply-building-in-progress = Unable to comply. Building in progress.
notification-upgrade-in-progress = Upgrade in progress.
notification-repairing = Repairing.
notification-unit-repaired = Unit repaired.
notification-select-target = Select target.
notification-spy-plane-ready = Spy plane ready.
notification-paratroopers-ready = Paratroopers ready.
notification-enemy-airstrike-initiated = Warning: Enemy airstrike initiated.
notification-force-shield-ready = Force Shield ready.
notification-force-shield-activated = Force Shield activated.
notification-lightning-storm-ready = Lightning Storm ready.
notification-lightning-storm-created = Warning: Lightning Storm created.
notification-weather-control-device-detected = Warning: Weather Control Device detected.
notification-chronosphere-ready = Chronosphere ready.
notification-chronosphere-activated = Warning: Chronosphere activated.
notification-chronosphere-detected = Warning: Chronosphere detected.
notification-iron-curtain-ready = Iron Curtain ready.
notification-iron-curtain-activated = Warning: Iron Curtain activated.
notification-iron-curtain-detected = Warning: Iron Curtain detected.
notification-nuclear-missile-ready = Nuclear Missile ready.
notification-nuclear-missile-launched = Warning: Nuclear Missile launched.
notification-nuclear-silo-detected = Warning: Nuclear Silo detected.
notification-lazarus-shield-ready = Lazarus Shield ready.
notification-lazarus-shield-activated = Warning: Lazarus Shield activated.
notification-lazarus-shield-generator-detected = Warning: Lazarus Shield Generator detected.
notification-psychic-dominator-ready = Psychic Dominator ready.
notification-psychic-dominator-activated = Warning: Psychic Dominator activated.
notification-psychic-dominator-detected = Warning: Psychic Dominator detected.

## aircraft.yaml
actor-shad =
   .name = Night Hawk
   .description = Infantry Transport Helicopter.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilties:
    - Can detect Stealth units
    - Invisible on radar

actor-zep =
   .name = Kirov Airship
   .description = Massive zeppelins that are the aerial terror of the Soviet force.
    
      Strong vs Buildings
      Weak vs Infantry, Vehicles
    
    Upgradeable with:
    - Radioactive Warheads
    - Advanced Irradiators (Iraq)
    - Aerial Propaganda (Vietnam)

actor-orca =
   .name = Harrier
   .description = Fast assault fighter.
    Cannot be built more than landing pads available.
    
      Strong vs Buildings, Infantry, Vehicles
      Weak vs Aircraft
    
    Upgradeable with:
    - Air-to-Air Missile Systems
    - Predator Missiles

actor-beag =
   .name = Black Eagle
   .description = Aircraft armed with EMP bomb.
    Cannot be built more than landing pads available.
      Strong vs Buildings, Vehicles
      Weak vs Infantry, Aircraft

actor-pdplane =
   .name = Cargo Plane
actor-a10 =
   .name = A-10 Thunderbolt
actor-txbmb =
   .name = Toxin Bomber
actor-f22drop =
   .name = F-22 Raptor
actor-b52 =
   .name = B-52 Stratofortress
actor-hornet =
   .name = Hornet
actor-asw =
   .name = Osprey
actor-spyp =
   .name = Spy Plane

actor-schp =
   .name = Siege Chopper
   .description = Helicopter capable of deploying into a long range artillery.
    
    Mobile:
      Strong vs Infantry
      Weak vs Vehicles
    Deployed:
      Strong vs Buildings, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can detect Stealth units
    
    Upgradeable with:
    - Armor-Piercing Bullets

actor-bpln =
   .name = MiG
   .description = Fast assault fighter.
    Cannot be built more than landing strips available.
    
      Strong vs Buildings, Vehicles, Aircraft
      Weak vs Infantry

actor-disk =
   .name = Leech Disc
   .description = Floating Disc armed with lasers.
    Can disable Power Plants and powered Defenses.
    Can steal resources from enemy Refineries.
    Can steal technology from enemy Battle Labs.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Upgradeable with:
    - Disc Armor (Lunar Eclipse)
    - Laser Capacitors (Lazarus Corps)

actor-hind =
   .name = Hind Carryall
   .description = Vehicle Transport Helicopter
    
      Unarmed

actor-fortress =
   .name = B2 Spirit
   .description = Carpet bomber plane.
    Cannot be built more than landing pads available.
    
      Strong vs Buildings, Vehicles
      Weak vs Aircraft
    
    Abilties:
    - Invisible on radar
    
    Upgradeable with:
    - Increased Payload

actor-sdrn =
   .name = Scout Drone

actor-txdx =
   .name = Lethocerus Platform
   .description = Light Transport Disc armed with toxin missile launchers.
    
      Strong vs Infantry
      Weak vs Structures, Aircraft
    
    Abilties:
    - Can detect Stealth units
    
    Upgradeable with:
    - Disc Armor (Lunar Eclipse)
    - Rocket Barrage

actor-kite =
   .name = Black Kite
   .description = Helicopter armed with napalm bombs.
    
      Strong vs Infantry, Buildings
      Weak vs Vehicles
    
    Upgradeable with:
    - Black Napalm
    - Aerial Propaganda (Vietnam)

actor-magnedisk =
   .name = Mosquito Disc
   .description = High-tech Disc armed with material drainer.
    After attacking organic material 3 times a Blood Vessel will be spawned.
    
      Strong vs Infantry
      Weak vs Aircraft
    
    Upgradeable with:
    - Disc Armor (Lunar Eclipse)

actor-havoc =
   .name = Havoc Attack Helicopter
   .description = Armored helicopter armed with anti-tank rockets.
    
      Strong vs Vehicles
    
      Weak vs Infantry, Aircraft

actor-badgr =
   .name = Tu-16 Badger
   .description = Heavy bomber plane armed with a fuel air bomb.
    Cannot be built more than landing pads available.
    
      Strong vs Buildings, Vehicles
      Weak vs Aircraft

actor-repdron =
   .name = Repair Drone

## allied-infantry.yaml
actor-engineer =
   .name = Engineer
   .description = Captures enemy structures, repairs damaged buildings and bridges.
    Disarms explosives.
    
    Abilities:
    - Can swim
    
      Unarmed

actor-dog =
   .name = Attack Dog
   .generic-name = Dog
   .allies-name = German Shepherd
   .soviet-name = Siberian Husky
   .psicorps-name = Rottweiler
   .description = Anti-infantry unit.
    Can be deployed to stun nearby infantry for a short while.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can swim
    - Can detect Stealth units
    - Can detect Spies

actor-e1 =
   .name = G.I.
   .description = General-purpose infantry.
    Can deploy to gain more range and damage.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Upgradeable with:
    - Fiber-Reinforced Fortifications
    - Advanced Training
    - Portable Weaponry (United Kingdom)

actor-ggi =
   .name = Guardian G.I.
   .description = Anti-tank and anti-air infantry.
    Can deploy to gain more range.
    
      Strong vs Vehicles, Aircraft
      Weak vs Buildings
    
    Upgradeable with:
    - Fiber-Reinforced Fortifications
    - Advanced Training
    - Boost-Gliding Systems (United States)
    - Portable Weaponry (United Kingdom)

actor-snipe =
   .name = Sniper
   .description = Special anti-infantry unit.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can kill garrisoned infantry

actor-spy =
   .name = Spy
   .description = Infiltrates enemy structures for intel or sabotage.
    Exact effect depends on the
    building infiltrated.
    
      Unarmed
    
    Abilities:
    - Can swim
    - Can disguise as an enemy infantry

actor-ghost =
   .name = Navy SEAL
   .description = Elite commando infantry, armed with a sub machine gun
    and C4 charges.
    
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can swim
    - Can place C4 on buildings
    
    Upgradeable with:
    - Advanced Training

actor-ccomand =
   .name = Chrono Commando
   .description = Elite commando infantry, armed with
    a sub machine gun and C4 charges.
    
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can place C4 on buildings
    - Can teleport anywhere on the map
    
    Upgradeable with:
    - Advanced Training

actor-ptroop =
   .name = Psi Commando
   .description = Psychic infantry. Can mind control enemy units.
    
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Dogs, Terror Drones, Aircraft
    
    Abilities:
    - Can place C4 on buildings
    
    Upgradeable with:
    - Mastery of Mind (Antarctica)

actor-tany =
   .name = Tanya Adams
   .description = Elite commando infantry, armed with
    dual pistols and C4 charges.
    
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Can swim
    - Can place C4 on buildings and vehicles
    
    Upgradeable with:
    - Advanced Training
    
      Maximum 1 can be trained

actor-jumpjet =
   .name = Rocketeer
   .description = Airborne soldier.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Upgradeable with:
    - Advanced training

actor-cleg =
   .name = Chrono Legionnaire
   .description = High-tech soldier capable of erasing enemy units.
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Can teleport anywhere on the map

actor-gren =
   .name = Grenadier
   .description = Infantry armed with grenades.
    Can kill garrisoned infantry.
    
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
    
    Upgradeable with:
    - Advanced Training
    - EMP Munitions (Korea)

## allied-naval.yaml
actor-dest =
   .name = Destroyer
   .description = Allied Main Battle Ship armed with cannons and
    an Osprey helicopter.
      Strong vs Naval units
      Weak vs Ground units, Aircraft
    
    Abilities:
    - Can detect Stealth units

actor-aegis =
   .name = Aegis Cruiser
   .description = Anti-Air naval unit.
    
      Strong vs Aircraft
      Weak vs Grounds units, Ships
    
    Upgradeable with:
    - Boost-Gliding Systems (United States)

actor-dlph =
   .name = Dolphin
   .description = Trained dolphin armed with sonic beams.
    
      Strong vs Ships
    
    Abilities:
    - Can remove Squids from ships by attacking them
    - Can detect Stealth units

actor-carrier =
   .name = Aircraft Carrier
   .description = Aircraft carrier ship.
    
      Strong vs Tanks, Structures
      Weak vs Infantry

actor-adest =
   .name = Assault Destroyer
   .description = Heavy vehicle armed with a cannon that can move over both land and water.
      Strong vs Vehicles, Ships
      Weak vs Infantry, Aircraft
    
    Abilities:
    - Can crush enemy land vehicles
    - Can detect Stealth units

## allied-structures.yaml
actor-gapowrup =
   .name = Advanced Coolants
   .description = Better coolants allow power plants to work more efficently
    providing 150 more power.

actor-gapile =
   .description = Trains Allied infantry.
    Can heal nearby infantry.
    
      Cannot be placed on water.
      Can be rotated.

actor-gaairc =
   .name = Airforce Command Headquarters
   .description = Provides radar.
    Produces Allied aircraft.
    Supports 4 aircraft.
    Researches basic upgrades.
    
    Provides a different support power depening on the subfaction:
    - Airborne (United States)
    - Carpet Bombing (United Kingdom)
    - Force Shield (France)
    - Chrono Grizzly (Germany)
    - Chronobomb (Korea)
    
    Abilities:
    - Comes with 3 repair drones.

actor-gaweap =
   .description = Produces Allied vehicles.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Cannot be placed on water.
      Can be rotated.

actor-gayard =
   .description = Produces Allied ships, and other naval units.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Can only be placed on water.

actor-gawall =
   .name = Allied Wall
   .description = Heavy wall capable of blocking units and projectiles.
    
    Upgradeable with:
    - Grand Cannon Protocols (France)
    
      Cannot be placed on water.

actor-gapill =
   .name = Pill Box
   .description = Automated anti-infantry defense.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Grand Cannon Protocols (France)

actor-nasam =
   .name = Patriot Missile System
   .description = Automated anti-aircraft defense.
    
      Strong vs Aircraft
      Weak vs Ground units
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Boost-Gliding Systems (United States)
    - Grand Cannon Protocols (France)
    
      Requires power to operate.

actor-gtgcan =
   .name = Grand Cannon
   .description = Automated, long ranged anti-ground defense.
    
      Strong vs Buildings, Vehicles
      Weak vs Aircraft
    
      Requires power to operate.

actor-gaorep =
   .name = Ore Purifier
   .megawealth-name = Oil Purifier
   .description = Refines income from ores and gems by 25%.
    
      Maximum 1 can be built.

actor-gaspysat =
   .name = Spy Satellite Uplink
   .description = Provides Satellite Scan power, which reveals the map for a while.
    Provides Radar.
    
      Requires power to operate.

actor-gagap =
   .name = Gap Generator
   .description = Obscures the enemy's view with shroud.
    
      Requires power to operate.

actor-gaweat =
   .name = Weather Controller
   .description = Play God with deadly weather!
    
      Requires power to operate.

actor-gacsph =
   .name = Chronosphere
   .description = Allows teleportation of vehicles.
    Kills infantry.
    
      Requires power to operate.
      Can be rotated.

actor-atesla =
   .name = Prism Tower
   .description = Advanced base defense.
    Can buff at most 3 other Prism Towers nearby.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Grand Cannon Protocols (France)
    
      Requires power to operate.

actor-garobo =
   .name = Robot Control Center
   .description = Allows production of Robot Tanks.
    Required for Robot Tanks to function.

actor-gaapad =
   .name = Airpad
   .description = Rearms up to 3 aircraft.

actor-gagate =
   .name = Allied Gate
   .description = Automated barrier that opens for allied units.
    
    Upgradeable with:
    - Grand Cannon Protocols (France)
    
      Can be rotated.

actor-gamgun =
   .name = Mirage Turret
   .description = Defensive structure that is invisible while not firing.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can be placed further than other buildings
    - Can detect Steath units
    
    Upgradeable with:
    - Grand Cannon Protocols (France)
    
      Requires power to operate.
      Can be rotated.
 
actor-gagun =
   .name = Gun Turret
   .description = Anti-tank defense structure.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Grand Cannon Protocols (France)
    
      Requires power to operate.

## allied-vehicles.yaml
actor-cmin =
   .name = Chrono Miner
   .description = Gathers Ore and Gems.
    
      Unarmed
    
    Abilities:
    - Can move over water
    - Cannot be mind controlled
    - Can teleport back to refineries

actor-mtnk =
   .name = Grizzly Battle Tank
   .description = Allied Main Battle Tank.
    
      Strong vs Vehicles, Ships
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Composite Armor
    - Advanced Gun Systems (Germany)

actor-tnkd =
   .name = Tank Destroyer
   .description = Special anti-armor unit.
    
      Strong vs Vehicles, Ships
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Composite Armor
    - Advanced Gun Systems (Germany)

actor-fv =
   .name = Infantry Fighting Vehicle
   .mg-name = Machine Gun IFV
   .init-name = Ignitor IFV
   .rocket-name = Rocket IFV
   .gren-name = Grenade IFV
   .mortar-name = Mortar IFV
   .engineer-name = Repair IFV
   .medic-name = Ambulance IFV
   .dog-name = Detector IFV
   .hijack-name = Grinder IFV
   .sniper-name = Sniper IFV
   .virus-name = Virus IFV
   .pyro-name = Flamethrower IFV
   .flak-name = Flak IFV
   .gatling-name = Gatling IFV
   .tesla-name = Tesla IFV
   .desolator-name = Desolator IFV
   .demo-name = Demolition IFV
   .seal-name = Sub-Machine Gun IFV
   .tanya-name = Tanya IFV
   .boris-name = Boris IFV
   .chrono-name = Chrono IFV
   .yuri-name = Psi IFV
   .iron-name = Iron Curtain IFV
   .lazarus-name = Lazarus IFV
   .toxin-name = Toxin Sprayer IFV
   .crkt-name = Chaos Rocket IFV
   .description = Multi-Purpose Vehicle.
    
    Without passenger:
      Strong vs Infantry, Aircraft
      Weak vs Vehicles, Ships
    
    Abilities:
    - Armament changes depending on passenger
    - Can detect Stealth units
    
    Upgradeable with:
    - Boost-Gliding Systems (United States)

actor-sref =
   .name = Prism Tank
   .description = Fires deadly beams of light.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft

actor-mgtk =
   .name = Mirage Tank
   .description = Tank that appears as a tree while immobile.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Upgradeable with:
    - Composite Armor

actor-bfrt =
   .name = Battle Fortress
   .description = Large vehicle with 5 fireports for infantry in to fire.
    
    Abilities:
    - Can crush enemy vehicles
    
    Upgradeable with:
    - Composite Armor

actor-robo =
   .name = Robot Tank
   .description = Remote controlled vehicle.
    Requires an Allied War Factory to operate.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can move over water
    - Cannot be mind controlled

actor-aambu =
   .name = Combat Ambulance
   .description = Heavy armored vehicle capable of healing nearby infantry.
    Can carry up to 5 infantry and heal them faster inside.
    
      Unarmed
    
    Upgradeable with:
    - Composite Armor

actor-hwtz =
   .name = Howitzer
   .description = Long range vehicle.
    
      Strong vs Infantry, Buildings
      Weak vs Aircraft
    
    Upgradeable with:
    - EMP Munitions (Korea)

actor-ctnk =
   .name = Chrono Dragon
   .description = Armed with anti-ground missiles.
    Teleports to areas within range.
    
      Strong vs Vehicles, Buildings
      Weak vs Infantry, Aircraft
    
    Abilities:
    - Can teleport anywhere on the map
    
    Upgradeable with:
    - Boost-Gliding Systems (United States)

actor-mrcv =
   .name = Mobile Robot Control Vehicle

## animals.yaml
actor-cow =
   .name = Cow
actor-all =
   .name = Alligator

actor-polarb =
   .name = Polar Bear
   .generic-name = Bear

actor-josh =
   .name = Monkey
actor-caml =
   .name = Camel
actor-gbear =
   .name = Grizzly Bear

## arrakis.yaml
actor-concrete =
   .name = Concrete Slab
   .description = Our buildings are strong enough for Arrakis' terrain, but concrete still looks good.

## bakuvian-infantry.yaml
actor-rctt =
   .name = Rocket Soldier
   .description = Anti-Air and anti-Vehicle unit.
    
      Strong vs Vehicle, Aircraft
      Weak vs Infantry
    
    Upgradeable with:
    - Bullet-Proof Coats

actor-medi =
   .name = Medic
   .description = Heals infantry.
    
      Unarmed

actor-mech =
   .name = Mechanic
   .description = Repairs friendly vehicles.
    
      Unarmed

actor-mengineer =
   .name = Motorised Engineer
   .description = Fast unit capable of capturing enemy structures (slower than regular engineers), repairing damaged buildings and bridges.
    Disarms explosives.
    
      Unarmed.
    
    Abilities:
    - Cannot be eaten by Attack Dogs

actor-sspy =
   .description = Infiltrates enemy structures for intel or sabotage.
    Exact effect depends on the
    building infiltrated.
    Steals enemy vehicles.
    
      Unarmed
    
    Abilities:
    - Can swim
    - Can disguise as an enemy infantry

actor-mtrp =
   .name = Mortar Trooper
   .description = Long range infantry armed with smoke bombs.
    
      Strong vs Infantry, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Reduces damage and sight of enemy units
    - Can kill garrisoned infantry

actor-ssnipe =
   .description = Special anti-infantry unit.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can kill garrisoned infantry
    
    Upgradeable with:
    - AP Bullets
    - High Caliber Rounds

actor-amob =
   .name = Angry Mob
   .description = Group of civilians armed with pistols and molotov coctails.
    
      Strong vs Infantry, Structures
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Slowly regains lost members
    
    Upgradeable with:
    - AP Bullets

actor-vlkv =
   .name = Vladislav Volkov
   .description = Elite cybernatic commando, armed with
    a tesla rifle.
    
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Cannot be eaten by Attack Dogs
    
      Maximum 1 can be trained.

actor-chit =
   .name = Chitzkoi
   .description = Cybernatic canine capable of destroying infantry and vehicles in seconds.
    Can be deployed to stun nearby infantry for a while.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can detect Stealth units
    - Can detect Spies
    - Cannot be eaten by Attack Dogs
    
      Maximum 1 can be trained.

## bakuvian-naval.yaml
actor-hydf =
   .name = Hydrofoil
   .description = Anti-Air and Anti-Infantry naval unit.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles, Naval units

## bakuvian-structures.yaml
actor-babarr =
   .description = Trains Bakuvian infantry.
    Can heal nearby infantry.
    
      Cannot be placed on water.
      Can be rotated.

actor-basops =
   .name = Special Operations Center
   .description = Provides radar.
    Researches basic upgrades.
    
    Provides Leaflet Drop support power.

actor-baairf =
   .name = Airfield
   .description = Produces Bakuvian aircraft.
    Supports 2 aircraft.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Can be rotated.

actor-baacdm =
   .name = Military Academy
   .description = Makes combat units trained as veteran.
    
      Maximum 1 can be built.

actor-bamort =
   .name = Mortar Turret
   .description = Long range defense structure armed with smoke bombs.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can detect Steath units
    - Reduces damage and sight of enemy units
    - Can kill garrisoned infantry
    
      Requires power to operate.

actor-baprop =
   .description = Buffs fire speed of nearby units and heals them.
    
      Requires power to operate.

## bakuvian-vehicles.yaml
actor-hytk =
   .name = Hydra Heavy Tank
   .description = Bakuvian Main Battle Tank with double barrel.
    
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Nuclear Engines
    - Mounted MG

actor-send =
   .name = Sentry Drone
   .description = Stealth drone armed with a heavy machine gun.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can detect Stealth units
    - Stealth while not moving or firing
    - Immune to Radiation
    - Cannot be mind controlled
    
    Upgradeable with:
    - Armor-Piercing Bullets

actor-smrj =
   .description = Jams nearby enemy radar domes and deflects incoming missiles.
    
      Unarmed
    
    Abilities:
    - Can detect Stealth units

actor-grad =
   .name = Grad MLRS
   .description = Long-range rocket artillery.
    
      Strong vs Buildings, Infantry
      Weak vs Aircraft
    
    Upgradeable with:
    - Cryo Warheads

actor-qyzyl =
   .name = Qızıl Ulduz Defense Platform
   .description = Heavy armored vehicle capable of making other nearby vehicles invulnerable.
    Damages nearby enemy infantry.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft

## bridges.yaml
actor-cabhut =
   .name = Bridge Repair Hut
meta-wooden-bridge =
   .name = Wooden Bridge
meta-concrete-bridge =
   .name = Concrete Bridge
meta-dead-bridge =
   .name = Dead Bridge
meta-bridge-ramp =
   .name = Bridge Ramp

## civilian-flags.yaml
actor-cadmfgl =
   .name = Flag
actor-causfgl =
   .name = American Flag
actor-caukfgl =
   .name = British Flag
actor-cafrfgl =
   .name = French Flag
actor-cagefgl =
   .name = German Flag
actor-caskfgl =
   .name = Korean Flag
actor-carufgl =
   .name = Soviet Flag
actor-cairfgl =
   .name = Iraqi Flag
actor-cavnfgl =
   .name = Vietnamese Flag
actor-cacufgl =
   .name = Cuban Flag
actor-calbfgl =
   .name = Libyan Flag
actor-capcfgl =
   .name = Yurigrad Flag
actor-caplfgl =
   .name = Lazarus Corps Flag
actor-capsfgl =
   .name = Antarctic Flag
actor-captfgl =
   .name = Transylvanian Flag
actor-capmfgl =
   .name = Lunar Eclipse Flag
actor-catcfgl =
   .name = Transcaucasian Flag
actor-catmfgl =
   .name = Turkmen Flag
actor-catvfgl =
   .name = Tuvan Flag
actor-carffgl =
   .name = Russian Flag
actor-casmfgl =
   .name = Serbo-Montenegrin Flag
actor-cajpfgl =
   .name = Japanese Flag
actor-cablfgl =
   .name = Belarusian Flag
actor-capofgl =
   .name = Polish Flag
actor-cauafgl =
   .name = Ukrainian Flag
actor-cachfgl =
   .name = Chinese Flag
actor-caaufgl =
   .name = Australian Flag
actor-catrfgl =
   .name = Turkish Flag
actor-cacnfgl =
   .name = Canadian Flag
actor-caclfgl =
   .name = Chilean Flag
actor-camxfgl =
   .name = Mexican Flag
actor-camofgl =
   .name = Mongolian Flag
actor-caarfgl =
   .name = Armenian SSR Flag
actor-caazfgl =
   .name = Azerbaijan SSR Flag
actor-cagofgl =
   .name = Georgian SSR Flag
actor-cakzfgl =
   .name = Kazakh SSR Flag
actor-cakyfgl =
   .name = Kyrgyz SSR Flag
actor-carsfgl =
   .name = Russian SFSR Flag
actor-catjfgl =
   .name = Tajik SSR Flag
actor-cauzfgl =
   .name = Uzbek SSR Flag
actor-caatfgl =
   .name = Austrian Flag
actor-cabefgl =
   .name = Belgian Flag
actor-cabrfgl =
   .name = Brazilian Flag
actor-cacyfgl =
   .name = Cypriot Flag
actor-caczfgl =
   .name = Czechoslovak Flag
actor-cadnfgl =
   .name = Danish Flag
actor-canlfgl =
   .name = Dutch Flag
actor-caesfgl =
   .name = Estonian Flag
actor-caphfgl =
   .name = Filipino Flag
actor-cafifgl =
   .name = Finnish Flag
actor-cagrfgl =
   .name = Greek Flag
actor-cahufgl =
   .name = Hungarian Flag
actor-cainfgl =
   .name = Indonesian Flag
actor-caeifgl =
   .name = Irish Flag
actor-caitfgl =
   .name = Italian Flag
actor-calafgl =
   .name = Latvian Flag
actor-calefgl =
   .name = Lebanese Flag
actor-califgl =
   .name = Lithuanian Flag
actor-calxfgl =
   .name = Luxembourgish Flag
actor-camlfgl =
   .name = Maltese Flag
actor-camrfgl =
   .name = Moroccan Flag
actor-canzfgl =
   .name = New Zealander Flag
actor-canwfgl =
   .name = Norwegian Flag
actor-caomfgl =
   .name = Omani Flag
actor-caslfgl =
   .name = Slovenian Flag
actor-caspfgl =
   .name = Spanish Flag
actor-cazafgl =
   .name = Zairian Flag
actor-caalfgl =
   .name = Albanian Flag
actor-cabmfgl =
   .name = Burmese Flag
actor-cacgfgl =
   .name = Congolese Flag
actor-cakmfgl =
   .name = Kampuchean Flag
actor-calofgl =
   .name = Laotian Flag
actor-cancfgl =
   .name = Nicaraguan Flag
actor-caprfgl =
   .name = Peruvian Flag
actor-casofgl =
   .name = Somalian Flag
actor-casyfgl =
   .name = Syrian Flag
actor-caymfgl =
   .name = Yemeni Flag
actor-capafgl =
   .name = Australian Psi-Corps Flag
actor-cabhfgl =
   .name = Bhutanese Flag
actor-cabufgl =
   .name = Bulgarian Flag
actor-cabunfgl =
   .name = Bulgarian Naval Ensign
actor-caicfgl =
   .name = Icelandic Flag
actor-cajrfgl =
   .name = Jordanian Flag
actor-cangfgl =
   .name = Nigerian Flag
actor-capnfgl =
   .name = Panamanian Flag
actor-capgfgl =
   .name = Portuguese Flag
actor-caswfgl =
   .name = Swedish Flag
actor-caszfgl =
   .name = Swiss Flag
actor-cahlfgl2 =
   .name = Angriverian Flag
actor-caeqfgl =
   .name = Equestrian Flag
actor-cahlfgl =
   .name = Herzlander Flag
actor-capvfgl =
   .name = Ponyvillian Flag

## civilian-naval.yaml
actor-tug =
   .name = Tug Boat
actor-cruise =
   .name = Cruise Ship
actor-cdest =
   .name = Coast Guard Boat
actor-vlad =
   .name = Vladamir's Dreadnought

## civilian-props.yaml
actor-camisc01 =
   .name = Barrels
actor-camisc02 =
   .name = Barrel
actor-camisc03 =
   .name = Dumpster
actor-camisc04 =
   .name = Mailbox
actor-camisc05 =
   .name = Pipes
actor-camisc06 =
   .name = V3 Ammunition
actor-camsc11 =
   .name = Tires
actor-camsc12 =
   .name = Practice Target
actor-camsc13 =
   .name = Derelict Tank
actor-ammocrat =
   .name = Ammo Crates
actor-camsc01 =
   .name = Hot Dog Stand
actor-camsc02 =
   .name = Beach Umbrellas
actor-camsc03 =
   .name = Beach Umbrellas
actor-camsc04 =
   .name = Beach Towels
actor-camsc05 =
   .name = Beach Towels
actor-camsc06 =
   .name = Camp Fire
actor-caeuro05 =
   .name = Statue
actor-capark01 =
   .name = Park Bench
actor-capark02 =
   .name = Swing Set
actor-capark03 =
   .name = Merry Go Round
actor-castrt01 =
   .name = Traffic Light
actor-castrt02 =
   .name = Traffic Light
actor-castrt03 =
   .name = Traffic Light
actor-castrt04 =
   .name = Traffic Light
actor-castrt05 =
   .name = Bus Stop
actor-camov01 =
   .name = Drive In Movie Screen
actor-camov02 =
   .name = Drive In Movie Concession Stand
actor-pole01 =
   .name = Utility Pole
actor-pole02 =
   .name = Utility Pole
actor-hdstn01 =
   .name = Alrington Stones
actor-spkr01 =
   .name = Drive-In Speaker
actor-carus02c =
   .name = Kremlin Walls
actor-carus02d =
   .name = Kremlin Walls
actor-carus02e =
   .name = Kremlin Walls
actor-carus02f =
   .name = Kremlin Walls
actor-cakrmw =
   .name = Kremlin Walls
actor-gagate-a =
   .name = Guard Border Crossing
actor-cabarr01 =
   .name = Barricade
actor-cabarr02 =
   .name = Barricade
actor-casin03e =
   .name = Construction Sign
actor-caurb01 =
   .name = Telephone Booth
actor-caurb02 =
   .name = Fire Hydrant
actor-caurb03 =
   .name = Spotlight
actor-cagatene =
   .name = Guard Border Crossing
actor-cagatenw =
   .name = Guard Border Crossing
actor-cagatesw =
   .name = Guard Border Crossing
actor-capark07 =
   .name = Picnic Table

## civilian-structures.yaml
actor-cafncb =
   .name = Black Fence
actor-cafncw =
   .name = White Fence
actor-cafnck =
   .name = Brown Fence
actor-cafncy =
   .name = Yellow Fence
actor-cafncg =
   .name = Green Fence
actor-cafncm =
   .name = Purple Fence
actor-cabarb =
   .name = Barbed Wire Fence
actor-gasand =
   .name = Sandbags
actor-cafncp =
   .name = Prison Camp Fence
actor-cawt01 =
   .name = Water Tower
actor-cats01 =
   .name = Twin Silos
actor-cabarn02 =
   .name = Barn
actor-cawash01 =
   .name = White House
actor-cawsh12 =
   .name = Washington Monument
actor-cawash14 =
   .name = Jefferson Memorial
actor-cawash15 =
   .name = Lincoln Memorial
actor-cawash16 =
   .name = Smithsonian Castle
actor-cawash17 =
   .name = Smithsonian Natural History Museum
actor-cawash18 =
   .name = Fountain
actor-cawash19 =
   .name = Iwo Jima Memorial
actor-canewy04 =
   .name = Statue of Liberty
actor-canewy05 =
   .name = World Trade Center
actor-canewy20 =
   .name = Warehouse
actor-canewy21 =
   .name = Warehouse
actor-caarmy01 =
   .name = Army Tent
actor-caarmy02 =
   .name = Army Tent
actor-caarmy03 =
   .name = Army Tent
actor-caarmy04 =
   .name = Army Tent
actor-cafarm01 =
   .name = Farm
actor-cafarm02 =
   .name = Farm Silo
actor-cafarm06 =
   .name = Lighthouse
actor-cacolo01 =
   .name = Air Force Academy Chapel
actor-caind01 =
   .name = Factory
actor-calab =
   .name = Einstein's Lab
actor-cagas01 =
   .name = Gas Station
actor-galite =
   .name = Light Post
actor-city05 =
   .name = Battersea Power Station
actor-catech01 =
   .name = Communications Center
actor-catexs02 =
   .name = Alamo
actor-capars01 =
   .name = Eiffel Tower
actor-capars07 =
   .name = Phone Booth
actor-capars10 =
   .name = Bistro
actor-capars11 =
   .name = Arc de Triumphe
actor-capars12 =
   .name = Notre Dame
actor-capars13 =
   .name = Bistro
actor-capars14 =
   .name = Bistro
actor-cafrma =
   .name = Farm House
actor-cafrmb =
   .name = Outhouse
actor-caprs03 =
   .name = Louvre
actor-cagard01 =
   .name = Guard Shack
actor-cagard02 =
   .name = Guard Shack
actor-carus01 =
   .name = St. Basil's Cathedral
actor-carus02a =
   .name = Kremlin Walls
actor-carus02b =
   .name = Kremlin Walls
actor-carus02g =
   .name = Kremlin Wall Clock Tower
actor-carus03 =
   .name = Kremlin Palace
actor-camiam04 =
   .name = Lifeguard Hut
actor-camiam08 =
   .name = Arizona Memorial
actor-camex01 =
   .name = Mayan Pyramid
actor-camex02 =
   .name = Mayan Castillo
actor-camex03 =
   .name = Mayan Minor Temple
actor-camex04 =
   .name = Mayan Large Temple
actor-camex05 =
   .name = Mayan Platfrom
actor-caeur1 =
   .name = Cottage
actor-caeur2 =
   .name = Cottage
actor-cachig04 =
   .name = Associates Center
actor-cachig05 =
   .name = Sears Tower
actor-cachig06 =
   .name = Water Tower
actor-castl05a =
   .name = Stadium
actor-castl05b =
   .name = Stadium
actor-castl05c =
   .name = Stadium
actor-castl05d =
   .name = Stadium
actor-castl05e =
   .name = Stadium
actor-castl05f =
   .name = Stadium
actor-castl05g =
   .name = Stadium
actor-castl05h =
   .name = Stadium
actor-camsc07 =
   .name = Hut
actor-camsc08 =
   .name = Hut
actor-camsc09 =
   .name = Hut
actor-camsc10 =
   .name = McBurger Kong
actor-cabunk01 =
   .name = Concrete Bunker
actor-cabunk02 =
   .name = Concrete Bunker
actor-cabunk03 =
   .name = Concrete Bunker
actor-cabunk04 =
   .name = Concrete Bunker
actor-cala03 =
   .name = Hollywood Sign
actor-cala04 =
   .name = Hollywood Bowl
actor-cala05 =
   .name = LAX
actor-cala06 =
   .name = LAX Control Tower
actor-cala07 =
   .name = Movie Theater
actor-cala08 =
   .name = Car Dealership
actor-cala09 =
   .name = Convenience Store
actor-cala10 =
   .name = Billboard
actor-cala11 =
   .name = Hollywood Bowl
actor-cala12 =
   .name = Hollywood Bowl
actor-cala13 =
   .name = Hollywood Sign
actor-cala14 =
   .name = Mini Mall
actor-cala15 =
   .name = Mini Mall
actor-caegyp01 =
   .name = Pyramid
actor-caegyp02 =
   .name = Pyramid
actor-caegyp03 =
   .name = Sphinx
actor-caegyp04 =
   .name = Pyramid
actor-caegyp05 =
   .name = Pyramid
actor-caegyp06 =
   .name = Pyramid
actor-calond04 =
   .name = British Parlaiment
actor-calond05 =
   .name = Big Ben
actor-calond06 =
   .name = Tower of London
actor-casanf04 =
   .name = Golden Gate Bridge
actor-casanf05 =
   .name = Alcatraz
actor-casanf09 =
   .name = Golden Gate Bridge
actor-casanf10 =
   .name = Golden Gate Bridge
actor-casanf11 =
   .name = Golden Gate Bridge
actor-casanf12 =
   .name = Golden Gate Bridge
actor-casanf13 =
   .name = Golden Gate Bridge
actor-casanf14 =
   .name = Golden Gate Bridge
actor-casanf15 =
   .name = Alcatraz Water Tower
actor-casanf16 =
   .name = Light House
actor-casydn02 =
   .name = McRoo Burger
actor-casydn03 =
   .name = Sydney Opera House

actor-calunr01 =
   .name = Lunar Lander
   .dune-name = Dunar Lander

actor-calunr02 =
   .name = American Flag
actor-caseat01 =
   .name = Seattle Space Needle
actor-caseat02 =
   .name = MassiveSoft Campus
actor-catran01 =
   .name = Crypt
actor-catran02 =
   .name = Crypt
actor-catran03 =
   .name = Yuri's Fortress
actor-catime01 =
   .name = Time Machine
actor-catime02 =
   .name = Time Machine
actor-caeast01 =
   .name = Moai

## civilian-vehicles.yaml
actor-bus =
   .name = School Bus
actor-limo =
   .name = Limousine
actor-pick =
   .name = Pickup Truck
actor-car =
   .name = Automobile
actor-wini =
   .name = Recreational Vehicle
actor-propa =
   .name = Propaganda Truck
actor-cop =
   .name = Police Car
actor-euroc =
   .name = Automobile
actor-cona =
   .name = Excavator
actor-trucka =
   .name = Truck
actor-truckb =
   .name = Truck
actor-suvb =
   .name = Automobile
actor-suvw =
   .name = Automobile
actor-stang =
   .name = Automobile
actor-ptruck =
   .name = Pickup Truck
actor-taxi =
   .name = Taxi
actor-ambu =
   .name = Ambulance
actor-bcab =
   .name = Black Cab
actor-cblc =
   .name = Cable Car
actor-ddbx =
   .name = Bus
actor-doly =
   .name = Camera Dolly
actor-ftrk =
   .name = Fire Truck
actor-jeep =
   .name = Pickup Truck
actor-ycab =
   .name = Yellow Cab
actor-civp =
   .name = Passenger Plane
actor-truckc =
   .name = Truck
actor-car2 =
   .name = Automobile
actor-car3 =
   .name = Automobile
actor-mixer =
   .name = Cement Mixer
actor-flata =
   .name = Missile Truck
actor-flatb =
   .name = Empty Truck

## civilians.yaml
actor-vladimir =
   .name = General Vladimir
actor-pentgen =
   .name = General
actor-ssrv =
   .name = Secret Service
actor-pres =
   .name = President Michael Dugan
actor-rmnv =
   .name = Premier Alexander Romanov
actor-eins =
   .name = Professor Albert Einstein
actor-mumy =
   .name = Mummy
actor-myak =
   .name = Chairman Mustapha Yakubov
actor-rainbow-dash =
   .name = Rainbow Dash
actor-derpy-hooves =
   .name = Derpy Hooves
actor-yuripr-regicide =
   .name = Master Yuri

## cpowers.yaml
actor-commanders-power-ifv-training =
   .name = IFV Training
   .description = Makes IFVs train as veteran.

actor-commanders-power-harrier-training =
   .name = Aircraft Training
   .description = Makes Harriers train as elite and other Allied Planes train as veteran.

actor-commanders-power-inf-training =
   .name = Infantry Training
   .description = Makes Conscripts and Flak Troopers train as veteran.

actor-commanders-power-gatling-training =
   .name = Gatling Training
   .description = Makes Gatling Tanks, Gatling Submarines and Gatling Troopers train as veteran.

actor-commanders-power-arty-training =
   .name = Artillery Training
   .description = Makes Mortar Troopers and Grad MLRSes train as veteran.

actor-commanders-power-airpad =
   .name = Airpad
   .description = Enables construction of Airpads from Construction Yard.

actor-commanders-power-gun-turret =
   .name = Gun Turret
   .description = Enables construction of Gun Turret from Construction Yard.

actor-commanders-power-tesla-fence =
   .name = Tesla Fence
   .description = Enables construction of Tesla Fence from Construction Yard.

actor-commanders-power-grinder =
   .name = Grinder
   .description = Enables construction of Grinder from Construction Yard.

actor-commanders-power-medic =
   .name = Medic
   .description = Enables training of Medic from Barracks.

actor-commanders-power-ambulance =
   .name = Combat Ambulance
   .description = Enables production of Combat Ambulance from War Factory.

actor-commanders-power-hijacker =
   .name = Hijacker
   .description = Enables training of Hijacker from Barracks.

actor-commanders-power-havoc =
   .name = Havoc Attack Helicopter
   .description = Enables training of Havoc Attack Helicopter from Helipad.

actor-commanders-power-minelayer =
   .name = Minelayer
   .description = Enables production of Minelayer from War Factory.

actor-commanders-power-annihilator-artillery =
   .name = Annihilator Artillery
   .description = Enables production of Annihilator Artillery from War Factory.

actor-commanders-power-mechanic =
   .name = Mechanic
   .description = Enables training of Mechanic from Barracks.

actor-commanders-power-intel-drop =
   .name = Intel Drop
   .description = Enables Intel Drop power.

actor-commanders-power-drone-drop =
   .name = Drone Drop
   .description = Enables Drone Drop power.

actor-commanders-power-cash-bounty =
   .name = Cash Bounty
   .description = Enables Cash Bounty power.

actor-commanders-power-scout-drone =
   .name = Scout Drone
   .description = Enables Scout Drone power.

actor-commanders-power-spy-plane =
   .name = Spy Plane
   .description = Enables Spy Plane power.

actor-commanders-power-psychic-reveal =
   .name = Psychic Reveal
   .description = Enables Psychic Reveal power.

actor-commanders-power-leaflet-drop =
   .name = Leaflet Drop
   .description = Enables Leaflet Drop power.

actor-commanders-power-cluster-mines =
   .name = Cluster Mines
   .description = Enables Cluster Mines power.

actor-commanders-power-brute-drop =
   .name = Brute Drop
   .description = Enables Brute Drop power.

actor-commanders-power-emergency-repair-1 =
   .name = Emergency Repair - Level 1
   .description = Enables Emergency Repair.

actor-commanders-power-emergency-repair-2 =
   .name = Emergency Repair - Level 2
   .description = Upgrades Emergency Repair power to Level 2.

actor-commanders-power-emergency-repair-3 =
   .name = Emergency Repair - Level 3
   .description = Upgrades Emergency Repair power to Level 3.

actor-commanders-power-a10-strike-1 =
   .name = A-10 Strike - Level 1
   .description = Enables A-10 Strike power.

actor-commanders-power-a10-strike-2 =
   .name = A-10 Strike - Level 2
   .description = Upgrades A-10 Strike power to Level 2.

actor-commanders-power-a10-strike-3 =
   .name = A-10 Strike - Level 3
   .description = Upgrades A-10 Strike power to Level 3.

actor-commanders-power-parabombs-1 =
   .name = Parabombs - Level 1
   .description = Enables Parabombs power.

actor-commanders-power-parabombs-2 =
   .name = Parabombs - Level 2
   .description = Upgrades Parabombs power to Level 2.

actor-commanders-power-parabombs-3 =
   .name = Parabombs - Level 3
   .description = Upgrades Parabombs power to Level 3.

actor-commanders-power-toxin-bombing-1 =
   .name = Toxin Bombing - Level 1
   .description = Enables Toxin Bombing power.

actor-commanders-power-toxin-bombing-2 =
   .name = Toxin Bombing - Level 2
   .description = Upgrades Toxin Bombing power to Level 2.

actor-commanders-power-toxin-bombing-3 =
   .name = Toxin Bombing - Level 3
   .description = Upgrades Toxin Bombing power to Level 3.

actor-commanders-power-fuel-air-bomb-1 =
   .name = Fuel Air Bomb - Level 1
   .description = Enables Fuel Air Bomb power.

actor-commanders-power-fuel-air-bomb-2 =
   .name = Fuel Air Bomb - Level 2
   .description = Upgrades Fuel Air Bomb power to Level 2.

actor-commanders-power-fuel-air-bomb-3 =
   .name = Fuel Air Bomb - Level 3
   .description = Upgrades Fuel Air Bomb power to Level 3.

actor-commanders-power-chrono-boost-1 =
   .name = Chrono Boost - Level 1
   .description = Enables Chrono Boost power.

actor-commanders-power-chrono-boost-2 =
   .name = Chrono Boost - Level 2
   .description = Upgrades Chrono Boost power to Level 2.

actor-commanders-power-chrono-boost-3 =
   .name = Chrono Boost - Level 3
   .description = Upgrades Chrono Boost power to Level 3.

actor-commanders-power-propaganda-1 =
   .name = Propaganda - Level 1
   .description = Enables Propaganda power.

actor-commanders-power-propaganda-2 =
   .name = Propaganda - Level 2
   .description = Upgrades Propaganda power to Level 2.

actor-commanders-power-propaganda-3 =
   .name = Propaganda - Level 3
   .description = Upgrades Propaganda power to Level 3.

actor-commanders-power-magnetic-beam-1 =
   .name = Magnetic Beam - Level 1
   .description = Enables Magnetic Beam power.

actor-commanders-power-magnetic-beam-2 =
   .name = Magnetic Beam - Level 2
   .description = Upgrades Magnetic Beam power to Level 2.

actor-commanders-power-magnetic-beam-3 =
   .name = Magnetic Beam - Level 3
   .description = Upgrades Magnetic Beam power to Level 3.

actor-commanders-power-cruiser-strike =
   .name = Cruiser Strike
   .description = Enables Crusiser Strike power.

actor-commanders-power-v3-storm =
   .name = V3 Storm
   .description = Enables V3 Storm power.

actor-commanders-power-orbital-drop =
   .name = Orbital Drop
   .description = Enables Orbital Drop power.

actor-commanders-power-em-pulse-strike =
   .name = E.M. Pulse Strike
   .description = Enables E.M. Pulse Strike power.

actor-commanders-power-fallout-bomb =
   .name = Fallout Bomb
   .description = Enables Fallout Bomb power.

actor-commanders-power-chaos-gas-drop =
   .name = Chaos Gas Drop
   .description = Enables Chaos Gas Drop power.

actor-commanders-power-cryo-bomb =
   .name = Cryo Bomb
   .description = Enables Cryo Bomb power.

actor-prerequisite-has-points-name = 1 Commander's Points
actor-prerequisite-3-stars-name = Major or Higher Rank
actor-prerequisite-5-stars-name = General Rank
meta-default-commanders-power-prerequisite-name = Commander's Power

## debug-structures.yaml
actor-cacnst =
   .name = Debug Construction Yard
   .civilian-name = Civilian Construction Yard
   .tech-name = Tech Construction Yard
   .unused-name = Debug Construction Yard
   .description = Debug structure to allow building Construction Yard of other (sub)factions.

actor-gacnst =
   .america-name = American Construction Yard
   .england-name = British Construction Yard
   .france-name = French Construction Yard
   .germany-name = German Construction Yard
   .korea-name = Korean Construction Yard
   .japan-name = Japanese Construction Yard
   .belarus-name = Belarusian Construction Yard
   .poland-name = Polish Construction Yard
   .ukraine-name = Ukrainian Construction Yard
   .aussie-name = Australian Construction Yard
   .china-name = Chinese Construction Yard
   .turkey-name = Turkish Construction Yard
   .canada-name = Canadian Construction Yard

actor-nacnst =
   .russia-name = Soviet Construction Yard
   .iraq-name = Iraqi Construction Yard
   .vietnam-name = Vietnamese Construction Yard
   .cuba-name = Cuban Construction Yard
   .libya-name = Libyan Construction Yard
   .chile-name = Chilean Construction Yard
   .mexico-name = Mexican Construction Yard
   .mongolia-name = Mongolian Construction Yard
   .transcaucus-name = Transcaucasian Construction Yard
   .turkmen-name = Turkmen Construction Yard
   .tuva-name = Tuvan Construction Yard
   .russianfed-name = Russian Construction Yard
   .serbia-name = Serbo-Montenegrin Construction Yard

actor-yacnst =
   .psicorps-name = Yurigrad Construction Yard
   .psinepal-name = Lazarus Construction Yard
   .psisouth-name = Antarctic Construction Yard
   .psitrans-name = Transylvanian Construction Yard
   .psimoon-name = Lunar Construction Yard

actor-cabrck =
   .name = Civilian Barracks
   .unused-name = Debug Barracks
   .unused-description = For unused infantry.

actor-caweap =
   .name = Civilian Factory
   .unused-name = Debug Factory
   .unused-description = For unused vehicles.

actor-cayard =
   .name = Civilian Shipyard

## default-naval.yaml
meta-amphibioustransport =
   .name = Amphibious Transport
   .description = General-purpose naval transport.
    Can carry infantry and vehicles.
    
      Unarmed

meta-seaanimal =
   .name = Sea Animal

## default-structures.yaml
meta-constructionyard =
   .name = Construction Yard
   .description = Allows construction of base structures.

meta-expansionnode =
   .name = Expansion Node

meta-powerplant =
   .name = Power Plant
   .description = Provides power for other structures.

meta-barracks =
   .name = Barracks
   .description = Trains infantry.
    Can heal nearby infantry.
    
      Cannot be placed on water.
      Can be rotated.

meta-refinery =
   .name = Ore Refinery
   .description = Processes ore into credits.
    
      Can be rotated.

meta-silo =
   .name = Ore Silo
   .description = Stores excess Ore.

meta-warfactory =
   .name = War Factory
   .description = Produces vehicles.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Cannot be placed on water.
      Can be rotated.

meta-shipyard =
   .name = Naval Yard
   .description = Produces ships, submarines, and other naval units.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Can only be placed on water.

meta-servicedepot =
   .name = Service Depot
   .description = Repairs vehicles and removes Terror Drones for a price.

meta-radar =
   .description = Provides radar.
    Researches basic upgrades.
    
    Provides a different support power depening on the subfaction.

meta-helipad =
   .description = Produces aircraft.
    
    Abilities:
    - Comes with 1 repair drone.

meta-battlelab =
   .name = Battle Lab
   .description = Allows deployment of advanced units.
    Researches advanced upgrades.
    
      Can be rotated.

actor-power-name = Power Plant
actor-refinery-name = Ore Refinery
actor-barracks-name = Infantry Production
actor-radar-name = Radar
actor-warfactory-name = Vehicle Production
actor-production-name = Unit Production
actor-repairpad-name = Service Depot

## default-vehicles.yaml
meta-miner =
   .name = Ore Miner
   .description = Gathers Ore and Gems.
    
      Unarmed
    
    Abilities:
    - Can move over water
    - Cannot be mind controlled

meta-constructionvehicle =
   .name = Mobile Construction Vehicle
   .description = Deploys into a Construction Yard.
    
      Unarmed
    
    Abilities:
    - Can move over water

meta-expansionvehicle =
   .name = Mobile Expansion Vehicle
   .description = Deploys into an Expansion Node.
    Cannot be undeployed back.
    
      Unarmed
    
    Abilities:
    - Can move over water

meta-mainbattletank =
   .description = Main Battle Tank.
    
      Strong vs Vehicles
      Weak vs Infantry, Aircraft

## defaults.yaml
meta-building =
   .name = Structure
meta-civbuilding =
   .name = Civilian Building
meta-flag =
   .name = Flag
meta-wall =
   .name = Wall
meta-fence =
   .name = Fence

meta-gate =
   .name = Gate
   .description = Automated barrier that opens for allied units.
    
      Cannot be placed on water.
      Can be rotated.

meta-infantry =
   .name = Soldier
meta-civilianinfantry =
   .name = Civilian

meta-pegasus =
   .name = Pegasus
   .generic-name = Pony

meta-vehicle =
   .name = Vehicle
meta-aircraft =
   .name = Aircraft
meta-shootablemissile =
   .name = Missile
meta-ship =
   .name = Ship
meta-oredrill =
   .name = Ore Drill
meta-tree =
   .name = Tree
meta-streetsign =
   .name = Street Sign
meta-trafficlight =
   .name = Traffic Light
meta-streetlight =
   .name = Street Light
meta-telephonepole =
   .name = Utility Pole
meta-rock =
   .name = Rock
meta-crate =
   .name = Crate

## misc.yaml
actor-ambient-bird-jungle-1-name = Jungle Bird Ambient Sound 1
actor-ambient-bird-jungle-2-name = Jungle Bird Ambient Sound 2
actor-ambient-bird-morning-name = Morning Bird Ambient Sound
actor-ambient-bird-park-name = Park Bird Ambient Sound
actor-ambient-bird-temperate-1-name = Temperate Bird Ambient Sound 1
actor-ambient-bird-temperate-2-name = Temperate Bird Ambient Sound 2
actor-ambient-cricket-1-name = Cricket Ambient Sound 1
actor-ambient-cricket-2-name = Cricket Ambient Sound 2
actor-ambient-cricket-3-name = Cricket Ambient Sound 3
actor-ambient-hawk-name = Hawk Ambient Sound
actor-ambient-seagull-1-name = Seagull Ambient Sound 1
actor-ambient-seagull-2-name = Seagull Ambient Sound 2
actor-ambient-owl-name = Owl Ambient Sound
actor-ambient-river-name = River Ambient Sound
actor-ambient-traffic-name = Traffic Ambient Sound
actor-ambient-urban-1-name = Urban Ambient Sound 1
actor-ambient-urban-2-name = Urban Ambient Sound 2
actor-ambient-wave-1-name = Wave Ambient Sound 1
actor-ambient-wave-2-name = Wave Ambient Sound 2
actor-ambient-wave-3-name = Wave Ambient Sound 3
actor-ambient-wind-1-name = Wind Ambient Sound 1
actor-ambient-wind-2-name = Wind Ambient Sound 2
actor-camera-name = (reveals area to owner)
actor-sonar-name = (support power proxy camera)
actor-camera-satscan-name = Satellite Scan
actor-magnetic-beam-1-name = Magnetic Beam
meta-lamppost-name = (Invisible Light Post)
actor-galite-white-name = (Invisible Light Post)
actor-galite-black-name = (Invisible Negative Light Post)
actor-galite-red-name = (Invisible Red Light Post)
actor-galite-cyan-name = (Invisible Negative Red Light Post)
actor-galite-green-name = (Invisible Green Light Post)
actor-galite-blue-name = (Invisible Blue Light Post)
actor-galite-yellow-name = (Invisible Yellow Light Post)
actor-galite-orange-name = (Invisible Orange Light Post)
actor-galite-purple-name = (Invisible Purple Light Post)
actor-galite-morning-temp-name = (Invisible Temperate Morning Light Post)
actor-galite-day-temp-name = (Invisible Temperate Day Light Post)
actor-galite-dusk-temp-name = (Invisible Temperate Dusk Light Post)
actor-galite-night-temp-name = (Invisible Temperate Night Light Post)
actor-galite-morning-snow-name = (Invisible Snow Morning Light Post)
actor-galite-day-snow-name = (Invisible Snow Day Light Post)
actor-galite-dusk-snow-name = (Invisible Snow Dusk Light Post)
actor-galite-dusk-night-name = (Invisible Snow Night Light Post)

## old-vehicles.yaml
actor-1tnk =
   .name = Light Tank
actor-2tnk =
   .name = Medium Tank
actor-3tnk =
   .name = Heavy Tank
actor-4tnk =
   .name = Mammoth Tank
actor-arty =
   .name = Artillery
actor-ftnktd =
   .name = Flame Tank
actor-hmve =
   .name = Humvee
actor-m113 =
   .name = APC
actor-mlrs =
   .name = Rocket Launcher
actor-mnly =
   .name = Minelayer
actor-mrj =
   .name = Mobile Radar Jammer

## soviet-infantry.yaml
actor-e2 =
   .name = Conscript
   .description = Cheap rifle infantry.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Upgradeable with:
    - Bullet-Proof Coats
    - Molotov Cocktails
    - Armor-Piercing Bullets

actor-flakt =
   .name = Flak Trooper
   .description = Anti-Air and anti-Infantry unit.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Upgradeable with:
    - Bullet-Proof Coats

actor-shk =
   .name = Tesla Trooper
   .description = Special armored unit using electricity.
    
      Strong vs Infantry, Tanks
      Weak vs Aircraft
    
    Abilities:
    - Can charge Tesla Coils
    
    Upgradeable with:
    - Overcharge (Soviet Union)

actor-terror =
   .name = Terrorist
   .description = Carries C4 charges taped to his body and kamikazes enemies
    blowing them up quickly and efficiently.
    
      Strong vs Ground units
      Weak vs Aircraft
    
    Upgradeable with:
    - Targeted Explosives (Cuba)

actor-deso =
   .name = Desolator
   .description = Carries a radiation-emitting weapon.
    Can deploy for area-of-effect damage.
    
      Strong vs Infantry, Light vehicles
      Weak vs Tanks, Aircraft
    
    Abilities:
    - Immune to Radiation
    
    Upgradeable with:
    - Advanced Irradiators (Iraq)

actor-ivan =
   .name = Crazy Ivan
   .description = Specialist for explosives. Can plant a Bomb on anything, even Cows.
    
    Upgradeable with:
    - Targeted Explosives (Cuba)
    - High Explosive Bombs (Libya)

actor-civan =
   .name = Chrono Ivan
   .description = Specialist for explosives. Can plant a Bomb on anything, even Cows.
    
    Abilities:
    - Can teleport anywhere on the map
    
    Upgradeable with:
    - High Explosive Bombs (Libya)

actor-yuri =
   .name = Yuri Clone
   .description = Psychic infantry. Can mind control enemy units.
    Can be deployed to unleash a powerful psychic wave.
    
      Strong vs Infantry, Vehicles
      Weak vs Terror Drones, Aircraft, Buildings

actor-yuripr =
   .name = Yuri Prime
   .description = Psychic infantry. Can mind control enemy units from a great range.
    Can be deployed to unleash a powerful psychic wave.
    
      Strong vs Infantry, Vehicles
      Weak vs Terror Drones, Aircraft, Buildings
    
      Maximum 1 can be trained.

actor-boris =
   .name = Boris Bukov
   .description = Elite commando infantry, armed with
    AKM rifle and signal flare.
    
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Can call airstrike against Buildings
    - Can move over water
    
    Upgradeable with:
    - Armor-Piercing Bullets
    
      Maximum 1 can be trained.

actor-pyro =
   .name = Flamethrower
   .description = Advanced anti-infantry and anti-structure infantry.
    
      Strong vs Buildings, Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can kill garrisoned infantry

actor-hjck =
   .name = Hijacker
   .description = Steals enemy vehicles.
    Can infiltrate enemy Battle Labs to gain access to new infantry.
    
      Unarmed
    
    Abilities:
    - Can swim
    - Stealth while not moving

actor-itrp =
   .name = Iron Trooper
   .description = High-tech soldier capable of making friendly vehicles invulnerabile and
    damage enemy infantry.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Cannot be eaten by Attack Dogs

actor-shkc =
   .name = Tesla Commando
   .description = Advanced armored unit with a Tesla Suit and Tesla Bombs.
    
      Strong vs Infantry, Tanks, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Can charge Tesla Coils
    - Can place Tesla Bombs on buildings
    
    Upgradeable with:
    - Overcharge (Soviet Union)

## soviet-naval.yaml
meta-submarine =
   .generic-name = Submarine

actor-sub =
   .name = Typhoon Attack Submarine
   .description = Submerged anti-ship unit armed with torpedoes.
    
      Strong vs Ships
      Weak vs Ground units, Aircraft
    
    Abilities:
    - Can detect Stealth units

actor-hyd =
   .name = Sea Scorpion
   .description = Anti-Air and Anti-Infantry naval unit.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles, Naval units

actor-sqd =
   .name = Giant Squid
   .generic-name = Squid
   .description = Large ocean creature capable of grabbing and sinking ships.
    
      Strong vs Ships
    
    Abilities:
    - Can be deployed to remove other Squids from nearby ships
    - Can detect Stealth units

actor-dred =
   .name = Dreadnought
   .description = Long-range rocket artillery ship.
    
      Strong vs Buildings, Infantry
      Weak vs Ships, Aircraft
    
    Upgradeable with:
    - Radioactive Warheads
    - Advanced Irradiators (Iraq)
    - High Explosive Bombs (Libya)

actor-dmisl =
   .name = Dreadnought Missile

actor-sray =
   .name = Stingray
   .description = Light ship armed with dual Tesla Coils.
    Can move on land.
    
      Strong vs Infantry, Vehicles, Ships
      Weak vs Aircraft
    
    Upgradeable with:
    - Overcharge (Soviet Union)

## soviet-structures.yaml
actor-napowr =
   .name = Tesla Reactor
   .description = Provides power for other structures.
    
      Can be rotated.

actor-nahand =
   .description = Trains Soviet infantry.
    Can heal nearby infantry.
    
      Cannot be placed on water.

actor-naradr =
   .name = Radar Tower
   .description = Provides radar.
    Researches basic upgrades.
    
    Provides a different support power depening on the subfaction:
    - Tesla Drop (Soviet Union)
    - Radiation Missile (Iraq)
    - Instant Bunker (Vietnam)
    - Death Bombs (Cuba)
    - Ambush (Libya)

actor-mine =
   .name = Mine

actor-naweap =
   .description = Produces Soviet vehicles.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Cannot be placed on water.
      Can be rotated.

actor-nayard =
   .description = Produces Soviet ships, submarines, and other naval units.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Can only be placed on water.

actor-nmine =
   .name = Naval Mine

actor-nadept =
   .description = Repairs vehicles and removes Terror Drones for a price.
    
      Can be rotated.

actor-naheli =
   .name = Helipad
   .description = Produces Soviet aircraft.
    
    Abilities:
    - Comes with 1 repair drone.

actor-nanrct =
   .name = Nuclear Reactor
   .description = Provides a large amount of power for other structures.
    
    Upgradeable with:
    - Advanced Irradiators (Iraq)
    
      Can be rotated.

actor-natech =
   .description = Allows deployment of advanced units.

actor-naclon =
   .name = Cloning Vats
   .description = Clones most trained infantry.
    
      Cannot be placed on water.
      Maximum 1 can be built.

actor-napsis =
   .name = Psychic Sensor
   .description = Detects enemy units and structures.
    
      Requires power to operate.

actor-nairon =
   .name = Iron Curtain Device
   .description = Grants invulnerability to vehicles and structures.
    Kills infantry.
    
      Requires power to operate.

actor-namisl =
   .name = Nuclear Missile Silo
   .description = Provides an atomic bomb.
    
    Upgradeable with:
    - Advanced Irradiators (Iraq)
    
      Requires power to operate.

actor-nawall =
   .name = Soviet Wall
   .description = Heavy wall capable of blocking units and projectiles.
    
      Cannot be placed on water.
 
actor-naflak =
   .name = Flak Cannon
   .description = Automated anti-aircraft defense.
    
      Strong vs Aircraft
      Weak vs Ground units
    
    Abilities:
    - Can detect Steath units
    
      Requires power to operate.

actor-tesla =
   .name = Tesla Coil
   .description = Advanced base defense.
    Can be buffed or made work during low power by Tesla Troopers.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Overcharge (Russia)
    
      Requires power to operate.

actor-nalasr =
   .name = Sentry Gun
   .description = Automated anti-infantry defense.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Armor-Piercing Bullets
 
actor-nabnkr =
   .name = Battle Bunker
   .description = Static defense with fireports for 6 garrisoned soldiers.
    Infantry inside cannot be killed by garrison killers.
    Comes with a Conscript inside.
    
      Cannot be placed on water.

actor-naindp =
   .name = Industrial Plant
   .description = Decreases vehicle costs by 25%.
    
      Maximum 1 can be built.

actor-nagate =
   .name = Soviet Gate

actor-napost =
   .name = Tesla Fence Post
   .description = Defence system that creates an electric fence between 2 fence posts.
    Posts also act as small Tesla Coils.
    Can be buffed or made work during low power by Tesla Troopers.
    
    Upgradeable with:
    - Overcharge (Russia)
    
      Cannot be placed on water.
      Requires power to operate.

actor-nafnce =
   .name = Tesla Fence

actor-naprop =
   .name = Propaganda Tower
   .description = Buffs fire speed of nearby units and heals them.
    
      Requires power to operate.

## soviet-vehicles.yaml
actor-harv =
   .name = War Miner
   .description = Gathers Ore and Gems.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can move over water
    - Cannot be mind controlled
    
    Upgradeable with:
    - Armor-Piercing Bullets

actor-dron =
   .name = Terror Drone
   .description = Small vehicle that can infect enemy vehicles and slowly kill them.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Immune to Radiation
    - Cannot be mind controlled

actor-htk =
   .name = Flak Track
   .description = Anti-Air and Anti-Infantry vehicle capable of transporting Infantry.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Abilities:
    - Can detect Stealth units

actor-htnk =
   .name = Rhino Heavy Tank
   .description = Soviet Main Battle Tank.
    
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Nuclear Engines
    - Uranium Shells
    - Advanced Irradiators (Iraq)

actor-apoc =
   .name = Apocalypse Tank
   .description = Soviet Advanced Battle Tank with Double Barrel
    and Anti-Aircraft Missile Launcher.
    
      Strong vs Vehicles, Aircraft
      Weak vs Infantry
    
    Abilities:
    - Can crush enemy vehicles
    
    Upgradeable with:
    - Nuclear Engines
    - Uranium Shells
    - Advanced Irradiators (Iraq)

actor-ttnk =
   .name = Tesla Tank
   .description = Russian special tank armed with dual small Tesla Coils.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Upgradeable with:
    - Overcharge (Soviet Union)

actor-dtruck =
   .name = Demolition Truck
   .description = Demolition Truck, actively armed with nuclear explosives.
    
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Aircraft
    
    Upgradeable with:
    - High Explosive Bombs (Libya)

actor-v3 =
   .name = V3 Launcher
   .description = Long-range rocket artillery.
    
      Strong vs Buildings, Infantry
      Weak vs Aircraft
    
    Upgradeable with:
    - Radioactive Warheads
    - Advanced Irradiators (Iraq)
    - High Explosive Bombs (Libya)

actor-m3 =
   .name = Meme3 Launcher
actor-v3rocket =
   .name = V3 Rocket
actor-m3rocket =
   .name = Meme3 Rocket

actor-tric =
   .name = Mortar Tricycle
   .description = Fast medium range unit.
    
      Strong vs Buildings, Infantry
      Weak vs Vehicles, Aircraft
    
    Upgradeable with:
    - High Explosive Bombs (Libya)

actor-deva =
   .name = Devastator
   .description = Soviet Advanced Battle Tank with Double Barrel
    armed with nuclear warheads
    
      Strong vs Vehicles, Infantry
      Weak vs Aircraft
    
    Abilities:
    - Immune to Radiation
    - Can be deployed to self-destruct and explode violently
    
    Upgradeable with:
    - Advanced Irradiators (Iraq)

actor-ftnk =
   .name = Flame Tank
   .description = Soviet tank armed with dual flamethrowers.
    
      Strong vs Buildings, Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can kill garrisoned infantry
    
    Upgradeable with:
    - Black Napalm

## tech-structures.yaml
actor-caoild =
   .name = Tech Oil Derrick
   .description = Periodically provides cash.
actor-caoild-mwspawner =
   .name = Megawealth Only Oil Derrick
actor-caoild-nonmwspawner =
   .name = Non-Megawealth Only Oil Derrick
actor-caairp =
   .name = Tech Airport
   .description = Provides Paradrop support power.
actor-cahosp =
   .name = Tech Hospital
   .description = Allows infantry to self-heal.
actor-caoutp =
   .name = Tech Outpost
   .description-1 = Provides repairing ground for vehicles.
   .description-2 = Armed with a missile launcher.
   .description-3 = Provides build area.
actor-capowr =
   .name = Tech Power Plant
   .description = Provides 400 power.
actor-camach =
   .name = Tech Machine Shop
   .description = Allows vehicles to self-repair.
actor-caslab =
   .name = Tech Secret Lab
   .description = Allow construction of a new 3rd tier vehicle.
actor-cacomm =
   .name = Tech Communications Center
   .description = Reveals a large area around it.
actor-caarmr =
   .name = Tech Armory
   .description = Increases the rate which units are promoted.
actor-carpad =
   .name = Tech Reinforcement Pad
   .description = Periodically provides an MBT.
actor-capsyb =
   .name = Tech Psychic Beacon
   .description-1 = Provides Psychic Control support power.
   .description-2 = Requires Power to operate.
actor-capsyb-koth =
   .name = Psychic Beacon
   .description = Capture and hold for 6 minutes to win.
actor-camisl =
   .name = Tech Missile Silo
   .description-1 = Provides Cluster Missile support power.
   .description-2 = Requires Power to operate.

## trees.yaml
actor-tibtre04 =
   .name = Gem Drill
actor-tibtre05 =
   .name = Fast Ore Drill

## upgrades.yaml
actor-upgrade-gi-fortifications =
   .name = Fiber-Reinforced Fortifications
   .description = Increases movement speed of G.I.s and Guardian G.I.s by 25%.
    Deployed G.I.s become uncrushable and gain 33% more durability.

actor-upgrade-a2a-missiles =
   .name = Air-to-Air Missile Systems
   .description = Enables Harriers to target enemy aircraft.

actor-upgrade-advanced-coolants =
   .name = Advanced Coolants
   .description = Doubles the power output of Power Plants.

actor-upgrade-advanced-training =
   .name = Advanced Training
   .description = The following units gain experience 50% faster:
     - G.I.
     - Guardian G.I.
     - Grenadier
     - Rocketeer
     - Navy SEAL
     - Tanya Adams
     - Chrono Commando

actor-upgrade-predator-missiles =
   .name = Predator Missiles
   .description = Increases the damage of Harriers by 25%.

actor-upgrade-composite-armor =
   .name = Composite Armor
   .description = Increases the durability of the following units by 33%:
     - Grizzly Tank
     - Ambulance
     - Tank Destroyer
     - Mirage Tank
     - Battle Fortress

actor-upgrade-boost-gliding-systems =
   .name = Boost-Gliding Systems
   .description = America's finest technology that increases the projectile speed and damage of the following units by 50%:
     - Patriot Missile System
     - Guardian G.I.
     - Infantry Fighting Vehicle
     - Aegis Cruiser
     - Chrono Dragon

actor-upgrade-portable-weaponry =
   .name = Portable Weaponry
   .description = Through tough British drilling G.I.s and Guardian G.I.s are now capable of
    using their deployed weapon while mobile.
    While deployed they fire 25% faster.

actor-upgrade-grand-cannon-protocols =
   .name = Grand Cannon Protocols
   .description = French defense protocol that allows the construction of the Grand Cannon.
    Increases durability of Walls, Gates and Defences by 15%.

actor-upgrade-advanced-gun-systems =
   .name = Advanced Gun Systems
   .description = Superior German weapon design increases the attack range of Grizzly Tanks and Tank Destroyers by 25%.

actor-upgrade-emp-munitions =
   .name = EMP Munitions
   .description = Korean invention that arms Grenadiers and Howitzers with EMP munitions which disable vehicles and buildings.

actor-upgrade-increased-payload =
   .name = Increased Payload
   .description = Doubles the payload of B2 Spirits from 5 to 10.

actor-upgrade-ap-bullets =
   .name = Armor-Piercing Bullets
   .description = Improves the damage of the following units by 25%:
     - Sentry Gun
     - Conscript
     - Boris Bukov
     - War Miner
     - Siege Chopper
   .description-baku = Improves the damage of the following units by 25%:
     - Sentry Gun
     - Conscript
     - War Miner
     - Sentry Drone

actor-upgrade-bullet-proof-coats =
   .name = Bullet-Proof Coats
   .description = Increases durability of Conscripts and Flak Troopers by 33%.
   .description-baku = Increases durability of Conscripts and Rocket Soldiers by 33%.

actor-upgrade-molotov-cocktails =
   .name = Molotov Cocktails
   .description = Provides Conscripts with Molotov Coctails which increase their damage against structures.

actor-upgrade-smoke-grenades =
   .name = Smoke Grenades
   .description = Provides Conscripts with Smoke Grenades which half the vision of enemy units and reduces their damage by 20%.

actor-upgrade-nuclear-engines =
   .name = Nuclear Engines
   .description = Increases the movement speed of the following units:
     - Rhino Tanks by 17%
     - Apocalypse Tanks by 25%
    Both units will explode violently and leave radiation when destroyed.
   .description-baku = Increases the movement speed of the following units:
     - Hydra Tanks by 17%
     - Apocalypse Tanks by 25%
    Both units will explode violently and leave radiation when destroyed.

actor-upgrade-uranium-shells =
   .name = Depleted Uranium Shells
   .description = Increases the damage of Rhino and Apocalypse Tanks by 25%.

actor-upgrade-mounted-mg =
   .name = Mounted Machine Guns
   .description = Arms Hydra Tanks with machine guns.

actor-upgrade-black-napalm =
   .name = Black Napalm
   .description = Doubles the damage of the following units:
     - Flame Tank
     - Black Kite

actor-upgrade-propaganda-effort =
   .name = Propaganda Effort
   .description = Increases effectiveness of Propaganda Towers and Propaganda Commander's Power by 25%.

actor-upgrade-overcharge =
   .name = Overcharge
   .description = Formidable Russian technology that enables Tesla weapons to disable enemy vehicles for a short time.

actor-upgrade-advanced-irradiators =
   .name = Advanced Irradiators
   .description = Devastating Iraqi fallout program that increases the damage of radiation by 50%.

actor-upgrade-aerial-propaganda =
   .name = Aerial Propaganda
   .description = Vietnamese propaganda must be spread world wide.
    This upgrade equips Black Kites and Kirov Airships with speakers to buff nearby friendly units.

actor-upgrade-targeted-explosives =
   .name = Targeted Explosives
   .description = Cuban safety protocols minimize any damage caused by explosions to your own army.
    Explosions caused by Terrorists, Death Bombs and Crazy Ivans no longer damage friendly units.
    Increases Terrorist and Death Bombs area of effect by 50%.

actor-upgrade-high-explosive-bombs =
   .name = High Explosive Bombs
   .description = Libyan chemical mixture that increases the damage of the following units by 25%:
     - Crazy Ivan
     - Chrono Ivan
     - Mortar Cycle
     - Demolition Truck
     - V3 Launcher
     - Dreadnought

actor-upgrade-radioactive-bombs =
   .name = Fallout Warheads
   .description = Makes Kirov, V3 Launcher and Dreadnought projectiles leave radiation.

actor-upgrade-cryo-warheads =
   .name = Cryo Missiles
   .description = Grad MLRS, Hydrofoil and MiG rockets gain cryo warheads
    which slow down enemy units and cause them to take more damage.

actor-upgrade-camouflage =
   .name = Camouflage
   .description = Provides Initiates and Viruses with camouflage.

actor-upgrade-grinder-treads =
   .name = Grinder Treads
   .description = Enables vehicles to heal themselves by crushing enemy units.

actor-upgrade-toxin-sprayer =
   .name = Toxin Sprayer
   .description = Provides Armored Trucks with a toxin sprayer cannon.

actor-upgrade-lazarus-prime =
   .name = Lazarus Prime
   .description = Enables Stealth Generators to cloak themselves.

actor-upgrade-autoloaders =
   .name = Autoloaders
   .description = Lasher Tanks will fire 2 shells at once.

actor-upgrade-rocket-barrage =
   .name = Rocket Barrage
   .description = Equips Lethocerus Platforms with a rocket pod that quickly fires 6 missiles at once.

actor-upgrade-chaos-tank-compensators =
   .name = Chaos Tank Compensators
   .description = Yurigrad developed gas pressure tubes causing vehicles to emit chaos gas as soon as they are destroyed.
    Increases area of effect of Chaos Drones.

actor-upgrade-laser-capacitors =
   .name = Laser Capacitors
   .description = Lazarus Corps' advanced laser technology increases the damage output of Lazarus Tanks and Leech Discs by 25%.
    This upgrade also turns the regular Lasher Tank weapon into a laser cannon.

actor-upgrade-mastery-of-mind =
   .name = Mastery of Mind
   .description = Antarctica's superior minds improved Yuri Clones and Masterminds to use Psi-Waves.
    Psi-Wave weapons no longer effect friendly units. They also now damage enemy vehicles.

actor-upgrade-dna-boosters =
   .name = DNA Boosters
   .description = Transylvanian technology that allows you to use the Genetic Mutator ability.
    This upgrade also increases the movement speed of the following units by 33%:
     - Brute
     - Mutant Tarantula
     - Mutant Crab

actor-upgrade-disc-armor =
   .name = Disc Armor
   .description = Lunar Eclipse Discs are impenetrable. This upgrade increases the durability of Discs by 33%.

actor-upgrade-chainguns =
   .name = Chainguns
   .description = Increases the damage of Gatling weapons by 25%.

actor-upgrade-endurance-training =
   .name = Endurance Training
   .description = Increases movement speed of G.I.s and Guardian G.I.s by 25%.

actor-upgrade-improved-flaks =
   .name = Improved Flaks
   .description = Anti-ground flak weapons become instant hit.

actor-upgrade-focused-mind =
   .name = Focused Mind
   .description = Increases attack speed of Initiates by 25%.

actor-upgrade-advanced-magnetics =
   .name = Advanced Magnetics
   .description = Magnetrons gain more attack range and deal 25% more damage. They also deal 50% of their damage to vehicles now.

actor-upgrade-cryo-shells =
   .name = Cryo Shells
   .description = Increases damage of Rhino and Apocalypse Tanks by 25%.
    Shells of both units freeze enemy units, slowing them down and making them take more damage.

actor-upgrade-high-caliber-rounds =
   .name = High Caliber Rounds
   .description = Enables Snipers to damage vehicles and kill gunners to disable them from firing for a while.

## yuri-infantry.yaml
actor-slav =
   .name = Slave
   .description = Gathers Ore and Gems.

actor-init =
   .name = Initiate
   .description = Basic Yuri Infantry.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Upgradeable with:
    - Camouflage

actor-brute =
   .name = Brute
   .description = Powerful soldiers.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Cannot be eaten by Attack Dogs
    
    Upgradeable with:
    - DNA Boosters (Transylvania)

actor-virus =
   .name = Virus
   .description = Sniper infantry armed with toxic bullets.
    Killed units leave toxin clouds.
    
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    
    Abilities:
    - Can kill garrisoned infantry
    
    Upgradeable with:
    - Camouflage

actor-yuripsi =
   .description = Psychic infantry. Can mind control enemy units.
    
      Strong vs Infantry, Vehicles
      Weak vs Terror Drones, Aircraft, Buildings
    
    Upgradeable with:
    - Mastery of Mind (Antarctica)

actor-yurix =
   .description = Psychic infantry. Can mind control enemy units and structures.
    Can be deployed to unleash a powerful psychic wave.
    
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Terror Drones, Aircraft
    
    Abilities:
    - Cannot be eaten by Attack Dogs
    - Can move over water
    
    Upgradeable with:
    - Mastery of Mind (Antarctica)
    
      Maximum 1 can be trained.

actor-lunr =
   .name = Cosmonaut
   .description = Airborne soldier.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles

actor-gtrp =
   .name = Gatling Trooper
   .description = Infantry armed with a heavy gatling cannon.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Upgradeable with:
    - Chainguns

actor-spct =
   .name = Lazarus Spectre
   .description = Invisible infantry armed with AP rifles with bullets capable of slowing enemy units.
    Can be deployed to make nearby units invisible but itself becomes visible.
    
      Strong vs Vehicles
      Weak vs Buildings, Aircraft

actor-ttrp =
   .name = Intoxicator
   .description = Infantry armed with a toxin sprayer and toxin bombs.
    Immune to toxic clouds.
    
      Strong vs Infantry, Buildings
      Weak vs Aircraft
    
    Abilities:
    - Can place Toxin Bombs on buildings

actor-crkt =
   .name = Chaos Trooper
   .description = Long range rocket infantry armed with chaos warheads.
    
      Strong vs Infantry, Vehicles, Aircraft
      Weak vs Buildings
    
    Abilities:
    - Immune to Chaos Gas

actor-visc-lrg =
   .name = Blood Vessel

## yuri-naval.yaml
actor-yhvr =
   .description = General-purpose naval transport.
    Can carry infantry and vehicles.
    
      Unarmed
    
    Upgradeable with:
    - Grinder Treads

actor-bsub =
   .name = Boomer Submarine
   .description = Submerged anti-ship and anti-structure armed with
    double torpedo and missile launchers.
    
      Strong vs Ships, Buildings
      Weak vs Ground units, Aircraft
    
    Abilities:
    - Can detect Stealth units

actor-cmisl =
   .name = Cruise Missile

actor-piranha =
   .name = Piranha Submarine
   .description = Submerged unit armed with fast firing torpedoes.
    
      Strong vs Ships
      Weak vs Ground units, Aircraft
    
    Abilities:
    - Can detect Stealth units

actor-gatsub =
   .name = Gatling Submarine
   .description = Submerged anti-air unit armed with gatling cannon.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles, Ships
    
    Abilities:
    - Can detect Stealth units
    
    Upgradeable with:
    - Chainguns

actor-floater =
   .name = Mutant Crab
   .generic-name = Crab
   .description = Large ocean creature capable of dealing heavy damage in close combat.
    Can move to land.
    
      Strong vs Ships
    
    Abilities:
    - Can remove Squids from ships by attacking them
    - Can detect Stealth units
    
    Upgradeable with:
    - DNA Boosters (Transylvania)

actor-strt =
   .name = Strider Tank
   .description = Light amphibious tank.
      Strong vs Vehicles, Ships
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Grinder Treads

actor-buoy =
   .name = Sensor Buoy

## yuri-structures.yaml
actor-yapowr =
   .name = Bio Reactor
   .description = Provides power for other structures.
    Can be occupied with up to 5 infantry for 50 more power each.

actor-yabrck =
   .description = Trains Psi-Corps infantry.
    Can heal nearby infantry.
    
      Cannot be placed on water.
      Can be rotated.

actor-yarefn =
   .name = Slave Miner
   .description = Gathers and processes ore.

actor-yaweap =
   .description = Produces Psi-Corps vehicles.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Cannot be placed on water.
      Can be rotated.

actor-yayard =
   .name = Submarine Pen
   .description = Produces Psi-Corps submarines, and other naval units.
    
    Abilities:
    - Comes with 3 repair drones.
    
      Can only be placed on water.

actor-yadept =
   .description = Repairs vehicles and removes Terror Drones for a price.
    
      Can be rotated.

actor-yagrnd =
   .name = Grinder
   .description = Converts Infantry and Vehicles back to credits.

actor-yadome =
   .name = Radar Dome
   .description = Provides radar.
    Researches basic upgrades.
    
    Provides a different support power depening on the subfaction:
    - Psi-Ops Drop (Yurigrad)
    - Hologram Army (Lazarus Corps)
    - Point Defense Drones (Antarctica)
    - Lethocerus Swarm (Transylvania)
    - Ion Cannon (Lunar Eclipse)

actor-yadisk =
   .name = Disc Pad
   .description = Produces Psi-Corps aircraft.
    
    Abilities:
    - Comes with 1 repair drone.

actor-yapsis =
   .name = Psychic Sensor
   .description = Detects enemy units and structures.
    
      Requires power to operate.

actor-yaclon =
   .name = Cloning Vats
   .description = Clones most trained infantry.
    
      Cannot be placed on water.
      Maximum 1 can be built.

actor-yawall =
   .name = Citadel Wall
   .description = Heavy wall capable of blocking units and projectiles.
    
      Cannot be placed on water.

actor-yaggun =
   .name = Gatling Cannon
   .description = Automated anti-infantry and anti-air defense.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Abilities:
    - Can detect Steath units
    
    Upgradeable with:
    - Chainguns
    
      Requires power to operate.

actor-natbnk =
   .name = Tank Bunker
   .description = Static defense with fireports for a vehicle to garrison.
    Provides increased firepower, fire speed and range to the vehicle.
    
      Cannot be placed on water.

actor-yapsyt =
   .name = Psychic Tower
   .description = Tower capable of mind controlling up to 3 enemy units.
    
      Strong vs Infantry, Vehicles
      Weak vs Buildings, Aircraft
    
    Abilities:
    - Can detect Steath units
    
      Requires power to operate.

actor-yasgen =
   .name = Stealth Generator
   .description = Makes nearby units and structures invisibile while not firing.
    
    Upgradeable with:
    - Lazarus Prime
    
      Requires power to operate.

actor-yagntc =
   .name = Lazarus Shield Generator
   .description = Makes vehicles invisible.
    Kills infantry.
    
      Requires power to operate.

actor-yappet =
   .name = Psychic Dominator
   .description = Release powerful energy that damages structures and mind controls units.
    
      Requires power to operate.

actor-yacomd =
   .name = Yuri's Command Center
actor-yapppt =
   .name = Psychic Dominator
actor-yarock =
   .name = Rocket Launch Pad
actor-yapsyb =
   .name = Psychic Beacon

actor-yaeast02 =
   .name = Yuri Statue
   .description = Big defense structure armed with lasers.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can detect Steath units

actor-yagate =
   .name = Psi-Corps Gate
actor-psirefn =
   .name = Ore Refinery

## yuri-vehicles.yaml
actor-pcv =
   .description = Deploys into a Construction Yard.
    
      Unarmed
    
    Abilities:
    - Can move over water
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)

actor-smin =
   .name = Slave Miner
   .description = Gathers and processes ore.
    
    Abilities:
    - Cannot be mind controlled
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)

actor-gmin =
   .name = Lazarus Miner
   .description = Gathers Ore and Gems.
    
      Unarmed
    
    Abilities:
    - Can move over water
    - Cannot be mind controlled
    - Stealth while not docked
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)

actor-ltnk =
   .name = Lasher Light Tank
   .description = Psi-Corps Main Battle Tank.
    
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Grinder Treads
    - Autoloaders
    - Chaos Tank Compensators (Yurigrad)
    - Laser Capacitors (Lazarus Corps)

actor-ytnk =
   .name = Gatling Tank
   .description = Anti-infantry and anti-air vehicle.
    Fires faster as it continues to fire.
    
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    
    Abilities:
    - Can detect Stealth units
    
    Upgradeable with:
    - Chaos Tank Compensators (Yurigrad)
    - Chainguns

actor-caos =
   .name = Chaos Drone
   .description = Drone capable of releasing gas that causes enemy units to fire random units.
    Units affected by the gas deal more damage.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities
    - Can be deployed to release gas continuously
    - Cannot be mind controlled
    
    Upgradeable with:
    - Chaos Tank Compensators (Yurigrad)

actor-tele =
   .name = Magnetron
   .description = Long range magnetic field generator.
    Freezes vehicles and damages structures.
    
      Strong vs Buildings, Vehicles
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Chaos Tank Compensators (Yurigrad)

actor-mind =
   .name = Master Mind
   .description = Heavy vehicle capable of mind controlling multipile enemy units.
    Starts taking damage if controlling more than 3.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Can crush enemy vehicles
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)
    - Mastery of Mind (Antarctica)

actor-mlyr =
   .name = Minelayer
   .description = Lays mines to destroy unwary enemy units.
      Unarmed
    
    Abilities:
    - Can detect Stealth units
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)

actor-kamaz =
   .name = Armored Truck
   .description = Armored infantry transport vehicle.
    
      Unarmed
    
    Upgradeable with:
    - Grinder Treads
    - Toxin Sprayer
    - Chaos Tank Compensators (Yurigrad)

actor-lart =
   .name = Annihilator Artillery
   .description = Long range laser artillery.
    
      Strong vs Infantry, Buildings
      Weak vs Aircraft
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)
    - Laser Capacitors (Lazarus Corps)

actor-yhtnk =
   .name = Lazarus Tank
   .description = Fast invisible tank designed for ambushes.
    
      Strong vs Buildings, Vehicles
      Weak vs Infantry, Aircraft
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)
    - Laser Capacitors (Lazarus Corps)

actor-grtk =
   .name = Grinder Tank
   .description = Melee vehicle with high damage against all ground units.
    
      Strong vs Buildings, Infantry, Vehicles
      Weak vs Aircraft
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)

actor-spider =
   .name = Mutant Tarantula
   .description = Mutated huge spider armed with poison spines and web launchers.
    
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    
    Abilities:
    - Immune to Radiation
    
    Upgradeable with:
    - DNA Boosters (Transylvania)

actor-expy =
   .description = Deploys into an Expansion Node.
    Cannot be undeployed back.
    
      Unarmed
    
    Abilities:
    - Can move over water
    
    Upgradeable with:
    - Grinder Treads
    - Chaos Tank Compensators (Yurigrad)
