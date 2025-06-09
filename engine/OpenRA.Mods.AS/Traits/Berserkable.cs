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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When enabled, the actor will randomly try to attack nearby allied actors.")]
	public class BerserkableInfo : ConditionalTraitInfo
	{
		[Desc("Do not attack this type of actors when berserked.")]
		public readonly BitSet<TargetableType> TargetTypesToIgnore;

		public override object Create(ActorInitializer init) { return new Berserkable(this); }
	}

	class Berserkable : ConditionalTrait<BerserkableInfo>, INotifyIdle, INotifyCreated
	{
		AttackBase[] attackBases;
		AutoTarget[] autoTargets;

		public Berserkable(BerserkableInfo info)
			: base(info) { }

		static void Blink(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				if (self.IsInWorld)
				{
					var stop = new Order("Stop", self, false);
					foreach (var t in self.TraitsImplementing<IResolveOrder>())
						t.ResolveOrder(self, stop);

					w.Remove(self);
					self.Generation++;
					w.Add(self);
				}
			});
		}

		protected override void Created(Actor self)
		{
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
			autoTargets = self.TraitsImplementing<AutoTarget>().ToArray();
			base.Created(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			// Getting enraged cancels current activity.
			Blink(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			// Getting unraged should drop the target, too.
			Blink(self);
		}

		WDist GetScanRange(List<AttackBase> atbs)
		{
			var range = WDist.Zero;

			// Get max value of autotarget scan range.
			foreach (var at in autoTargets.Where(a => !a.IsTraitDisabled))
			{
				var r = at.Info.ScanRadius;
				if (r > range.Length)
					range = WDist.FromCells(r);
			}

			// Get maxrange weapon.
			foreach (var atb in atbs)
			{
				var r = atb.GetMaximumRange();
				if (r.Length > range.Length)
					range = r;
			}

			return range;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var atbs = attackBases.Where(a => !a.IsTraitDisabled && !a.IsTraitPaused).ToList();
			if (atbs.Count == 0)
			{
				self.QueueActivity(new Wait(15));
				return;
			}

			var range = GetScanRange(atbs);

			var targets = self.World.FindActorsInCircle(self.CenterPosition, range)
				.Where(a => a != self && a.IsTargetableBy(self) && !Info.TargetTypesToIgnore.Overlaps(a.GetEnabledTargetTypes())).ToArray();

			var preferredtargets = targets.Where(a => a.Owner.IsAlliedWith(self.Owner));

			if (!preferredtargets.Any())
			{
				preferredtargets = targets.Where(a => !a.Owner.IsAlliedWith(self.Owner));

				if (!preferredtargets.Any())
				{
					self.QueueActivity(new Wait(15));
					return;
				}
			}

			// Attack a random target.
			var target = Target.FromActor(preferredtargets.Random(self.World.SharedRandom));
			self.QueueActivity(atbs[0].GetAttackActivity(self, AttackSource.Default, target, true, true));
		}
	}
}
