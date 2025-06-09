#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor provides AS GPS.")]
	public class GpsASProviderInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new GpsASProvider(this); }
	}

	public class GpsASProvider : ConditionalTrait<GpsASProviderInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		public GpsASProvider(GpsASProviderInfo info)
			: base(info) { }

		GpsASWatcher watcher;

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			watcher = self.Owner.PlayerActor.Trait<GpsASWatcher>();

			if (!IsTraitDisabled)
				TraitEnabled(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!IsTraitDisabled)
				TraitDisabled(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			watcher.ActivateGps(this, self.Owner);
		}

		protected override void TraitDisabled(Actor self)
		{
			watcher.DeactivateGps(this, self.Owner);
		}
	}
}
