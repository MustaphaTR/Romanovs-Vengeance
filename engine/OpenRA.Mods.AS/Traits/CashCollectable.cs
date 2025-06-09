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
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Traits
{
	public class CashCollectableType { }

	[Desc("Tag trait for CashCollector to function.")]
	public class CashCollectableInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		public readonly int Value;

		[FieldLoader.Require]
		public readonly BitSet<CashCollectableType> Types = default;

		public override object Create(ActorInitializer init) { return new CashCollectable(this); }
	}

	public class CashCollectable : ConditionalTrait<CashCollectableInfo>
	{
		public CashCollectable(CashCollectableInfo info)
			: base(info) { }
	}
}
