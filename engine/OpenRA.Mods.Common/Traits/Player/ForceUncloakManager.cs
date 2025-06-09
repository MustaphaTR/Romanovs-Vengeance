using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Attach this to the player actor to allow cloak actors can be forced reveal, if player only has actors can be force uncloack.")]
	public sealed class ForceUncloakManagerInfo : ConditionalTraitInfo
	{
		[Desc("Scan interval. Set it longer for performace.")]
		public readonly int ScanInterval = 51;

		[Desc("Does force uncloak reversible?")]
		public readonly bool Irreversible = true;

		[Desc("Ignore those actors when checking, like some of the proxy and dummy actors")]
		public readonly HashSet<string> IgnoreActors = new();

		[Desc("The duration before force uncloak when there are only units can be forced uncloak. Set to < 0 can skip warning.")]
		public readonly int DurationBeforeForceUncloak = 2000;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string ForceUncloakNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification the perpetrator will see after successful infiltration.")]
		public readonly string ForceUncloakTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string ForceUncloakWarningNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification the perpetrator will see after successful infiltration.")]
		public readonly string ForceUncloakWarningTextNotification = null;

		public override object Create(ActorInitializer init) { return new ForceUncloakManager(this, init.Self); }
	}

	public sealed class ForceUncloakManager : ConditionalTrait<ConditionalTraitInfo>, ITick, INotifyCreated
	{
		readonly World world;
		readonly ForceUncloakManagerInfo info;
		bool forcedUncloakWarning;
		int scanInterval;
		int remainingWarningtime;

		public bool ForcedUncloak { get; private set; }

		public ForceUncloakManager(ForceUncloakManagerInfo info, Actor self)
			: base(info)
		{
			this.info = info;
			world = self.World;
		}

		protected override void Created(Actor self)
		{
			scanInterval = world.SharedRandom.Next(1, info.ScanInterval);
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || (info.Irreversible && ForcedUncloak) || --scanInterval > 0)
				return;

			// When there are only actors that can be forced uncloak, first warning then force uncloak
			if (world.ActorsHavingTrait<IOccupySpace>().Where(a => a.Owner == self.Owner && !info.IgnoreActors.Contains(a.Info.Name) && a.IsInWorld && !a.IsDead)
				.All(a => a.TraitsImplementing<Cloak>().Any(c => c.Info.CanBeForcedUncloak && !c.IsTraitDisabled)))
			{
				if (!forcedUncloakWarning)
				{
					forcedUncloakWarning = true;
					remainingWarningtime = info.DurationBeforeForceUncloak;

					// Show warning notification if we can show warning
					if (info.DurationBeforeForceUncloak > 0)
					{
						if (info.ForceUncloakWarningNotification != null)
							Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ForceUncloakWarningNotification, self.Owner.Faction.InternalName);

						TextNotificationsManager.AddTransientLine(self.Owner, info.ForceUncloakWarningTextNotification);

						scanInterval = Math.Min(remainingWarningtime, info.ScanInterval);
					}
					else
						scanInterval = 0;
				}
				else if (!ForcedUncloak)
				{
					remainingWarningtime -= info.ScanInterval;
					if (remainingWarningtime < 0)
					{
						ForcedUncloak = true;

						if (info.ForceUncloakNotification != null)
							Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ForceUncloakNotification, self.Owner.Faction.InternalName);

						TextNotificationsManager.AddTransientLine(self.Owner, info.ForceUncloakTextNotification);
						return;
					}

					scanInterval = Math.Min(remainingWarningtime, info.ScanInterval);
				}
				else
					scanInterval = info.ScanInterval;
			}

			// When there is any actors cannot be forced uncloak, restore the check
			else
			{
				scanInterval = info.ScanInterval;
				remainingWarningtime = info.DurationBeforeForceUncloak;
				forcedUncloakWarning = false;
				ForcedUncloak = false;
			}
		}
	}
}
