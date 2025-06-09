#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the turn movement speed of this actor.")]
	public class TurnSpeedMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new TurnSpeedMultiplier(this); }
	}

	public class TurnSpeedMultiplier : ConditionalTrait<TurnSpeedMultiplierInfo>, ITurnSpeedModifier
	{
		public TurnSpeedMultiplier(TurnSpeedMultiplierInfo info)
			: base(info) { }

		int ITurnSpeedModifier.GetTurnSpeedModifier() { return IsTraitDisabled ? 100 : Info.Modifier; }
	}
}
