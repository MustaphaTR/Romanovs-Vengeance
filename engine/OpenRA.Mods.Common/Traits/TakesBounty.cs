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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor takes bounty from actors with Gives Bounty trait with same Type.")]
	public class TakesBountyInfo : ConditionalTraitInfo
	{
		[Desc("Percentage of the killed actor's Cost or CustomSellValue to be given.")]
		public readonly int Percentage = 10;

		[Desc("Scale bounty based on the veterancy of the killed unit. The value is given in percent.")]
		public readonly int LevelMod = 125;

		[Desc("Accepted `Gives Bounty` types. Leave empty to accept all types.")]
		public readonly HashSet<string> ValidTypes = new() { "Bounty" };

		public override object Create(ActorInitializer init) { return new TakesBounty(this); }
	}

	public class TakesBounty : ConditionalTrait<TakesBountyInfo>
	{
		public TakesBounty(TakesBountyInfo info)
			: base(info) { }
	}
}
