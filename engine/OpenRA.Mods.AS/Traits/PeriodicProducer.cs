#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Produces an actor without using the standard production queue.")]
	public class PeriodicProducerInfo : PausableConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] Actors = null;

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Duration between productions.")]
		public readonly int ChargeDuration = 1000;

		public readonly bool ResetTraitOnEnable = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color ChargeColor = Color.DarkOrange;

		[Desc("Defines to which players the bar is to be shown.")]
		public readonly PlayerRelationship SelectionBarDisplayRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new PeriodicProducer(init, this); }
	}

	public class PeriodicProducer : PausableConditionalTrait<PeriodicProducerInfo>, ISelectionBar, ITick, ISync
	{
		readonly PeriodicProducerInfo info;
		Production[] productionlines;
		readonly Actor self;

		[Sync]
		public int Ticks { get; private set; }

		public PeriodicProducer(ActorInitializer init, PeriodicProducerInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			Ticks = info.ChargeDuration;
		}

		protected override void Created(Actor self)
		{
			productionlines = self.TraitsImplementing<Production>().ToArray();
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused)
				return;

			if (!IsTraitDisabled && --Ticks < 0)
			{
				var sp = Array.Find(productionlines, p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Contains(info.Type));

				var activated = false;

				if (sp != null)
				{
					foreach (var name in info.Actors)
					{
						var inits = new TypeDictionary
						{
							new OwnerInit(self.Owner),
							new FactionInit(sp.Faction)
						};

						activated |= sp.Produce(self, self.World.Map.Rules.Actors[name.ToLowerInvariant()], info.Type, inits, 0);
					}
				}

				if (activated)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				else
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);

				Ticks = info.ChargeDuration;
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				Ticks = info.ChargeDuration;
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (viewer != null && !Info.SelectionBarDisplayRelationships.HasRelationship(self.Owner.RelationshipWith(viewer)))
				return 0f;

			return (float)(info.ChargeDuration - Ticks) / info.ChargeDuration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.ChargeColor;
		}

		bool ISelectionBar.DisplayWhenEmpty
		{
			get
			{
				var viewer = self.World.RenderPlayer ?? self.World.LocalPlayer;
				if (viewer != null && !Info.SelectionBarDisplayRelationships.HasRelationship(self.Owner.RelationshipWith(viewer)))
					return false;

				return info.ShowSelectionBar && !IsTraitDisabled;
			}
		}
	}
}
