using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using YMM4TableShapePlugin.Enums;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin;

internal partial class TableShapeSource
{
	/// <summary>
	/// キャッシュ判定用の構造体
	/// </summary>
	/// <param name="Model"></param>
	/// <param name="BorderWidth"></param>
	/// <param name="Row"></param>
	/// <param name="Col"></param>
	/// <param name="Width"></param>
	/// <param name="Height"></param>
	/// <param name="BorderColor"></param>
	/// <param name="BackgroundColor"></param>
	/// <param name="OuterBorderWidth"></param>
	/// <param name="OuterBorderColor"></param>

	readonly record struct TableDrawCache(
		TableModel Model,
		double BorderWidth,
		int Row,
		int Col,
		double Width,
		double Height,
		Color BorderColor,
		Color BackgroundColor,
		double OuterBorderWidth,
		Color OuterBorderColor,
		ShowHeader HeaderDisplay,
		Color HeaderRowBackgroundColor,
		Color HeaderColumnBackgroundColor
	);

	/// <summary>
	/// コレクションキャッシュ判定用の構造体
	/// </summary>
	/// <param name="RowBoundaries"></param>
	/// <param name="ColumnBoundaries"></param>
	/// <param name="TextLists"></param>
	/// <param name="CellLists"></param>
	readonly record struct TableDrawCollections(
		ImmutableList<Animation> RowBoundaries,
		ImmutableList<Animation> ColumnBoundaries,
		IEnumerable<string> TextLists,
		ImmutableList<TableCell> CellLists
	)
	{
		public bool Equals(TableDrawCollections other)
		{
			return RowBoundaries.SequenceEqual(
					other.RowBoundaries
				)
				&& ColumnBoundaries.SequenceEqual(
					other.ColumnBoundaries
				)
				&& TextLists.SequenceEqual(
					other.TextLists,
					StringComparer.Ordinal
				)
				&& CellLists.SequenceEqual(other.CellLists);
		}

		public override int GetHashCode()
		{
			var hash = new HashCode();
			foreach (var item in RowBoundaries)
				hash.Add(item);
			foreach (var item in ColumnBoundaries)
				hash.Add(item);
			foreach (var item in TextLists)
				hash.Add(item);
			foreach (var item in CellLists)
				hash.Add(item);
			return hash.ToHashCode();
		}
	}

	/// <summary>
	/// テーブル描画に必要なパラメータをまとめた構造体
	/// </summary>
	internal readonly record struct TableRenderContext(
		int Frame,
		int Length,
		int Fps,
		TableModel Model,
		double BorderWidth,
		double OuterBorderWidth,
		ID2D1DeviceContext6 DeviceContext,
		ID2D1SolidColorBrush? CellBackgroundBrush,
		ID2D1SolidColorBrush? TextBrush,
		IDWriteFactory? WriteFactory,
		double Width,
		double Height,
		[Range(1, int.MaxValue)]
		int RowCount,
		[Range(1, int.MaxValue)]
		int ColCount,
		float RealOuterWidth,
		float OuterMargin,
		Color BackgroundColor,
		Color BorderColor,
		Color OuterBorderColor,
		Color? HeaderRowBackgroundColor = null,
		Color? HeaderColumnBackgroundColor = null
	);
}
