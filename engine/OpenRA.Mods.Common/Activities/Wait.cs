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

using System;
using OpenRA.Activities;

namespace OpenRA.Mods.Common.Activities
{
	public class Wait : Activity
	{
		int remainingTicks;

		public Wait(int period) { ActivityType = ActivityType.Undefined; remainingTicks = period; }
		public Wait(int period, bool interruptible)
		{
			ActivityType = ActivityType.Undefined;
			remainingTicks = period;
			IsInterruptible = interruptible;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			return remainingTicks-- == 0;
		}
	}

	public class WaitFor : Activity
	{
		readonly Func<bool> f;

		public WaitFor(Func<bool> f) { this.f = f; ActivityType = ActivityType.Undefined; }
		public WaitFor(Func<bool> f, bool interruptible)
		{
			ActivityType = ActivityType.Undefined;
			this.f = f;
			IsInterruptible = interruptible;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			return f == null || f();
		}
	}
}
