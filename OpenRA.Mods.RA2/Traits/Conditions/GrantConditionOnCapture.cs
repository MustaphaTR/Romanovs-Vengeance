#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA2.Traits
{
	[Desc("Grants a condition after actor gets captures..")]
	public class GrantConditionOnCaptureInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant")]
		public readonly string Condition = null;

		[Desc("Grant condition only if the capturer's CaptureTypes overlap with these types. Leave empty to allow all types.")]
		public readonly BitSet<CaptureType> CaptureTypes = default(BitSet<CaptureType>);

		public object Create(ActorInitializer init) { return new GrantConditionOnCapture(init.Self, this); }
	}

	public class GrantConditionOnCapture : INotifyCreated, INotifyCapture
	{
		readonly GrantConditionOnCaptureInfo info;
		ConditionManager manager;

		int token = ConditionManager.InvalidConditionToken;

		public GrantConditionOnCapture(Actor self, GrantConditionOnCaptureInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void GrantCondition(Actor self, string cond)
		{
			if (manager == null)
				return;

			if (string.IsNullOrEmpty(cond))
				return;

			if (token == ConditionManager.InvalidConditionToken)
				token = manager.GrantCondition(self, cond);
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
		{
			if (info.CaptureTypes.IsEmpty || info.CaptureTypes.Overlaps(captureTypes))
				GrantCondition(self, info.Condition);
		}
	}
}
