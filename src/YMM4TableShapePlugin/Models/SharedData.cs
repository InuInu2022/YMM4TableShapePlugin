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

	//セルの中身
	//public ImmutableList<
	//	ImmutableList<TableCell>
	//> Cells { get; } = [.. parameter.TableModel.Cells];



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
		//セルの中身をセット
		//parameter.TableModel.Cells = [.. Cells];
	}
}
