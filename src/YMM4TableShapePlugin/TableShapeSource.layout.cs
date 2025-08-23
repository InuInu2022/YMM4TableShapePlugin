using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Metadata;
using System.Windows.Documents;
using System.Windows.Media;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using YMM4TableShapePlugin.Enums;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using static YMM4TableShapePlugin.TableShapeSource;

namespace YMM4TableShapePlugin;

internal partial class TableShapeSource : IShapeSource2
{
	/// <summary>
	/// 一番外側のテーブルの枠線Rectを計算
	/// </summary>
	/// <param name="context"></param>
	/// <returns>一番外側のテーブルの枠線Rect</returns>
	internal static Rect CalcTableOuterBorderRect(
		TableRenderContext context
	)
	{
		var margin = context.RealOuterWidth / 2f;

		return new Rect(
			margin,
			margin,
			(float)context.Width - margin * 2,
			(float)context.Height - margin * 2
		);
	}

	internal static Rect CalculateCellRect(
		int row,
		int col,
		int rowCount,
		int colCount,
		double width,
		double height,
		double realOuterBorderWidth
	)
	{
		//grid線分の計算をいれる

		//高さと幅
		// col/rowにかかわらず同じ高さ・幅
		var cellWidth =
			(float)(
				width
				//外枠分
				- realOuterBorderWidth * 2
				//セル間分(count - 1)
				- realOuterBorderWidth * (colCount - 1)
			) / colCount;
		var cellHeight =
			(float)(
				height
				- realOuterBorderWidth * 2
				- realOuterBorderWidth * (rowCount - 1)
			) / rowCount;

		//左上の座標
		var left =
			col * cellWidth
			//外枠分
			+ (float)realOuterBorderWidth
			//セル間分
			+ (float)realOuterBorderWidth * col;
		var top =
			row * cellHeight
			+ (float)realOuterBorderWidth
			+ (float)realOuterBorderWidth * row;
		return new Rect(left, top, cellWidth, cellHeight);
	}
}
