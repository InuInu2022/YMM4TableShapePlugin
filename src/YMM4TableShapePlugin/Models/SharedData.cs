using System.Collections.Immutable;
using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin.Models;

internal class SharedData(TableShapeParameter parameter)
{
	TableModel TableModel { get; } = parameter.TableModel;

	// TableModelから区切り情報を取得
	public ImmutableList<Animation> RowBoundaries { get; } =
		[.. parameter.TableModel.RowBoundaries];
	public ImmutableList<Animation> ColumnBoundaries { get; } =
		[.. parameter.TableModel.ColumnBoundaries];

	public void CopyTo(TableShapeParameter parameter)
	{
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
