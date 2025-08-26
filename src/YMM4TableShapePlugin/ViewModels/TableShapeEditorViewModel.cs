using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Epoxy;
using YMM4TableShapePlugin.Models;
using YMM4TableShapePlugin.View;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;

namespace YMM4TableShapePlugin.ViewModels;

[ViewModel]
public class TableShapeEditorViewModel : IDisposable
{
	public Well<TableShapeEditor> MainControlWell { get; } =
		Well.Factory.Create<TableShapeEditor>();
	public Well<Thumb> BottomThumbWell { get; } =
		Well.Factory.Create<Thumb>();

	public Well<Grid> MainGridWell {get;} =
		Well.Factory.Create<Grid>();

	public ObservableCollection<
		ObservableCollection<TableCell>
	> Cells
	{ get; private set; } = []; // 初期値を空コレクションに修正
	public ObservableCollection<
		ObservableCollection<TableCell>
	> FilteredCells { get; private set; } = [];

	public ObservableCollection<TableCell>? SelectedRow { get; set; }
	public TableCell? SelectedCell { get; set; }
	public Well<TextEditor> TextEditorWell { get; set; } =
		Well.Factory.Create<TextEditor>();
	public DataTable CellTable { get; private set; } =
		new();

	public double EditorHeight { get; private set; } = 100;

	public IEditorInfo? EditorInfo { get; set; }

	public event EventHandler? BeginEdit;
	public event EventHandler? EndEdit;

	readonly ItemProperty[] _properties;
	readonly TableShapeParameter? _parameter;
	const double ThumbHeight = 5;
	private bool _disposedValue;
	private double dragStartHeight;
	private double dragDeltaSum;
	private double dragDefaultHeight;

	public TableShapeEditorViewModel(
		ItemProperty[] properties
	)
	{
		this._properties = properties;

		//var shapeItem = properties[0].Item;

		if (
			properties[0].PropertyOwner
			is TableShapeParameter parameter
		)
		{
			_parameter = parameter;
			_parameter.RowCount.PropertyChanged += Item_PropertyChanged;
			_parameter.ColumnCount.PropertyChanged +=
				Item_PropertyChanged;

			Debug.WriteLine("_parameter.GetHashCode():" + _parameter.GetHashCode());

			foreach (var v in _parameter.RowCount.Values.OfType<INotifyPropertyChanged>())
			{
				v.PropertyChanged +=
					OnRowOrColumnValuePropertyChanged;
			}
			foreach (
				var v in _parameter.ColumnCount.Values.OfType<INotifyPropertyChanged>()
			)
			{
				v.PropertyChanged +=
					OnRowOrColumnValuePropertyChanged;
			}
		}

		InitializeEventHandlers();
	}

	void OnRowOrColumnValuePropertyChanged(
		object? sender,
		PropertyChangedEventArgs e
	)
	{
		Debug.WriteLine($"OnAnimationValuePropertyChanged: {e.PropertyName}");
		if (string.Equals(e.PropertyName, nameof(AnimationValue.Value), StringComparison.Ordinal))
		{
			// ここで「数値が編集された」ことを検知できる
			UpdateCells();
		}
	}

	void InitializeEventHandlers()
	{
		MainControlWell.Add(
			"Loaded",
			() =>
			{
				UpdateCells();

				return default;
			}
		);

		MainGridWell.Add<SizeChangedEventArgs>(
			"SizeChanged",
			e =>
			{
				if (e.Source is Grid grid
					&& grid.ActualHeight > 0)
				{
					EditorHeight = grid.ActualHeight + ThumbHeight;
				}
				return default;
			}
		);

		TextEditorWell.Add<MouseButtonEventArgs>(
			"PreviewMouseDown",
			(e) =>
			{
				if (
					e.Source is TextBox textBox
					&& textBox.DataContext is TableCell cell
				)
				{
					SelectedCell = cell;
				}

				return default;
			}
		);

		BottomThumbWell.Add<DragStartedEventArgs>(
			"DragStarted",
			e =>
			{
				if (TryGetGrid(e, out var grid))
				{
					if (dragStartHeight == 0)
					{
						dragDefaultHeight =
							grid.ActualHeight;
					}
					dragStartHeight = grid.ActualHeight;
					dragDeltaSum = 0;
				}
				return default;
			}
		);

		BottomThumbWell.Add<DragDeltaEventArgs>(
			"DragDelta",
			e =>
			{
				if (TryGetGrid(e, out var grid))
				{
					dragDeltaSum += e.VerticalChange;
					grid.Height = Math.Max(
						dragStartHeight + dragDeltaSum,
						dragDefaultHeight
					);
				}
				return default;
			}
		);

		static bool TryGetGrid<T>(
			T e,
			[MaybeNullWhen(false)] out Grid grid
		)
			where T : RoutedEventArgs
		{
			if (
				e.Source is Thumb thumb
				&& thumb.Parent is DockPanel dock
				&& dock
					.Children.OfType<Grid>()
					.FirstOrDefault()
					is Grid pGrid
			)
			{
				grid = pGrid;
				return true;
			}
			grid = null;
			return false;
		}
	}

	void Item_PropertyChanged(
		object? sender,
		PropertyChangedEventArgs e
	)
	{
		//呼ばれない？
		//Animationプロパティは差し替え時にしかよばれない

		Debug.WriteLine(
			$"Item_PropertyChanged: {e.PropertyName}"
		);
		if (
			string.Equals(
				e.PropertyName,
				_properties[0].PropertyInfo.Name,
				StringComparison.Ordinal
			)
		)
		{
			//UpdateCells();
			Debug.WriteLine(
				$"\tUpdateCells();: {e.PropertyName}"
			);
		}

		// RowCount/ColumnCount変更時にフィルタ更新
		if (
			e.PropertyName == nameof(TableShapeParameter.RowCount)
			|| e.PropertyName == nameof(TableShapeParameter.ColumnCount)
		)
		{
			UpdateCells();
			Debug.WriteLine(
				$"\tUpdateFilteredCells(): {e.PropertyName}"
			);
		}
	}

	void UpdateCells()
	{
		if (
			_properties[0].PropertyOwner
			is not TableShapeParameter param
		)
		{
			return;
		}
		if (
			param.TableModel is null
			|| param.TableModel.Cells is null
		)
		{
			return;
		}
		var values = param.TableModel.Cells;
		if (values is null || Cells is null)
		{
			return;
		}

		BeginEdit?.Invoke(this, EventArgs.Empty);

		// Row/Colを超えたCellデータは消す必要はないが、
		// 存在しなければ追加する必要がある

		(int maxRow, int maxCol) = GetMaxRowCol(param);

		if (Cells.Count < maxRow)
		{
			// RowCountが変更された場合、行数を更新する
			for (int i = 0; i < maxRow; i++)
			{
				if (i >= Cells.Count)
				{
					Cells.Add([]);	//とりあえず空行追加
				}
			}
		}

		// ColumnCountが変更された場合、列数を更新する
		for (var j = 0; j < Cells.Count; j++)
		{
			var row = Cells[j];
			for (int i = 0; i < maxCol; i++)
			{
				if (i >= row.Count)
				{
					//足りない列分のセルを追加
					row.Add(new TableCell()
					{
						Row = j + 1,
						Col = i + 1,
						Text = string.Empty,
					});
				}
			}
		}

		//Cellsの更新
		if (!Cells.SequenceEqual(param.TableModel.Cells))
		{
			Cells.Clear();

			foreach (var row in param.TableModel.Cells)
			{
				Cells.Add(
					new ObservableCollection<TableCell>(row)
				);
			}
		}

		UpdateFilteredCells();
		EndEdit?.Invoke(this, EventArgs.Empty);
	}

	void UpdateFilteredCells()
	{
		if (
			_properties[0].PropertyOwner
			is not TableShapeParameter param
		)
		{
			return;
		}

		(int maxRow, int maxCol) = GetMaxRowCol(param);

		FilteredCells.Clear();
		for (int i = 0; i < maxRow; i++)
		{
			var row = Cells[i];
			var filteredRow =
				new ObservableCollection<TableCell>(
					row.Take(maxCol)
				);
			if (filteredRow.Count > 0)
				FilteredCells.Add(filteredRow);
			Debug.WriteLine("filtered:" + FilteredCells.Count);
		}
	}

	(int maxRow, int maxCol) GetMaxRowCol(
		TableShapeParameter param
	)
	{
		var frame = EditorInfo?.ItemPosition?.Frame ?? 0;
		var total = EditorInfo?.ItemDuration?.Frame ?? 0;
		var fps = EditorInfo?.VideoInfo?.FPS ?? 60;

		var maxRow = (int)
			param.RowCount.GetValue(frame, total, fps);
		var maxCol = (int)
			param.ColumnCount.GetValue(frame, total, fps);
		return (maxRow, maxCol);
	}

	[PropertyChanged(nameof(SelectedCell))]
	[SuppressMessage("", "IDE0051")]
	private ValueTask SelectedCellChangedAsync(
		TableCell? value
	)
	{
		if (value is null)
		{
			return default;
		}

		return default;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				if (_parameter is not null)
				{
					_parameter.ColumnCount.PropertyChanged -=
						Item_PropertyChanged;
					_parameter.RowCount.PropertyChanged -=
						Item_PropertyChanged;
				}

				CellTable?.Clear();
				CellTable?.Dispose();
			}

			// アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
			// 大きなフィールドを null に設定します
			_disposedValue = true;
		}
	}

	// // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
	// ~TableShapeEditorViewModel()
	// {
	//     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
