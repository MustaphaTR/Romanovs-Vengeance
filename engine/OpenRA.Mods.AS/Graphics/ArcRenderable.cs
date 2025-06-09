#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Graphics
{
	public readonly struct ArcRenderable : IRenderable, IFinalizedRenderable
	{
		readonly Color color;
		readonly WPos b;
		readonly WAngle angle;
		readonly WDist width;
		readonly int segments;

		public ArcRenderable(WPos a, WPos b, int zOffset, WAngle angle, Color color, WDist width, int segments)
		{
			Pos = a;
			this.b = b;
			this.angle = angle;
			this.color = color;
			ZOffset = zOffset;
			this.width = width;
			this.segments = segments;
		}

		public WPos Pos { get; }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get; }
		public bool IsDecoration { get { return true; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new ArcRenderable(Pos, b, ZOffset, angle, color, width, segments); }
		public IRenderable WithZOffset(int newOffset) { return new ArcRenderable(Pos, b, ZOffset, angle, color, width, segments); }
		public IRenderable OffsetBy(in WVec vec) { return new ArcRenderable(Pos + vec, b + vec, ZOffset, angle, color, width, segments); }
		public IRenderable AsDecoration() { return this; }

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			var screenWidth = wr.ScreenVector(new WVec(width, WDist.Zero, WDist.Zero))[0];

			var points = new float3[segments + 1];
			for (var i = 0; i <= segments; i++)
				points[i] = wr.Screen3DPosition(WPos.LerpQuadratic(Pos, b, angle, i, segments));

			Game.Renderer.WorldRgbaColorRenderer.DrawLine(points, screenWidth, color, false);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
