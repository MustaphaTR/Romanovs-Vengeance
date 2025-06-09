#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public enum ActorStatContent
	{
		None,
		Armor,
		Sight,
		Speed,
		Power,
		Damage,
		MindControl,
		Spread,
		ReloadDelay,
		MinRange,
		MaxRange,
		Harvester,
		Collector,
		CashTrickler,
		PeriodicProducer,
		Cargo,
		Carrier,
		Mob,
		Drones
	}

	public class ActorStatValuesInfo : TraitInfo
	{
		[Desc("Overrides the icon for the unit for the stats.")]
		public readonly string Icon;

		// Doesn't work properly with `bool?`.
		// [PaletteReference(nameof(IconPaletteIsPlayerPalette))]
		[Desc("Overrides the icon palette for the unit for the stats.")]
		public readonly string IconPalette;

		[Desc("Overrides if icon palette for the unit for the stats is a player palette.")]
		public readonly bool? IconPaletteIsPlayerPalette;

		[Desc("Types of stats to show.")]
		public readonly ActorStatContent[] Stats = { ActorStatContent.Armor, ActorStatContent.Sight };

		[Desc("Armament names to use for weapon stats.")]
		public readonly string[] Armaments;

		[Desc("Use this value for base damage of the unit for the stats.")]
		public readonly int? Damage;

		[Desc("Use this value for weapon spread of the unit for the stats.")]
		public readonly WDist? Spread;

		[Desc("Overrides the reload delay value from the weapons for the stats.")]
		public readonly int? ReloadDelay;

		[Desc("Overrides the sight value from RevealsShroud trait for the stats.")]
		public readonly WDist? Sight;

		[Desc("Overrides the range value from the weapons for the stats, enter 2 values for short and long range.")]
		public readonly WDist[] Range = Array.Empty<WDist>();

		[Desc("Overrides the minimum range value from the weapons for the stats.")]
		public readonly WDist? MinimumRange;

		[Desc("Overrides the movement speed value from Mobile or Aircraft traits for the stats.")]
		public readonly int? Speed;

		[Desc("Don't show these armor classes for the Armor stat.")]
		public readonly string[] ArmorsToIgnore = Array.Empty<string>();

		[Desc("Show shield level in place of Armor when actor has active Shielded trait.")]
		public readonly bool ShowShield = true;

		[ActorReference]
		[Desc("Actor to use for Tooltip when hovering of the icon.")]
		public readonly string TooltipActor;

		[Desc("Prerequisites to enable upgrades, without them upgrades won't be shown.",
			"Only checked at the actor creation.")]
		public readonly string[] UpgradePrerequisites = Array.Empty<string>();

		[ActorReference]
		[Desc("Upgrades this actor is affected by",
			"Upgrade actor must have prerequisite of actor's name !!")]
		public readonly string[] Upgrades = Array.Empty<string>();

		[ActorReference]
		[Desc("Which of the actors defined under Upgrades are produced by the actor itself, and only effects it. ",
			"Upgrade is the name of produced actor")]
		public readonly string[] LocalUpgrades = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new ActorStatValues(init, this); }
	}

	public class ActorStatValues : INotifyCreated, INotifyDisguised, INotifyOwnerChanged, INotifyProduction
	{
		[FluentReference]
		const string DefaultArmorName = "label-armor-class.no-armor";

		readonly Actor self;
		public ActorStatValuesInfo Info;

		public BuildableInfo BuildableInfo;
		public string Icon;
		public string IconPalette;
		public bool IconPaletteIsPlayerPalette;
		public WithStatIconOverlay[] IconOverlays;

		public ActorStatContent[] CurrentStats;
		public ActorStatOverride[] TooltipActorOverrides;
		public ActorStatOverride[] IconOverrides;
		public ActorStatOverride[] StatClassOverrides;
		public ActorStatOverride[] HealthStatOverrides;
		public ActorStatOverride[] DamageStatOverrides;
		public ActorStatOverride[] UpgradeOverrides;

		public int CurrentMaxHealth;
		public int? CurrentDamage;
		public int Speed;

		public ITooltip[] Tooltips;
		public Armor[] Armors;
		public RevealsShroud[] RevealsShrouds;
		public AttackBase[] AttackBases;
		public Armament[] Armaments;
		public Power[] Powers;

		public IHealth Health;
		public Shielded Shielded;

		public Mobile Mobile;
		public Aircraft Aircraft;

		public MindController MindController;

		public IStoresResources ResourceHold;
		public ISupplyCollector Collector;
		public CashTrickler[] CashTricklers = Array.Empty<CashTrickler>();
		public PeriodicProducer[] PeriodicProducers = Array.Empty<PeriodicProducer>();
		public Cargo Cargo;
		public SharedCargo SharedCargo;
		public Garrisonable Garrisonable;
		public CarrierMaster CarrierMaster;
		public MobSpawnerMaster[] MobSpawnerMasters;
		public DroneSpawnerMaster[] DroneSpawnerMasters;

		public IRevealsShroudModifier[] SightModifiers;
		public IFirepowerModifier[] FirepowerModifiers;
		public IReloadModifier[] ReloadModifiers;
		public IRangeModifier[] RangeModifiers;
		public ISpeedModifier[] SpeedModifiers;
		public IPowerModifier[] PowerModifiers;
		public IResourceValueModifier[] ResourceValueModifiers;

		public ActorInfo TooltipActor;

		PlayerResources playerResources;
		TechTree techTree;

		public bool UpgradesEnabled;
		public string[] CurrentUpgrades = Array.Empty<string>();
		public Dictionary<string, bool> Upgrades = new();

		public bool Disguised;
		public Player DisguisePlayer;
		public string DisguiseImage;
		public int DisguiseMaxHealth = 0;
		public string[] DisguiseStatIcons = new string[9];
		public string[] DisguiseStats = new string[9];
		public Dictionary<string, bool> DisguiseUpgrades = new();
		public string[] DisguiseCurrentUpgrades = Array.Empty<string>();

		public ActorStatValues(ActorInitializer init, ActorStatValuesInfo info)
		{
			Info = info;
			self = init.Self;

			self.World.ActorAdded += UpdateExternalUpgradesState;
			self.World.ActorRemoved += UpdateExternalUpgradesState;
		}

		void INotifyCreated.Created(Actor self)
		{
			IconOverrides = self.TraitsImplementing<ActorStatOverride>().Where(aso => aso.Info.Icon != null).ToArray();
			TooltipActorOverrides = self.TraitsImplementing<ActorStatOverride>().Where(aso => aso.Info.TooltipActor != null).ToArray();
			StatClassOverrides = self.TraitsImplementing<ActorStatOverride>().Where(aso => aso.Info.Stats != null).ToArray();
			HealthStatOverrides = self.TraitsImplementing<ActorStatOverride>().Where(aso => aso.Info.Health != null).ToArray();
			DamageStatOverrides = self.TraitsImplementing<ActorStatOverride>().Where(aso => aso.Info.Damage != null).ToArray();
			UpgradeOverrides = self.TraitsImplementing<ActorStatOverride>().Where(aso => aso.Info.Upgrades != null).ToArray();
			CalculateStats();

			BuildableInfo = self.Info.TraitInfos<BuildableInfo>().FirstOrDefault();
			SetupCameos();
			IconOverlays = self.TraitsImplementing<WithStatIconOverlay>().ToArray();

			Tooltips = self.TraitsImplementing<ITooltip>().ToArray();
			Armors = self.TraitsImplementing<Armor>().Where(a => !Info.ArmorsToIgnore.Contains(a.Info.Type)).ToArray();
			RevealsShrouds = self.TraitsImplementing<RevealsShroud>().ToArray();
			Powers = self.TraitsImplementing<Power>().ToArray();

			AttackBases = self.TraitsImplementing<AttackBase>().ToArray();
			Armaments = self.TraitsImplementing<Armament>().Where(a => IsValidArmament(a.Info.Name)).ToArray();

			Health = self.TraitOrDefault<IHealth>();
			Shielded = self.TraitOrDefault<Shielded>();

			Mobile = self.TraitOrDefault<Mobile>();
			Aircraft = self.TraitOrDefault<Aircraft>();

			MindController = self.TraitOrDefault<MindController>();

			ResourceHold = self.TraitOrDefault<IStoresResources>();
			Collector = self.TraitOrDefault<ISupplyCollector>();
			CashTricklers = self.TraitsImplementing<CashTrickler>().ToArray();
			PeriodicProducers = self.TraitsImplementing<PeriodicProducer>().ToArray();
			Cargo = self.TraitOrDefault<Cargo>();
			SharedCargo = self.TraitOrDefault<SharedCargo>();
			Garrisonable = self.TraitOrDefault<Garrisonable>();
			CarrierMaster = self.TraitOrDefault<CarrierMaster>();
			MobSpawnerMasters = self.TraitsImplementing<MobSpawnerMaster>().ToArray();
			DroneSpawnerMasters = self.TraitsImplementing<DroneSpawnerMaster>().ToArray();

			CalculateHealthStat();
			CalculateDamageStat();
			if (Info.Speed != null)
				Speed = Info.Speed.Value;
			else if (Aircraft != null)
				Speed = Aircraft.Info.Speed;
			else if (Mobile != null)
				Speed = Mobile.Info.Speed;

			SightModifiers = self.TraitsImplementing<IRevealsShroudModifier>().ToArray();
			FirepowerModifiers = self.TraitsImplementing<IFirepowerModifier>().ToArray();
			ReloadModifiers = self.TraitsImplementing<IReloadModifier>().ToArray();
			RangeModifiers = self.TraitsImplementing<IRangeModifier>().ToArray();
			SpeedModifiers = self.TraitsImplementing<ISpeedModifier>().ToArray();
			PowerModifiers = self.TraitsImplementing<IPowerModifier>().ToArray();
			ResourceValueModifiers = self.TraitsImplementing<IResourceValueModifier>().ToArray();

			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			techTree = self.Owner.PlayerActor.Trait<TechTree>();

			UpgradesEnabled = techTree.HasPrerequisites(Info.UpgradePrerequisites);
			CalculateUpgrades();
		}

		void SetupCameos()
		{
			CalculateIcon();

			if (Info.IconPalette != null)
				IconPalette = Info.IconPalette;
			else if (BuildableInfo != null)
				IconPalette = BuildableInfo.IconPalette;

			if (Info.IconPaletteIsPlayerPalette != null)
				IconPaletteIsPlayerPalette = Info.IconPaletteIsPlayerPalette.Value;
			else if (BuildableInfo != null)
				IconPaletteIsPlayerPalette = BuildableInfo.IconPaletteIsPlayerPalette;

			CalculateTooltipActor();
		}

		public void CalculateIcon()
		{
			if (Info.Icon != null)
				Icon = Info.Icon;
			else if (BuildableInfo != null)
				Icon = BuildableInfo.Icon;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var iconOverride = Array.Find(
				IconOverrides, aso => !aso.IsTraitDisabled && (viewer == null || aso.Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))));
			if (iconOverride != null)
				Icon = iconOverride.Info.Icon;
		}

		public void CalculateTooltipActor()
		{
			if (Info.TooltipActor != null)
				TooltipActor = self.World.Map.Rules.Actors[Info.TooltipActor];
			else
				TooltipActor = self.Info;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var tooltipActorOverride = Array.Find(
				TooltipActorOverrides, aso => !aso.IsTraitDisabled && (viewer == null || aso.Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))));
			if (tooltipActorOverride != null)
				TooltipActor = self.World.Map.Rules.Actors[tooltipActorOverride.Info.TooltipActor];
		}

		public void CalculateStats()
		{
			CurrentStats = Info.Stats;
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var statOverride = Array.Find(
				StatClassOverrides, aso => !aso.IsTraitDisabled && (viewer == null || aso.Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))));
			if (statOverride != null)
				CurrentStats = statOverride.Info.Stats;
		}

		public void CalculateHealthStat()
		{
			if (Health != null)
				CurrentMaxHealth = Health.MaxHP;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var healthOverride = Array.Find(
				HealthStatOverrides, aso => !aso.IsTraitDisabled && (viewer == null || aso.Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))));
			if (healthOverride != null)
				CurrentMaxHealth = healthOverride.Info.Health.Value;
		}

		public void CalculateDamageStat()
		{
			CurrentDamage = Info.Damage;
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var damageOverride = Array.Find(
				DamageStatOverrides, aso => !aso.IsTraitDisabled && (viewer == null || aso.Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))));
			if (damageOverride != null)
				CurrentDamage = damageOverride.Info.Damage.Value;
		}

		public void CalculateUpgrades()
		{
			if (!UpgradesEnabled)
				return;

			CurrentUpgrades = Info.Upgrades;
			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			var upgradeOverride = Array.Find(
				UpgradeOverrides, aso => !aso.IsTraitDisabled && (viewer == null || aso.Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(viewer))));
			if (upgradeOverride != null)
				CurrentUpgrades = upgradeOverride.Info.Upgrades;

			Upgrades.Clear();
			foreach (var upgrade in CurrentUpgrades)
				Upgrades.Add(upgrade, techTree.HasPrerequisites(new string[] { upgrade }));
		}

		// Handle the upgrade that provide effect by using prerequisite
		void UpdateExternalUpgradesState(Actor a)
		{
			var upgrade = a.Info.Name;
			if (!UpgradesEnabled || Info.LocalUpgrades.Contains(upgrade))
				return;

			if (a.Owner == self.Owner && Upgrades.ContainsKey(upgrade))
				Upgrades[upgrade] = techTree.HasPrerequisites(new string[] { upgrade });

			if (a.Owner == DisguisePlayer && DisguiseUpgrades.ContainsKey(a.Info.Name))
				DisguiseUpgrades[a.Info.Name] = DisguisePlayer.PlayerActor.Trait<TechTree>().HasPrerequisites(new string[] { upgrade });
		}

		// Handle the upgrade on this actor produces upgrade for it own.
		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (Info.LocalUpgrades.Length == 0)
				return;

			if (Info.LocalUpgrades.Contains(other.Info.Name) && Upgrades.ContainsKey(other.Info.Name))
				Upgrades[other.Info.Name] = true;
		}

		public bool IsValidArmament(string armament)
		{
			if (Info.Armaments != null)
				return Info.Armaments.Contains(armament);
			else
				return Array.Exists(AttackBases, ab => ab.Info.Armaments.Contains(armament));
		}

		public string CalculateArmor()
		{
			if (Info.ShowShield && Shielded != null && !Shielded.IsTraitDisabled && Shielded.Strength > 0)
				return (Shielded.Strength / 100).ToString(NumberFormatInfo.CurrentInfo) + " / " + (Shielded.Info.MaxStrength / 100).ToString(NumberFormatInfo.CurrentInfo);

			var activeArmor = Array.Find(Armors, a => !a.IsTraitDisabled);
			if (activeArmor == null)
				return FluentProvider.GetMessage(DefaultArmorName);

			return FluentProvider.GetMessage("label-armor-class." + activeArmor?.Info.Type.Replace('.', '-'));
		}

		public string CalculateSight()
		{
			var revealsShroudValue = WDist.Zero;
			if (Info.Sight != null)
				revealsShroudValue = Info.Sight.Value;
			else
				foreach (var rs in RevealsShrouds)
					if (!rs.IsTraitDisabled)
						revealsShroudValue = revealsShroudValue > rs.Info.Range ? revealsShroudValue : rs.Info.Range;

			foreach (var rsm in SightModifiers.Select(rsm => rsm.GetRevealsShroudModifier()))
				revealsShroudValue = revealsShroudValue * rsm / 100;

			return Math.Round((float)revealsShroudValue.Length / 1024, 2).ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateSpeed()
		{
			if (Mobile == null && Aircraft == null)
				return "0";

			var speedValue = Speed;
			foreach (var sm in SpeedModifiers.Select(sm => sm.GetSpeedModifier()))
				speedValue = speedValue * sm / 100;

			return speedValue.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculatePower()
		{
			var powerValue = 0;
			foreach (var p in Powers)
				if (!p.IsTraitDisabled)
					powerValue += p.Info.Amount;

			foreach (var pm in PowerModifiers.Select(pm => pm.GetPowerModifier()))
				powerValue = powerValue * pm / 100;

			return powerValue.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateMindControl()
		{
			if (MindController == null)
				return "0 / 0";

			return MindController.Slaves.Count().ToString(NumberFormatInfo.CurrentInfo) + " / " + MindController.Info.Capacity.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateDamage()
		{
			var damageValue = 0;
			if (CurrentDamage != null)
				damageValue = CurrentDamage.Value;
			else
				foreach (var ar in Armaments)
					if (!ar.IsTraitDisabled)
						damageValue += ar.Info.Damage ?? 0;

			foreach (var dm in FirepowerModifiers.Select(fm => fm.GetFirepowerModifier(null)))
				damageValue = damageValue * dm / 100;

			return damageValue.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateSpread()
		{
			var spreadValue = WDist.Zero;
			if (Info.Spread != null)
				spreadValue = Info.Spread.Value;
			else
				foreach (var ar in Armaments)
					if (!ar.IsTraitDisabled)
					{
						var sv = ar.Info.Spread ?? WDist.Zero;
						spreadValue = spreadValue > sv ? spreadValue : sv;
					}

			return Math.Round((float)spreadValue.Length / 1024, 2).ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateRoF()
		{
			var rofValue = int.MaxValue;
			if (Info.ReloadDelay != null)
				rofValue = Info.ReloadDelay.Value;
			else
			{
				foreach (var ar in Armaments)
					if (!ar.IsTraitDisabled)
					{
						var rof = ar.Info.ReloadDelay ?? ar.Weapon.ReloadDelay;
						rofValue = rofValue < rof ? rofValue : rof;
					}
			}

			if (rofValue != int.MaxValue)
				foreach (var rm in ReloadModifiers.Select(sm => sm.GetReloadModifier(null)))
					rofValue = rofValue * rm / 100;
			else
				rofValue = 0;

			return rofValue.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateRange(int slot)
		{
			var shortRangeValue = WDist.MaxValue;
			var longRangeValue = WDist.Zero;

			if (Info.Range.Length >= 1)
			{
				shortRangeValue = Info.Range.Min();
				longRangeValue = Info.Range.Max();
			}
			else
			{
				foreach (var ar in Armaments)
					if (!ar.IsTraitDisabled)
					{
						var wr = ar.Info.Range ?? ar.Weapon.Range;
						longRangeValue = longRangeValue > wr ? longRangeValue : wr;
						shortRangeValue = shortRangeValue < wr ? shortRangeValue : wr;
					}
			}

			if (shortRangeValue == WDist.MaxValue)
				shortRangeValue = WDist.Zero;

			foreach (var rm in RangeModifiers.Select(rm => rm.GetRangeModifier()))
			{
				shortRangeValue = shortRangeValue * rm / 100;
				longRangeValue = longRangeValue * rm / 100;
			}

			var text = "";
			if (CurrentStats[slot - 1] == ActorStatContent.MaxRange)
				text += Math.Round((float)longRangeValue.Length / 1024, 2).ToString(NumberFormatInfo.CurrentInfo);
			else if (CurrentStats[slot - 1] == ActorStatContent.MinRange)
				text += Math.Round((float)shortRangeValue.Length / 1024, 2).ToString(NumberFormatInfo.CurrentInfo);

			var minimumRangeValue = WDist.MaxValue;
			if (Info.MinimumRange != null)
				minimumRangeValue = Info.MinimumRange.Value;
			else
			{
				foreach (var ar in Armaments)
					if (!ar.IsTraitDisabled)
					{
						var mr = ar.Info.MinimumRange ?? ar.Weapon.MinRange;
						minimumRangeValue = minimumRangeValue < mr ? minimumRangeValue : mr;
					}
			}

			if (minimumRangeValue.Length > 100 && minimumRangeValue != WDist.MaxValue)
				text = Math.Round((float)minimumRangeValue.Length / 1024, 2).ToString(NumberFormatInfo.CurrentInfo) + "-" + text;

			return text;
		}

		public string CalculateResourceHold()
		{
			if (ResourceHold == null)
				return "$0";

			var currentContents = ResourceHold.Contents.Values.Sum().ToString(NumberFormatInfo.CurrentInfo);
			var capacity = ResourceHold.Capacity.ToString(NumberFormatInfo.CurrentInfo);

			var value = 0;
			foreach (var content in ResourceHold.Contents)
				value += playerResources.Info.ResourceValues[content.Key] * content.Value;

			return currentContents + " / " + capacity + " ($" + value.ToString(NumberFormatInfo.CurrentInfo) + ")";
		}

		public string CalculateCollector()
		{
			if (Collector == null)
				return "$0";

			var value = Collector.Amount();
			foreach (var dm in ResourceValueModifiers.Select(rvm => rvm.GetResourceValueModifier()))
				value = value * dm / 100;

			return "$" + value.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateCashTrickler()
		{
			var minTicks = int.MaxValue;
			foreach (var ct in CashTricklers)
				if (!ct.IsTraitDisabled)
					minTicks = Math.Min(minTicks, ct.Ticks);

			return WidgetUtils.FormatTime(minTicks == int.MaxValue ? 0 : minTicks, self.World.Timestep);
		}

		public string CalculatePeriodicProducer()
		{
			var minTicks = int.MaxValue;
			foreach (var pp in PeriodicProducers)
				if (!pp.IsTraitDisabled)
					minTicks = Math.Min(minTicks, pp.Ticks);

			return WidgetUtils.FormatTime(minTicks == int.MaxValue ? 0 : minTicks, self.World.Timestep);
		}

		public string CalculateCargo()
		{
			if (Cargo != null)
				return Cargo.TotalWeight + " / " + Cargo.Info.MaxWeight;
			else if (SharedCargo != null)
				return SharedCargo.Manager.TotalWeight + " / " + SharedCargo.Manager.Info.MaxWeight;
			else if (Garrisonable != null)
				return Garrisonable.TotalWeight + " / " + Garrisonable.Info.MaxWeight;
			else
				return "0 / 0";
		}

		public string CalculateCarrier()
		{
			if (CarrierMaster == null)
				return "0 / 0 / 0";

			var stored = 0;
			var valid = 0;

			foreach (var s in CarrierMaster.SlaveEntries)
				if (s.IsValid)
				{
					valid++;
					if (!s.IsLaunched) stored++;
				}

			return stored.ToString(NumberFormatInfo.CurrentInfo) + " / "
				+ valid.ToString(NumberFormatInfo.CurrentInfo) + " / "
				+ CarrierMaster.Info.Actors.Length.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateMobSpawner()
		{
			var total = 0;
			var spawned = 0;
			foreach (var mobSpawnerMaster in MobSpawnerMasters)
			{
				if (!mobSpawnerMaster.IsTraitDisabled)
				{
					total += mobSpawnerMaster.Info.Actors.Length;
					spawned += mobSpawnerMaster.SlaveEntries.Count(s => s.IsValid);
				}
			}

			return spawned.ToString(NumberFormatInfo.CurrentInfo) + " / " + total.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string CalculateDroneSpawner()
		{
			var total = 0;
			var spawned = 0;
			foreach (var droneSpawnerMaster in DroneSpawnerMasters)
			{
				if (!droneSpawnerMaster.IsTraitDisabled)
				{
					total += droneSpawnerMaster.Info.Actors.Length;
					spawned += droneSpawnerMaster.SlaveEntries.Count(s => s.IsValid);
				}
			}

			return spawned.ToString(NumberFormatInfo.CurrentInfo) + " / " + total.ToString(NumberFormatInfo.CurrentInfo);
		}

		public string GetIconFor(int slot)
		{
			if (CurrentStats.Length < slot || CurrentStats[slot - 1] == ActorStatContent.None)
				return null;
			else if (CurrentStats[slot - 1] == ActorStatContent.Armor)
			{
				if (Info.ShowShield && Shielded != null && !Shielded.IsTraitDisabled && Shielded.Strength > 0)
					return "actor-stats-shield";

				return "actor-stats-armor";
			}
			else if (CurrentStats[slot - 1] == ActorStatContent.Sight)
				return "actor-stats-sight";
			else if (CurrentStats[slot - 1] == ActorStatContent.Speed)
				return "actor-stats-speed";
			else if (CurrentStats[slot - 1] == ActorStatContent.Power)
				return "actor-stats-power";
			else if (CurrentStats[slot - 1] == ActorStatContent.Damage)
				return "actor-stats-damage";
			else if (CurrentStats[slot - 1] == ActorStatContent.MindControl)
				return "actor-stats-mindcontrol";
			else if (CurrentStats[slot - 1] == ActorStatContent.ReloadDelay)
				return "actor-stats-rof";
			else if (CurrentStats[slot - 1] == ActorStatContent.Spread)
				return "actor-stats-spread";
			else if (CurrentStats[slot - 1] == ActorStatContent.MinRange)
				if (CurrentStats.Contains(ActorStatContent.MaxRange))
					return "actor-stats-shortrange";
				else
					return "actor-stats-range";
			else if (CurrentStats[slot - 1] == ActorStatContent.MaxRange)
				if (CurrentStats.Contains(ActorStatContent.MinRange))
					return "actor-stats-longrange";
				else
					return "actor-stats-range";
			else if (CurrentStats[slot - 1] == ActorStatContent.Harvester)
				return "actor-stats-resources";
			else if (CurrentStats[slot - 1] == ActorStatContent.Collector)
				return "actor-stats-resources";
			else if (CurrentStats[slot - 1] == ActorStatContent.CashTrickler)
				return "actor-stats-timer";
			else if (CurrentStats[slot - 1] == ActorStatContent.PeriodicProducer)
				return "actor-stats-timer";
			else if (CurrentStats[slot - 1] == ActorStatContent.Cargo)
				return "actor-stats-cargo";
			else if (CurrentStats[slot - 1] == ActorStatContent.Carrier)
				return "actor-stats-carrier";
			else if (CurrentStats[slot - 1] == ActorStatContent.Mob)
				return "actor-stats-mob";
			else if (CurrentStats[slot - 1] == ActorStatContent.Drones)
				return "actor-stats-drones";
			else
				return null;
		}

		public string GetValueFor(int slot)
		{
			if (CurrentStats.Length < slot || CurrentStats[slot - 1] == ActorStatContent.None)
				return null;
			else if (CurrentStats[slot - 1] == ActorStatContent.Armor)
				return CalculateArmor();
			else if (CurrentStats[slot - 1] == ActorStatContent.Sight)
				return CalculateSight();
			else if (CurrentStats[slot - 1] == ActorStatContent.Speed)
				return CalculateSpeed();
			else if (CurrentStats[slot - 1] == ActorStatContent.Power)
				return CalculatePower();
			else if (CurrentStats[slot - 1] == ActorStatContent.Damage)
				return CalculateDamage();
			else if (CurrentStats[slot - 1] == ActorStatContent.MindControl)
				return CalculateMindControl();
			else if (CurrentStats[slot - 1] == ActorStatContent.ReloadDelay)
				return CalculateRoF();
			else if (CurrentStats[slot - 1] == ActorStatContent.Spread)
				return CalculateSpread();
			else if (CurrentStats[slot - 1] == ActorStatContent.MinRange || CurrentStats[slot - 1] == ActorStatContent.MaxRange)
				return CalculateRange(slot);
			else if (CurrentStats[slot - 1] == ActorStatContent.Harvester)
				return CalculateResourceHold();
			else if (CurrentStats[slot - 1] == ActorStatContent.Collector)
				return CalculateCollector();
			else if (CurrentStats[slot - 1] == ActorStatContent.CashTrickler)
				return CalculateCashTrickler();
			else if (CurrentStats[slot - 1] == ActorStatContent.PeriodicProducer)
				return CalculatePeriodicProducer();
			else if (CurrentStats[slot - 1] == ActorStatContent.Cargo)
				return CalculateCargo();
			else if (CurrentStats[slot - 1] == ActorStatContent.Carrier)
				return CalculateCarrier();
			else if (CurrentStats[slot - 1] == ActorStatContent.Mob)
				return CalculateMobSpawner();
			else if (CurrentStats[slot - 1] == ActorStatContent.Drones)
				return CalculateDroneSpawner();

			return "";
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			techTree = newOwner.PlayerActor.Trait<TechTree>();
			foreach (var upgrade in CurrentUpgrades)
			{
				if (Info.LocalUpgrades.Contains(upgrade))
					continue;

				Upgrades[upgrade] = techTree.HasPrerequisites(new string[] { upgrade });
			}
		}

		void INotifyDisguised.DisguiseChanged(Actor self, Actor target)
		{
			Disguised = self != target;

			if (Disguised)
			{
				var targetASV = target.TraitOrDefault<ActorStatValues>();
				if (targetASV != null)
				{
					Icon = targetASV.Icon;
					IconPalette = targetASV.IconPalette;
					IconPaletteIsPlayerPalette = targetASV.IconPaletteIsPlayerPalette;
					TooltipActor = targetASV.TooltipActor;

					DisguisePlayer = target.Owner;
					DisguiseImage = target.TraitOrDefault<RenderSprites>()?.GetImage(target);
					DisguiseMaxHealth = targetASV.CurrentMaxHealth;

					for (var i = 1; i <= 8; i++)
					{
						DisguiseStatIcons[i] = targetASV.GetIconFor(i);
						DisguiseStats[i] = targetASV.GetValueFor(i);
					}

					DisguiseUpgrades = targetASV.Upgrades;
					DisguiseCurrentUpgrades = targetASV.CurrentUpgrades;
				}
				else
				{
					SetupCameos();
					DisguiseImage = null;
					DisguiseMaxHealth = 0;
					Disguised = false;
				}
			}
			else
			{
				SetupCameos();
				DisguiseImage = null;
				DisguiseMaxHealth = 0;
			}
		}
	}
}
