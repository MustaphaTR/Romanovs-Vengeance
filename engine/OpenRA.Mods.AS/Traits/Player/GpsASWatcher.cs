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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Required for AS GPS-related logic to function. Attach this to the player actor.")]
	class GpsASWatcherInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new GpsASWatcher(init.Self.Owner); }
	}

	interface IOnGpsASRefreshed { void OnGpsASRefresh(Actor self, Player player); }

	class GpsASWatcher : ISync, IPreventsShroudReset
	{
		[Sync]
		public bool GrantedAllies { get; private set; }

		[Sync]
		public bool Granted { get; private set; }

		readonly Player owner;

		public readonly List<GpsASProvider> Providers = new();
		readonly HashSet<TraitPair<IOnGpsASRefreshed>> notifyOnRefresh = new();

		public GpsASWatcher(Player owner)
		{
			this.owner = owner;
		}

		public void DeactivateGps(GpsASProvider trait, Player owner)
		{
			if (!Providers.Contains(trait))
				return;

			Providers.Remove(trait);
			RefreshGps(owner);
		}

		public void ActivateGps(GpsASProvider trait, Player owner)
		{
			if (Providers.Contains(trait))
				return;

			Providers.Add(trait);
			RefreshGps(owner);
		}

		public void RefreshGps(Player launcher)
		{
			RefreshGranted();

			foreach (var i in launcher.World.ActorsWithTrait<GpsASWatcher>())
				i.Trait.RefreshGranted();
		}

		void RefreshGranted()
		{
			var wasGranted = Granted;
			var wasGrantedAllies = GrantedAllies;
			var allyWatchers = owner.World.ActorsWithTrait<GpsASWatcher>().Where(kv => kv.Actor.Owner.IsAlliedWith(owner));

			Granted = Providers.Count > 0;
			GrantedAllies = allyWatchers.Any(w => w.Trait.Granted);

			if (wasGranted != Granted || wasGrantedAllies != GrantedAllies)
				foreach (var tp in notifyOnRefresh.ToList())
					tp.Trait.OnGpsASRefresh(tp.Actor, owner);
		}

		bool IPreventsShroudReset.PreventShroudReset(Actor self)
		{
			return Granted || GrantedAllies;
		}

		public void RegisterForOnGpsRefreshed(Actor actor, IOnGpsASRefreshed toBeNotified)
		{
			notifyOnRefresh.Add(new TraitPair<IOnGpsASRefreshed>(actor, toBeNotified));
		}

		public void UnregisterForOnGpsRefreshed(Actor actor, IOnGpsASRefreshed toBeNotified)
		{
			notifyOnRefresh.Remove(new TraitPair<IOnGpsASRefreshed>(actor, toBeNotified));
		}
	}
}
