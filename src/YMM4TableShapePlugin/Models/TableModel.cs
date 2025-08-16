using System.ComponentModel.DataAnnotations;

using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin.Models;

public sealed record TableModel
{
	[Display(AutoGenerateField = true)]
	public IList<Animation> RowBoundaries { get; set; } =
		[
			new(0f, BoundariesMin, BoundariesMax),
			new(500f, BoundariesMin, BoundariesMax),
		];
	[Display(AutoGenerateField = true)]
	public IList<Animation> ColumnBoundaries { get; set; } =
		[
			new(0f, BoundariesMin, BoundariesMax),
			new(300f, BoundariesMin, BoundariesMax),
		];

	[Display(AutoGenerateField = true)]
	public IList<IList<TableCell>> Cells
	{
		get;
		private set;
	} =
		[
			[
				new TableCell
				{
					Row = 1,
					Col = 1,
					Text = "text",
				},
			],
		];

	public int Rows => Cells.Count;
	public int Cols => Cells.FirstOrDefault()?.Count ?? 0;

	/// <summary>
	/// デフォルトの行の高さ（ピクセル）
	/// </summary>
	const int DefaultRowHeight = 50;

	/// <summary>
	/// デフォルトの列の幅（ピクセル）
	/// </summary>
	const int DefaultColWidth = 80;

	const double BoundariesMin = 0f;
	const double BoundariesMax = 100000f;

	public TableModel(int rows, int cols)
	{
		Resize(rows, cols);
	}

	public void Resize(int rows, int cols)
	{
		var cellList = new List<IList<TableCell>>(rows);
		for (int r = 0; r < rows; r++)
		{
			var rowList = new List<TableCell>(cols);
			for (int c = 0; c < cols; c++)
			{
				TableCell cell =
					(r < Cells.Count && c < Cells[r].Count)
						? Cells[r][c]
						: new TableCell
						{
							Row = r,
							Col = c,
						};
				rowList.Add(cell);
			}
			cellList.Add(rowList);
		}
		Cells = cellList;

		ResetBoundaries(rows, cols);
	}

	// ResetBoundaries は、行・列数変更時に各境界値を初期化します。
	private void ResetBoundaries(int rows, int cols)
	{
		RowBoundaries.Clear();
		ColumnBoundaries.Clear();

		for (int r = 0; r <= rows; r++)
		{
			RowBoundaries.Add(
				new(
					r * DefaultRowHeight,
					BoundariesMin,
					BoundariesMax
				)
			);
		}

		for (int c = 0; c <= cols; c++)
		{
			ColumnBoundaries.Add(
				new(
					c * DefaultColWidth,
					BoundariesMin,
					BoundariesMax
				)
			);
		}
	}
}
