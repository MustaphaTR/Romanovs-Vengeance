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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	sealed class WithDisguisingInfantryBodyInfo : WithInfantryBodyInfo, Requires<DisguiseInfo>
	{
		public override object Create(ActorInitializer init) { return new WithDisguisingInfantryBody(init, this); }
	}

	sealed class WithDisguisingInfantryBody : WithInfantryBody
	{
		readonly Disguise disguise;
		readonly RenderSprites rs;
		WithInfantryBodyInfo disguiseInfantryBody;
		string intendedSprite;

		public WithDisguisingInfantryBody(ActorInitializer init, WithDisguisingInfantryBodyInfo info)
			: base(init, info)
		{
			rs = init.Self.Trait<RenderSprites>();
			disguise = init.Self.Trait<Disguise>();
			intendedSprite = disguise.AsSprite;
		}

		protected override WithInfantryBodyInfo GetDisplayInfo()
		{
			return disguiseInfantryBody ?? Info;
		}

		protected override void Tick(Actor self)
		{
			if (disguise.AsSprite != intendedSprite && !IsTraitDisabled)
			{
				// Force actor back to the stand state to avoid mismatched sequences
				PlayStandAnimation(self);

				var infantryBodies = disguise.AsActor.TraitInfos<WithInfantryBodyInfo>();
				var infantryBody = infantryBodies.FirstOrDefault(t => t.Name == Info.Name) ?? infantryBodies.FirstOrDefault(t => t.EnabledByDefault);
				if (infantryBody != null)
					disguiseInfantryBody = infantryBody;

				intendedSprite = disguise.AsSprite;
				var sequence = DefaultAnimation.GetRandomExistingSequence(GetDisplayInfo().StandSequences, Game.CosmeticRandom);
				if (sequence != null)
					DefaultAnimation.ChangeImage(intendedSprite ?? rs.GetImage(self), sequence);

				rs.UpdatePalette();
			}

			base.Tick(self);
		}
	}
}
