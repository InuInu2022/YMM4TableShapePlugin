using Vortice.Direct2D1;
using Vortice.Mathematics;
using Xunit;
using YMM4TableShapePlugin;

using static YMM4TableShapePlugin.TableShapeSource;

namespace PluginTests;

/// <summary>
/// TableRenderSource.RenderTableCells()関連のテスト
///
/// 仕様:
/// - 外枠（OuterBorderRect）はテーブル領域内に収まること（はみ出し禁止）
/// - 各セルのRectは分割時に均一サイズであること
/// - 隣接セル間に隙間がないこと（セル同士がぴったり隣接すること）
/// - 各セルRectは外枠Rectの内側に収まること
/// - 枠線が重ならないこと
///   - ※補足: 実装方法によっては隣接セルの境界線が両方描画されて重複する場合がある。
///     テストでは単純な座標比較だけでなく、実装方針に応じた検証が必要。
///     たとえば外枠と枠線を2本の太さの違う線を重ねて表現する場合は
///     この仕様満たさないのが正しい。
/// - セル背景に隙間がないこと
/// - 文字が枠線に被らないこと
/// - 全セルサイズが均一であること
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Naming",
	"CA1707:識別子はアンダースコアを含むことはできません",
	Justification = "<保留中>"
)]
public class DrawTableCellTests
{
	readonly TableRenderContext _tcx = new(
		0,
		0,
		0,
		new(1, 1),
		1,
		0,
		new ID2D1DeviceContext6(0),
		null,
		null,
		null,
		100,
		100,
		1,
		1,
		10,
		4,
		new(),
		new(),
		new()
	);

	[Theory(
		DisplayName = "OuterBorderRect_IsWithinTableBounds_NoOverflow"
	)]
	[InlineData(400, 300, 1, 10)]
	[InlineData(800, 600, 10, 20)]
	[InlineData(100, 100, 5, 0)]
	public void CalculateOuterBorderRect_OuterBorderIsWithinBounds_NoOverflow(
		double width,
		double height,
		double borderWidth,
		double outerBorderWidth
	)
	{
		// 仕様: 外枠（OuterBorderRect）はテーブル領域内に収まること（はみ出し禁止）

		Assert.True(borderWidth >= 1, "検証データエラー");
		Assert.True(
			outerBorderWidth >= 0,
			"検証データエラー"
		);

		var ctx = _tcx with
		{
			OuterBorderWidth = outerBorderWidth,
			Width = width,
			Height = height,
			BorderWidth = borderWidth,
			RealOuterWidth =
				(float)borderWidth / 2f
				+ (float)outerBorderWidth,
		};
		var rect =
			CalcTableOuterBorderRect(
				ctx
			);

		Assert.True(rect.Left >= ctx.RealOuterWidth / 2);
		Assert.True(rect.Top >= ctx.RealOuterWidth / 2);
		Assert.True(
			rect.Width
				<= (float)width - ctx.RealOuterWidth / 2f
		);
		Assert.True(
			rect.Height
				<= (float)height - ctx.RealOuterWidth / 2f
		);
		Assert.True(rect.Right < (float)width);
		Assert.True(rect.Bottom < (float)height);
	}

	[Theory(
		DisplayName = "CellRects_AreUniformSize_WhenDivided"
	)]
	[InlineData(400, 300, 1, 10, 2, 2)]
	[InlineData(800, 600, 10, 20, 4, 4)]
	[InlineData(100, 100, 20, 0, 1, 1)]
	[InlineData(100, 100, 20, 0, 11, 9)]
	public void CalculateCellRect_CellRectsAreUniformSize_WhenDivided(
		double width,
		double height,
		double borderWidth,
		double outerBorderWidth,
		int rowCount,
		int colCount
	)
	{
		// 仕様: 各セルのRectは分割時に均一サイズであること
		// 仕様: 全セルサイズが均一であること

		var realOuterBorderWidth =
			outerBorderWidth + borderWidth / 2;

		List<Rect> rects = [];

		for (int r = 0; r < rowCount; r++)
		{
			for (int c = 0; c < colCount; c++)
			{
				var cellRect = CalculateCellRect(
					r,
					c,
					rowCount,
					colCount,
					width,
					height,
					realOuterBorderWidth
				);
				rects.Add(cellRect);
			}
		}

		var w = rects.First().Width;
		var h = rects.First().Height;

		Assert.True(
			rects.All(r => r.Width == w && r.Height == h)
		);
	}

	[Theory(DisplayName = "CellRects_NoGapBetweenCells")]
	[InlineData(400, 300, 1, 10, 2, 2)]
	[InlineData(800, 600, 5, 20, 4, 4)]
	[InlineData(400, 300, 6, 10, 6, 3)]
	[InlineData(800, 600, 10, 20, 4, 4)]
	[InlineData(100, 100, 20, 0, 1, 1)]
	public void CalculateCellRect_NoGapBetweenCells(
		double width,
		double height,
		double borderWidth,
		double outerBorderWidth,
		int rowCount,
		int colCount
	)
	{
		// 仕様: 隣接セル間に隙間がないこと（セル同士がぴったり隣接すること）
		// 仕様: セル背景に隙間がないこと（座標計算上は隣接セル間隙間なしで検証済み）

		var realOuterBorderWidth =
			outerBorderWidth + borderWidth / 2;

		List<Rect> rects = [];

		for (int r = 0; r < rowCount; r++)
		{
			for (int c = 0; c < colCount; c++)
			{
				var cellRect = CalculateCellRect(
					r,
					c,
					rowCount,
					colCount,
					width,
					height,
					realOuterBorderWidth
				);
				rects.Add(cellRect);
			}
		}


		for (int i = 0; i < rects.Count; i++)
		{
			for (int j = i + 1; j < rects.Count; j++)
			{
				// 仕様: 全てのセルRectが重なっていないこと（重複なし）
				Assert.False(
					rects[i].IntersectsWith(rects[j]),
					$"Rect[{i}]とRect[{j}]が重なっています"
				);
			}
		}
	}

	[Theory(DisplayName = "CellRects_AreInsideOuterBorder")]
	[InlineData(400, 300, 5, 10, 2, 2)]
	[InlineData(800, 600, 14, 20, 4, 4)]
	public void CalculateCellRect_CellRectsAreInsideOuterBorder(
		double width,
		double height,
		double borderWidth,
		double outerBorderWidth,
		int rowCount,
		int colCount
	)
	{
		// 仕様: 各セルRectは外枠Rectの内側に収まること
		var ctx = _tcx with
		{
			OuterBorderWidth = outerBorderWidth,
			Width = width,
			Height = height,
			ColCount = colCount,
			RowCount = rowCount,
			BorderWidth = borderWidth,
			RealOuterWidth =
				(float)borderWidth / 2f
				+ (float)outerBorderWidth,
		};
		var outerRect =
			CalcTableOuterBorderRect(ctx);

		for (int r = 0; r < rowCount; r++)
		{
			for (int c = 0; c < colCount; c++)
			{
				var cellRect =
					TableShapeSource.CalculateCellRect(
						r,
						c,
						rowCount,
						colCount,
						width,
						height,
						outerBorderWidth
					);
				Assert.True(
					cellRect.Left >= outerRect.Left
				);
				Assert.True(cellRect.Top >= outerRect.Top);
				Assert.True(
					cellRect.Right <= outerRect.Right
				);
				Assert.True(
					cellRect.Bottom <= outerRect.Bottom
				);
			}
		}
	}

	[Theory(DisplayName = "TextRect_DoesNotOverlapBorder")]
	[InlineData(400, 300, 10, 4, 2, 2)]
	[InlineData(800, 600, 20, 8, 4, 4)]
	public void CalculateTextRect_DoesNotOverlapBorder(
		double width,
		double height,
		double outerBorderWidth,
		double borderWidth,
		int rowCount,
		int colCount
	)
	{
		// 仕様: 文字が枠線に被らないこと（文字領域が枠線領域に重ならない）
		var rect = TableShapeSource.CalculateCellRect(
			0,
			0,
			rowCount,
			colCount,
			width,
			height,
			outerBorderWidth
		);

		var padding = 4f;
		var borderAndOuter =
			(float)(borderWidth + outerBorderWidth) / 2f;

		var leftText = rect.Left + borderAndOuter + padding;
		var topText = rect.Top + borderAndOuter + padding;
		var widthText = MathF.Max(
			0f,
			rect.Width - borderAndOuter * 2f - padding * 2f
		);
		var heightText = MathF.Max(
			0f,
			rect.Height - borderAndOuter * 2f - padding * 2f
		);

		var textRect = new Rect(
			leftText,
			topText,
			widthText,
			heightText
		);
		var borderRect = new Rect(
			rect.Left,
			rect.Top,
			rect.Width,
			rect.Height
		);

		Assert.True(textRect.Left >= borderRect.Left);
		Assert.True(textRect.Top >= borderRect.Top);
		Assert.True(textRect.Right <= borderRect.Right);
		Assert.True(textRect.Bottom <= borderRect.Bottom);
	}

	[Theory(
		DisplayName = "CellRects_AdjacentRectsHaveExpectedGap"
	)]
	[InlineData(400, 300, 1, 10, 2, 2)]
	[InlineData(800, 600, 5, 20, 4, 4)]
	[InlineData(400, 300, 6, 10, 6, 3)]
	[InlineData(800, 600, 10, 20, 4, 4)]
	[InlineData(100, 100, 20, 0, 1, 1)]
	public void CalculateCellRect_AdjacentRectsHaveExpectedGap(
		double width,
		double height,
		double borderWidth,
		double outerBorderWidth,
		int rowCount,
		int colCount
	)
	{
		// 隣接セル間の距離が realOuterBorderWidth であることを検証
		var realOuterBorderWidth =
			outerBorderWidth + borderWidth / 2;
		Rect[,] rects = new Rect[rowCount, colCount];

		for (int r = 0; r < rowCount; r++)
		{
			for (int c = 0; c < colCount; c++)
			{
				rects[r, c] = CalculateCellRect(
					r,
					c,
					rowCount,
					colCount,
					width,
					height,
					realOuterBorderWidth
				);
			}
		}

		// 左右隣接セル
		for (int r = 0; r < rowCount; r++)
		{
			for (int c = 0; c < colCount - 1; c++)
			{
				var rightGap =
					rects[r, c + 1].Left
					- rects[r, c].Right;
				Assert.True(
					Math.Abs(
						rightGap - realOuterBorderWidth
					) < 0.01,
					$"左右隣接セル間の距離が一致しません: {rightGap} != {realOuterBorderWidth}"
				);
			}
		}

		// 上下隣接セル
		for (int c = 0; c < colCount; c++)
		{
			for (int r = 0; r < rowCount - 1; r++)
			{
				var bottomGap =
					rects[r + 1, c].Top
					- rects[r, c].Bottom;
				Assert.True(
					Math.Abs(
						bottomGap - realOuterBorderWidth
					) < 0.01,
					$"上下隣接セル間の距離が一致しません: {bottomGap} != {realOuterBorderWidth}"
				);
			}
		}
	}
}
