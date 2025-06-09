#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Render
{
	public class WithSpawnerMasterPipsDecorationInfo : WithDecorationBaseInfo, Requires<BaseSpawnerMasterInfo>
	{
		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for spawnees stored in the spawner actor.")]
		public readonly string StoredSequence = "pip-green";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for spawnees on the field.")]
		public readonly string SpawnedSequence = "pip-yellow";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence used for lost spawnees.")]
		public readonly string EmptySequence = "pip-empty";

		[PaletteReference]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new WithSpawnerMasterPipsDecoration(init.Self, this); }
	}

	public class WithSpawnerMasterPipsDecoration : WithDecorationBase<WithSpawnerMasterPipsDecorationInfo>
	{
		readonly Animation pips;
		readonly BaseSpawnerMaster spawner;
		/*
		readonly int pipCount;
		*/

		public WithSpawnerMasterPipsDecoration(Actor self, WithSpawnerMasterPipsDecorationInfo info)
			: base(self, info)
		{
			pips = new Animation(self.World, info.Image);
			spawner = self.Trait<BaseSpawnerMaster>();
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);
			screenPos -= pipSize / 2;

			foreach (var item in spawner.SlaveEntries.Where(x => x.IsValid && !x.IsLaunched))
			{
				pips.PlayRepeating(Info.StoredSequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette, 1f);

				screenPos += pipStride;
			}

			foreach (var item in spawner.SlaveEntries.Where(x => x.IsValid && x.IsLaunched))
			{
				pips.PlayRepeating(Info.SpawnedSequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette, 1f);

				screenPos += pipStride;
			}

			foreach (var item in spawner.SlaveEntries.Where(x => !x.IsValid))
			{
				pips.PlayRepeating(Info.EmptySequence);
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette, 1f);

				screenPos += pipStride;
			}
		}
	}
}
