using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace YMM4TableShapePlugin.Renderers;

internal class OutlineTextRenderer(
	ID2D1DeviceContext deviceContext,
	ID2D1Brush outlineBrush,
	ID2D1Brush fillBrush,
	Vector2 origin,
	float outlineWidth
) : TextRendererBase
{
	readonly ID2D1DeviceContext deviceContext =
		deviceContext;
	readonly ID2D1Brush outlineBrush = outlineBrush;
	readonly ID2D1Brush fillBrush = fillBrush;
	readonly Vector2 origin = origin;
	readonly float outlineWidth = outlineWidth;

	public override void DrawGlyphRun(
		IntPtr clientDrawingContext,
		float baselineOriginX,
		float baselineOriginY,
		Vortice.DCommon.MeasuringMode measuringMode,
		Vortice.DirectWrite.GlyphRun glyphRun,
		GlyphRunDescription glyphRunDescription,
		SharpGen.Runtime.IUnknown clientDrawingEffect
	)
	{
		using var pathGeometry =
			deviceContext.Factory.CreatePathGeometry();
		using var sink = pathGeometry.Open();

		glyphRun.FontFace?.GetGlyphRunOutline(
			glyphRun.FontEmSize,
			glyphRun.Indices,
			glyphRun.Advances,
			glyphRun.Offsets,
			glyphRun.IsSideways,
			(glyphRun.BidiLevel & 1) == 1,
			sink
		);
		sink.Close();

		// 座標補正: origin + baselineOriginX/Y
		var originalTransform = deviceContext.Transform;
		deviceContext.Transform =
			Matrix3x2.CreateTranslation(
				origin.X + baselineOriginX,
				origin.Y + baselineOriginY
			);

		deviceContext.DrawGeometry(
			pathGeometry,
			outlineBrush,
			outlineWidth
		);
		deviceContext.FillGeometry(pathGeometry, fillBrush);

		deviceContext.Transform = originalTransform;
	}
}
