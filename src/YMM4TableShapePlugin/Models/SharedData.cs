using System.Collections.Immutable;
using System.Windows.Media;

using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin.Models;

// ShapeParameterBaseを継承した*Parameterのプロパティを保存しておくクラス
// 図形データ切り替わったときにもデータを保持しておくため
internal class SharedData
{
	Animation Width { get; } = new(500, 0, 10000);
	Animation Height { get; } = new(300, 0, 10000);
	Animation RowCount { get; } = new(1, 1, 100);
	Animation ColumnCount { get; } = new(1, 1, 100);
	Animation BorderWidth { get; } = new(1, 1, 100000);
	Animation OuterBorderWidth { get; } = new(0, 0, 100000);
	Color BorderColor { get; }
	Color OuterBorderColor { get; }
	Color BackgroundColor { get; }

	TableModel TableModel { get; }
	ShowHeader HeaderDisplay { get; }
	Color HeaderRowBackgroundColor { get; }
	Color HeaderColumnBackgroundColor { get; }

	// TableModelから区切り情報を取得
	public ImmutableList<Animation> RowBoundaries { get; }
	public ImmutableList<Animation> ColumnBoundaries { get; }

	public SharedData(TableShapeParameter parameter)
	{
		Width?.CopyFrom(parameter.Width);
		Height?.CopyFrom(parameter.Height);
		RowCount?.CopyFrom(parameter.RowCount);
		ColumnCount?.CopyFrom(parameter.ColumnCount);
		BorderWidth?.CopyFrom(parameter.BorderWidth);
		OuterBorderWidth?.CopyFrom(
			parameter.OuterBorderWidth
		);
		BorderColor = parameter.BorderColor;
		OuterBorderColor = parameter.OuterBorderColor;
		BackgroundColor = parameter.BackgroundColor;

		HeaderDisplay = parameter.HeaderDisplay;
		HeaderRowBackgroundColor =
			parameter.HeaderRowBackgroundColor;
		HeaderColumnBackgroundColor =
			parameter.HeaderColumnBackgroundColor;

		TableModel = parameter.TableModel;
		RowBoundaries =
		[.. parameter.TableModel.RowBoundaries];
		ColumnBoundaries =
		[.. parameter.TableModel.ColumnBoundaries];
	}

	public void CopyTo(TableShapeParameter parameter)
	{
		parameter.Width?.CopyFrom(Width);
		parameter.Height?.CopyFrom(Height);
		parameter.RowCount?.CopyFrom(RowCount);
		parameter.ColumnCount?.CopyFrom(ColumnCount);
		parameter.BorderWidth?.CopyFrom(BorderWidth);
		parameter.OuterBorderWidth?.CopyFrom(
			OuterBorderWidth
		);
		parameter.BorderColor = BorderColor;
		parameter.OuterBorderColor = OuterBorderColor;
		parameter.BackgroundColor = BackgroundColor;

		parameter.HeaderDisplay = HeaderDisplay;
		parameter.HeaderRowBackgroundColor =
			HeaderRowBackgroundColor;
		parameter.HeaderColumnBackgroundColor =
			HeaderColumnBackgroundColor;

		parameter.TableModel = TableModel;
		// TableModelへ区切り情報をセット
		parameter.TableModel.RowBoundaries =
		[
			.. RowBoundaries,
		];
		parameter.TableModel.ColumnBoundaries =
		[
			.. ColumnBoundaries,
		];

	}
}
