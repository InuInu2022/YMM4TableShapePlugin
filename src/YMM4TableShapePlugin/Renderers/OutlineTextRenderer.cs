using System.Numerics;

using Vortice.Direct2D1;
using Vortice.DirectWrite;

using YMM4TableShapePlugin.Enums;

namespace YMM4TableShapePlugin.Renderers;

internal class OutlineTextRenderer(
	ID2D1DeviceContext deviceContext,
	ID2D1Brush outlineBrush,
	ID2D1Brush fillBrush,
	Vector2 origin,
	float outlineWidth,
	CellTextStyle textStyle // 追加
) : TextRendererBase
{
	readonly ID2D1DeviceContext deviceContext =
		deviceContext;
	readonly ID2D1Brush outlineBrush = outlineBrush;
	readonly ID2D1Brush fillBrush = fillBrush;
	readonly Vector2 origin = origin;
	readonly float outlineWidth = outlineWidth;
	readonly CellTextStyle textStyle = textStyle;

	static readonly StrokeStyleProperties shapedBorderStroke =
		new()
		{
			LineJoin = LineJoin.Miter,
			StartCap = CapStyle.Square,
			EndCap = CapStyle.Square,
			DashCap = CapStyle.Square,
			MiterLimit = 10.0f,
		};
	static readonly StrokeStyleProperties roundedBorderStroke =
		new()
		{
			LineJoin = LineJoin.Round,
			StartCap = CapStyle.Round,
			EndCap = CapStyle.Round,
			DashCap = CapStyle.Round,
		};

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

		var originalTransform = deviceContext.Transform;
		deviceContext.Transform =
			Matrix3x2.CreateTranslation(
				origin.X + baselineOriginX,
				origin.Y + baselineOriginY
			);

		var strokeProps = textStyle switch
		{
			CellTextStyle.ShapedBorder =>
				shapedBorderStroke,
			CellTextStyle.RoundedBorder =>
				roundedBorderStroke,
			_ => shapedBorderStroke,
		};

		using var strokeStyle =
			deviceContext.Factory.CreateStrokeStyle(
				strokeProps
			);
		deviceContext.DrawGeometry(
			pathGeometry,
			outlineBrush,
			outlineWidth * 2,
			strokeStyle
		);
		deviceContext.FillGeometry(pathGeometry, fillBrush);

		deviceContext.Transform = originalTransform;
	}
}
