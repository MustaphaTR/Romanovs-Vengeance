#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Effects
{
	class GpsDotEffectAS : IEffect, IEffectAboveShroud
	{
		readonly Actor actor;
		readonly GpsDotAS trait;
		readonly Animation anim;

		readonly PlayerDictionary<DotState> dotStates;
		readonly IDefaultVisibility visibility;
		readonly IVisibilityModifier[] visibilityModifiers;

		class DotState
		{
			public readonly GpsASWatcher Watcher;
			public readonly FrozenActor FrozenActor;
			public bool Visible;
			public DotState(Actor a, GpsASWatcher watcher, FrozenActorLayer frozenLayer)
			{
				Watcher = watcher;
				if (frozenLayer != null)
					FrozenActor = frozenLayer.FromID(a.ActorID);
			}
		}

		public GpsDotEffectAS(Actor actor, GpsDotAS trait)
		{
			this.actor = actor;
			this.trait = trait;
			anim = new Animation(actor.World, trait.Info.Image);
			anim.PlayRepeating(trait.Info.Sequence);

			visibility = actor.Trait<IDefaultVisibility>();
			visibilityModifiers = actor.TraitsImplementing<IVisibilityModifier>().ToArray();

			dotStates = new PlayerDictionary<DotState>(actor.World,
				p => new DotState(actor, p.PlayerActor.Trait<GpsASWatcher>(), p.FrozenActorLayer));
		}

		bool ShouldRender(DotState state, Player toPlayer)
		{
			// Hide the indicator if the owner trait is disabled
			if (trait.IsTraitDisabled)
				return false;

			// Hide the indicator if no watchers are available
			if (!state.Watcher.Granted && !state.Watcher.GrantedAllies)
				return false;

			// Hide the indicator if a frozen actor portrait is visible
			if (state.FrozenActor != null && state.FrozenActor.HasRenderables)
				return false;

			// Hide the indicator if the unit appears to be owned by an allied player
			if (actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null &&
					toPlayer.IsAlliedWith(actor.EffectiveOwner.Owner))
				return false;

			// Hide indicator if the actor wouldn't otherwise be visible if there wasn't fog
			foreach (var visibilityModifier in visibilityModifiers)
				if (!visibilityModifier.IsVisible(actor, toPlayer))
					return false;

			// Hide the indicator behind shroud
			if (!trait.Info.VisibleInShroud && !toPlayer.Shroud.IsExplored(actor.CenterPosition))
				return false;

			// Hide the indicator if it is not in range of a provider
			if (trait.Info.Range > WDist.Zero && !actor.World.FindActorsInCircle(actor.CenterPosition, trait.Info.Range)
				.Any(a => a.Info.HasTraitInfo<GpsASProviderInfo>() && state.Watcher.Providers.Contains(a.Trait<GpsASProvider>())))
				return false;

			return !visibility.IsVisible(actor, toPlayer);
		}

		void IEffect.Tick(World world)
		{
			for (var playerIndex = 0; playerIndex < dotStates.Count; playerIndex++)
			{
				var state = dotStates[playerIndex];
				state.Visible = ShouldRender(state, world.Players[playerIndex]);
			}
		}

		IEnumerable<IRenderable> IEffect.Render(WorldRenderer wr)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IEffectAboveShroud.RenderAboveShroud(WorldRenderer wr)
		{
			if (actor.World.RenderPlayer == null || !dotStates[actor.World.RenderPlayer].Visible)
				return SpriteRenderable.None;

			var effectiveOwner = actor.EffectiveOwner != null && actor.EffectiveOwner.Owner != null ?
				actor.EffectiveOwner.Owner : actor.Owner;

			var palette = wr.Palette(trait.Info.IndicatorPalettePrefix + effectiveOwner.InternalName);
			return anim.Render(actor.CenterPosition, palette);
		}
	}
}
