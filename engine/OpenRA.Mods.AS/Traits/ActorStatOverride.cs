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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public class ActorStatOverrideInfo : ConditionalTraitInfo, Requires<ActorStatValuesInfo>
	{
		[Desc("Overrides the icon for the unit for the stats.")]
		public readonly string Icon;

		[ActorReference]
		[Desc("Actor to use for Tooltip when hovering of the icon.")]
		public readonly string TooltipActor;

		[Desc("Types of stats to show.")]
		public readonly ActorStatContent[] Stats;

		[Desc("Overrides the health value for the unit for the stats.")]
		public readonly int? Health;

		[Desc("Overrides the damage value for the unit for the stats.")]
		public readonly int? Damage;

		[ActorReference]
		[Desc("Upgrades this actor is affected by.")]
		public readonly string[] Upgrades;

		[Desc("Relationships that the override will apply for.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		public override object Create(ActorInitializer init) { return new ActorStatOverride(init, this); }
	}

	public class ActorStatOverride : ConditionalTrait<ActorStatOverrideInfo>, ITick
	{
		readonly ActorStatValues asv;
		Player cachedRenderPlayer;

		public ActorStatOverride(ActorInitializer init, ActorStatOverrideInfo info)
			: base(info)
		{
			asv = init.Self.Trait<ActorStatValues>();

			cachedRenderPlayer = init.World.RenderPlayer;
		}

		void UpdateData()
		{
			if (Info.Icon != null)
				asv.CalculateIcon();
			if (Info.TooltipActor != null)
				asv.CalculateTooltipActor();
			if (Info.Stats != null)
				asv.CalculateStats();
			if (Info.Health != null)
				asv.CalculateHealthStat();
			if (Info.Damage != null)
				asv.CalculateDamageStat();
			if (Info.Upgrades != null)
				asv.CalculateUpgrades();
		}

		void ITick.Tick(Actor self)
		{
			if (cachedRenderPlayer != self.World.RenderPlayer)
			{
				cachedRenderPlayer = self.World.RenderPlayer;
				UpdateData();
			}
		}

		protected override void TraitEnabled(Actor self) { UpdateData(); }

		protected override void TraitDisabled(Actor self) { UpdateData(); }
	}
}
