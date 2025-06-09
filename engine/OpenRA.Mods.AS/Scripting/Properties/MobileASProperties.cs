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

using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Scripting
{
	[ScriptPropertyGroup("Movement")]
	public class MobileASProperties : ScriptActorProperties, Requires<MobileInfo>
	{
		public MobileASProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[ScriptActorPropertyActivity]
		[Desc("Move to and enter the shared transport.")]
		public void EnterSharedTransport(Actor transport)
		{
			Self.QueueActivity(new RideSharedTransport(Self, Target.FromActor(transport), null));
		}

		[ScriptActorPropertyActivity]
		[Desc("Move to and enter the transport.")]
		public void EnterGarrisonable(Actor transport)
		{
			Self.QueueActivity(new EnterGarrison(Self, Target.FromActor(transport), null));
		}
	}
}
