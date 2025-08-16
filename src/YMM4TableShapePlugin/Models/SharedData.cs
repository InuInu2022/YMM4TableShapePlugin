using System.Collections.Immutable;
using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin.Models;

internal class SharedData(TableShapeParameter parameter)
{
	// TableModelから区切り情報を取得
	public ImmutableList<Animation> RowBoundaries { get; } =
		[.. parameter.TableModel.RowBoundaries];
	public ImmutableList<Animation> ColumnBoundaries { get; } =
		[.. parameter.TableModel.ColumnBoundaries];

	// TableModelへ区切り情報をセット
	public void CopyTo(TableShapeParameter parameter)
	{
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
