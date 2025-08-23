using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using YMM4TableShapePlugin.Models;

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

		var lineJoin = textStyle switch
		{
			CellTextStyle.ShapedBorder =>
				LineJoin.MiterOrBevel,
			CellTextStyle.RoundedBorder => LineJoin.Round,
			_ => LineJoin.MiterOrBevel,
		};
		var capStyle = textStyle switch
		{
			CellTextStyle.ShapedBorder => CapStyle.Square,
			CellTextStyle.RoundedBorder => CapStyle.Round,
			_ => CapStyle.Square,
		};

		using var strokeStyle =
			deviceContext.Factory.CreateStrokeStyle(
				new()
				{
					LineJoin = lineJoin,
					StartCap = capStyle,
					EndCap = capStyle,
					DashCap = capStyle,
				}
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
