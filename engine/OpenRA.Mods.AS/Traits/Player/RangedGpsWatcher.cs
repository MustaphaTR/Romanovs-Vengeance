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
	[Desc("Required for Ranged GPS-related logic to function. Attach this to the player actor.")]
	public class RangedGpsWatcherInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new RangedGpsWatcher(init.Self.Owner); }
	}

	public interface IOnRangedGpsRefreshed { void OnRangedGpsRefresh(Actor self, Player player); }

	public class RangedGpsWatcher : ISync, IPreventsShroudReset
	{
		[Sync]
		public bool GrantedAllies { get; private set; }

		[Sync]
		public bool Granted { get; private set; }

		readonly Player owner;

		public readonly List<RangedGpsProvider> Providers = new();
		readonly HashSet<TraitPair<IOnRangedGpsRefreshed>> notifyOnRefresh = new();

		public RangedGpsWatcher(Player owner)
		{
			this.owner = owner;
		}

		public void DeactivateGps(RangedGpsProvider trait, Player owner)
		{
			if (!Providers.Contains(trait))
				return;

			Providers.Remove(trait);
			RefreshGps(owner);
		}

		public void ActivateGps(RangedGpsProvider trait, Player owner)
		{
			if (Providers.Contains(trait))
				return;

			Providers.Add(trait);
			RefreshGps(owner);
		}

		public void RefreshGps(Player launcher)
		{
			RefreshGranted();

			foreach (var i in launcher.World.ActorsWithTrait<RangedGpsWatcher>())
				i.Trait.RefreshGranted();
		}

		void RefreshGranted()
		{
			var wasGranted = Granted;
			var wasGrantedAllies = GrantedAllies;
			var allyWatchers = owner.World.ActorsWithTrait<RangedGpsWatcher>().Where(kv => kv.Actor.Owner.IsAlliedWith(owner));

			Granted = Providers.Count > 0;
			GrantedAllies = allyWatchers.Any(w => w.Trait.Granted);

			if (wasGranted != Granted || wasGrantedAllies != GrantedAllies)
				foreach (var tp in notifyOnRefresh.ToList())
					tp.Trait.OnRangedGpsRefresh(tp.Actor, owner);
		}

		bool IPreventsShroudReset.PreventShroudReset(Actor self)
		{
			return Granted || GrantedAllies;
		}

		public void RegisterForOnGpsRefreshed(Actor actor, IOnRangedGpsRefreshed toBeNotified)
		{
			notifyOnRefresh.Add(new TraitPair<IOnRangedGpsRefreshed>(actor, toBeNotified));
		}

		public void UnregisterForOnGpsRefreshed(Actor actor, IOnRangedGpsRefreshed toBeNotified)
		{
			notifyOnRefresh.Remove(new TraitPair<IOnRangedGpsRefreshed>(actor, toBeNotified));
		}
	}
}
