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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("To produce some specific actors, this trait should be enabled on the actor.")]
	public class ConditionPrerequisiteInfo : PausableConditionalTraitInfo, Requires<ProductionQueueInfo>
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor that this condition will apply.")]
		public readonly string Actor = null;

		[FieldLoader.Require]
		[Desc("Queues that this condition will apply.")]
		public readonly HashSet<string> Queue = new();

		public override object Create(ActorInitializer init) { return new ConditionPrerequisite(init.Self, this); }
	}

	public class ConditionPrerequisite : PausableConditionalTrait<ConditionPrerequisiteInfo>, INotifyCreated
	{
		readonly ProductionQueue[] queues;

		public ConditionPrerequisite(Actor self, ConditionPrerequisiteInfo info)
			: base(info)
		{
			queues = self.TraitsImplementing<ProductionQueue>().Where(t => Info.Queue.Contains(t.Info.Type)).ToArray();
		}

		protected override void Created(Actor self)
		{
			if (Info.RequiresCondition == null)
			{
				foreach (var queue in queues.Where(t => t.Enabled))
				{
					queue.CacheProducibles();
					queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Visible = true;
					if (!IsTraitPaused)
						queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Buildable = true;
				}
			}

			if (IsTraitDisabled)
			{
				foreach (var queue in queues.Where(t => t.Enabled))
				{
					queue.CacheProducibles();
					queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Visible = false;
				}
			}

			base.Created(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			foreach (var queue in queues.Where(t => t.Enabled))
			{
				queue.CacheProducibles();
				queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Visible = true;
				if (!IsTraitPaused)
					queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Buildable = true;
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			foreach (var queue in queues.Where(t => t.Enabled))
			{
				queue.CacheProducibles();
				queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Visible = false;
			}
		}

		protected override void TraitPaused(Actor self)
		{
			foreach (var queue in queues.Where(t => t.Enabled))
			{
				queue.CacheProducibles();
				queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Buildable = false;
			}
		}

		protected override void TraitResumed(Actor self)
		{
			foreach (var queue in queues.Where(t => t.Enabled))
			{
				queue.CacheProducibles();
				queue.Producible[self.World.Map.Rules.Actors[Info.Actor]].Buildable = true;
			}
		}
	}
}
