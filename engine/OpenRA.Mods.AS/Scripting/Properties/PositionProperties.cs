#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Scripting;

namespace OpenRA.Mods.AS.Scripting
{
	[ScriptPropertyGroup("General")]
	public class PositionProperties : ScriptActorProperties
	{
		public PositionProperties(ScriptContext context, Actor self)
			: base(context, self) { }

		[Desc("Is the actor at ground.")]
		public bool IsAtGroundLevel()
		{
			return Self.IsAtGroundLevel();
		}
	}
}
