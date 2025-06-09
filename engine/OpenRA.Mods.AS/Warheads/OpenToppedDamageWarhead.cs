#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Deals damage to the passengers/garrisoners of the target actors.")]
	public class OpenToppedDamageWarhead : TargetDamageWarhead
	{
		[Desc("Amount of garrisoners that will be affected, use -1 to affect all.")]
		public readonly int Amount = -1;

		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			var validTraits = victim.TraitsImplementing<INotifyPassengersDamage>();
			foreach (var trait in validTraits)
			{
				trait.DamagePassengers(Damage, firedBy, Amount, Versus, DamageTypes, args.DamageModifiers);
			}
		}
	}
}
