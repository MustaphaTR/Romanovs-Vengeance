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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	sealed class WithDisguisingSpriteTurretInfo : WithSpriteTurretInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new WithDisguisingSpriteTurret(init.Self, this); }
	}

	sealed class WithDisguisingSpriteTurret : WithSpriteTurret, ITick
	{
		readonly Disguise disguise;
		readonly RenderSprites rs;
		readonly Turreted t;
		string intendedSprite;

		public WithDisguisingSpriteTurret(Actor self, WithDisguisingSpriteTurretInfo info)
			: base(self, info)
		{
			rs = self.Trait<RenderSprites>();
			t = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == info.Turret);
			disguise = self.Trait<Disguise>();
			intendedSprite = disguise.AsSprite;
		}

		void ITick.Tick(Actor self)
		{
			if (disguise.AsSprite != intendedSprite)
			{
				intendedSprite = disguise.AsSprite;
				DefaultAnimation.ChangeImage(intendedSprite ?? rs.GetImage(self), DefaultAnimation.CurrentSequence.Name);
				rs.UpdatePalette();

				// Restrict turret facings to match the sprite
				t.QuantizedFacings = DefaultAnimation.CurrentSequence.Facings;
			}
		}
	}
}
