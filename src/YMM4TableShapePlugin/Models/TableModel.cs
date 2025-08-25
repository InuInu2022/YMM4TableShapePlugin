using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Epoxy;

using YukkuriMovieMaker.Commons;

namespace YMM4TableShapePlugin.Models;

public sealed class TableModel
	: Animatable,
		IEquatable<TableModel>
{
	[Display(AutoGenerateField = true)]
	public ImmutableList<Animation> RowBoundaries
	{
		get => _rowBoundaries;
		set => Set(ref _rowBoundaries, value);
	}
	ImmutableList<Animation> _rowBoundaries =
		[
			new(0f, BoundariesMin, BoundariesMax),
			new(500f, BoundariesMin, BoundariesMax),
		];

	[Display(AutoGenerateField = true)]
	public ImmutableList<Animation> ColumnBoundaries
	{
		get => _columnBoundaries;
		set => Set(ref _columnBoundaries, value);
	}
	ImmutableList<Animation> _columnBoundaries =
		[
			new(0f, BoundariesMin, BoundariesMax),
			new(300f, BoundariesMin, BoundariesMax),
		];


	[Display(AutoGenerateField = true)]
	public ObservableCollection<
		ObservableCollection<TableCell>
	> Cells
	{
		get => _cells;
		set
		{
			Set(ref _cells, value);
		}
	}
	ObservableCollection<
		ObservableCollection<TableCell>
	> _cells = []; // ← デフォルト行を削除し空リストにする

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

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"VSTHRD001:Avoid legacy thread switching APIs",
		Justification = "<保留中>"
	)]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"VSTHRD002:Avoid problematic synchronous waits",
		Justification = "<保留中>"
	)]
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Correctness",
		"SS034:Use await to get the result of an asynchronous operation",
		Justification = "<保留中>"
	)]
	public void Resize(int rows, int cols)
	{
		if (rows == Rows && cols == Cols)
		{
			// サイズが変更されていない場合は何もしない
			return;
		}

		var cellList =
			new ObservableCollection<
				ObservableCollection<TableCell>
			>(
			//rows
		);
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
			cellList.Add([.. rowList]);
		}
		BeginEdit();

		if (IsCurrentThreadUI())
		{
			Cells = [.. cellList];
		}
		else
		{
			UIThread
				.InvokeAsync(() =>
				{
					Cells = [.. cellList];
					return default;
				})
				.AsTask()
				.Wait();
		}

		ResetBoundaries(rows, cols);
		EndEditAsync().AsTask().Wait();
	}

	[SuppressMessage(
		"Roslynator",
		"RCS1179:Unnecessary assignment",
		Justification = "<保留中>"
	)]
	[SuppressMessage(
		"Usage",
		"VSTHRD002:Avoid problematic synchronous waits",
		Justification = "<保留中>"
	)]
	[SuppressMessage(
		"Correctness",
		"SS034:Use await to get the result of an asynchronous operation",
		Justification = "<保留中>"
	)]
	private static bool IsCurrentThreadUI()
	{
		var isUIThreadTask = UIThread.IsBoundAsync();
		bool isUIThread;
		if (isUIThreadTask.IsCompletedSuccessfully)
		{
			isUIThread = isUIThreadTask.Result;
		}
		else
		{
			isUIThread = isUIThreadTask
				.AsTask()
				.GetAwaiter()
				.GetResult();
		}

		return isUIThread;
	}

	// ResetBoundaries は、行・列数変更時に各境界値を初期化します。
	private void ResetBoundaries(int rows, int cols)
	{
		_ = RowBoundaries.Clear();
		_ = ColumnBoundaries.Clear();

		for (int r = 0; r <= rows; r++)
		{
			_ = RowBoundaries.Add(
				new(
					r * DefaultRowHeight,
					BoundariesMin,
					BoundariesMax
				)
			);
		}

		for (int c = 0; c <= cols; c++)
		{
			_ = ColumnBoundaries.Add(
				new(
					c * DefaultColWidth,
					BoundariesMin,
					BoundariesMax
				)
			);
		}
	}

	protected override IEnumerable<IAnimatable> GetAnimatables()
	{
		Debug.WriteLine(
			$"GetAnimatables[{nameof(TableModel)}]: {RowBoundaries.Count} rows, {ColumnBoundaries.Count} columns"
		);
		return
		[
			.. RowBoundaries,
			.. ColumnBoundaries,
			.. Cells.SelectMany(c => c),
		];
	}

	[SuppressMessage(
		"Usage",
		"SMA0028:Invalid Enum-like Pattern",
		Justification = "<保留中>"
	)]
	public bool Equals(TableModel? other)
	{
		if (other is null)
		{
			return false;
		}

		return Cols == other.Cols
			&& Rows == other.Rows
			&& Cells.SequenceEqual(other.Cells)
			&& RowBoundaries.SequenceEqual(
				other.RowBoundaries
			)
			&& ColumnBoundaries.SequenceEqual(
				other.ColumnBoundaries
			);
	}

	[SuppressMessage(
		"Usage",
		"SMA0028:Invalid Enum-like Pattern",
		Justification = "<保留中>"
	)]
	public override bool Equals(object? obj)
	{
		return Equals(obj as TableModel);
	}

	[SuppressMessage(
		"Correctness",
		"SS008:GetHashCode() refers to mutable or static member",
		Justification = "<保留中>"
	)]
	public override int GetHashCode()
	{
		return HashCode.Combine(
			Cols,
			Rows,
			Cells,
			RowBoundaries,
			ColumnBoundaries
		);
	}
}
