using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

	public Well<Grid> MainGridWell { get; } =
		Well.Factory.Create<Grid>();

	public Pile<ItemsControl> TablePile { get; } =
		Pile.Factory.Create<ItemsControl>();

	public ObservableCollection<
		ObservableCollection<TableCell>
	> Cells { get; private set; } = []; // 初期値を空コレクションに修正
	public ObservableCollection<
		ObservableCollection<TableCell>
	> FilteredCells { get; private set; } = [];

	public ObservableCollection<TableCell>? SelectedRow { get; set; }
	public TableCell? SelectedCell { get; set; }
	public Well<TextEditor> TextEditorWell { get; set; } =
		Well.Factory.Create<TextEditor>();

	public Command UpdateCommand { get; private set; }

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

		if (
			properties[0].PropertyOwner
			is TableShapeParameter parameter
		)
		{
			_parameter = parameter;

			Debug.WriteLine(
				"_parameter.GetHashCode():"
					+ _parameter.GetHashCode()
			);

			foreach (
				var v in _parameter.RowCount.Values.OfType<INotifyPropertyChanged>()
			)
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

		// TableModelの変更を監視
		if (_parameter?.TableModel is not null)
		{
			_parameter.TableModel.PropertyChanged +=
				OnTableModelPropertyChanged;
			SubscribeToCellsChanges(
				_parameter.TableModel.Cells
			);
		}

		InitializeEventHandlers();
	}

	private void OnTableModelPropertyChanged(
		object? sender,
		PropertyChangedEventArgs e
	)
	{
		if (
			string.Equals(
				e.PropertyName,
				nameof(TableModel.Cells),
				StringComparison.Ordinal
			)
		)
		{
			// Cellsが差し替わった場合、再購読
			UnsubscribeFromCellsChanges();
			if (_parameter?.TableModel?.Cells is not null)
			{
				SubscribeToCellsChanges(
					_parameter.TableModel.Cells
				);
				_ = UpdateCellsSafetyAsync();
			}
		}
	}

	private void SubscribeToCellsChanges(
		ObservableCollection<
			ObservableCollection<TableCell>
		> cells
	)
	{
		cells.CollectionChanged += OnCellsCollectionChanged;
		foreach (var row in cells)
		{
			row.CollectionChanged += OnRowCollectionChanged;
			foreach (var cell in row)
			{
				cell.PropertyChanged +=
					OnCellPropertyChanged;
				cell.PropertyChanging +=
					OnCellPropertyChanging;
			}
		}
	}

	private void UnsubscribeFromCellsChanges()
	{
		if (_parameter?.TableModel?.Cells is null)
		{
			return;
		}
		_parameter.TableModel.Cells.CollectionChanged -=
			OnCellsCollectionChanged;
		foreach (var row in _parameter.TableModel.Cells)
		{
			row.CollectionChanged -= OnRowCollectionChanged;
			foreach (var cell in row)
			{
				cell.PropertyChanged -=
					OnCellPropertyChanged;
				cell.PropertyChanging -=
					OnCellPropertyChanging;
			}
		}
	}

	private void OnCellsCollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs e
	)
	{
		// 行の追加/削除時に購読を更新
		if (e.NewItems is not null)
		{
			foreach (
				ObservableCollection<TableCell> newRow in e.NewItems
			)
			{
				newRow.CollectionChanged +=
					OnRowCollectionChanged;
				foreach (var cell in newRow)
				{
					cell.PropertyChanged +=
						OnCellPropertyChanged;
					cell.PropertyChanging +=
						OnCellPropertyChanging;
				}
			}
		}
		if (e.OldItems is not null)
		{
			foreach (
				ObservableCollection<TableCell> oldRow in e.OldItems
			)
			{
				oldRow.CollectionChanged -=
					OnRowCollectionChanged;
				foreach (var cell in oldRow)
				{
					cell.PropertyChanged -=
						OnCellPropertyChanged;
					cell.PropertyChanging -=
						OnCellPropertyChanging;
				}
			}
		}
		_ = UpdateCellsSafetyAsync();
	}

	/// <summary>
	/// voidイベントハンドラ内でセルの更新を安全に行います。
	/// awaitで処理を待たないでください。
	/// 処理を待つ場合は `ContinueWith`で続けてください。
	/// </summary>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	async Task UpdateCellsSafetyAsync()
	{
		try
		{
			await UpdateCellsFromModelAsync()
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error occurred while updating cells: {ex.Message}");
			Log.Default.Write($"[{nameof(TableShapePlugin)}] {ex.Message}", ex);
		}
	}

	private void OnRowCollectionChanged(
		object? sender,
		NotifyCollectionChangedEventArgs e
	)
	{
		// 列の追加/削除時に購読を更新
		if (e.NewItems is not null)
		{
			foreach (TableCell newCell in e.NewItems)
			{
				newCell.PropertyChanged +=
					OnCellPropertyChanged;
				newCell.PropertyChanging +=
					OnCellPropertyChanging;
			}
		}
		if (e.OldItems is not null)
		{
			foreach (TableCell oldCell in e.OldItems)
			{
				oldCell.PropertyChanged -=
					OnCellPropertyChanged;
				oldCell.PropertyChanging -=
					OnCellPropertyChanging;
			}
		}
		_ = UpdateCellsSafetyAsync();
	}

	/// <summary>
	/// セルのプロパティが変更されたときに呼び出されます。
	/// </summary>
	void OnCellPropertyChanged(
		object? sender,
		PropertyChangedEventArgs e
	)
	{
		// セルのプロパティ変更時にUIを更新
		if (
			string.Equals(
				e.PropertyName,
				nameof(TableCell.Row),
				StringComparison.Ordinal
			)
			|| string.Equals(
				e.PropertyName,
				nameof(TableCell.Col),
				StringComparison.Ordinal
			)
		)
		{
			_ = UpdateCellsSafetyAsync();
		}
	}

	/// <summary>
	/// セルのプロパティが変更される前に呼び出されます。
	/// </summary>
	void OnCellPropertyChanging(
		object? sender,
		PropertyChangingEventArgs e
	)
	{
		// セルのプロパティ変更時にUIを随時更新
		switch (e.PropertyName)
		{
			case nameof(TableCell.Text):
			case nameof(TableCell.TextStyle):
			case nameof(TableCell.FontSize):
			case nameof(TableCell.Font):
			case nameof(TableCell.FontColor):
			case nameof(TableCell.FontOutlineColor):
			case nameof(TableCell.VideoEffect):
				ForceRefresh();
				break;
			default:
				break;
		}
	}

	async ValueTask UpdateCellsFromModelAsync()
	{
		if (_parameter?.TableModel?.Cells is null)
			return;

		// CellsをTableModel.Cellsに同期
		Cells.Clear();
		foreach (var row in _parameter.TableModel.Cells)
		{
			var newRow =
				new ObservableCollection<TableCell>();
			foreach (var cell in row)
			{
				newRow.Add(cell); // 参照コピー（変更は即時反映）
			}
			Cells.Add(newRow);
		}

		// FilteredCellsも更新
		await UpdateFilteredCellsAsync()
			.ConfigureAwait(true);
	}

	[SuppressMessage("Usage", "VSTHRD100")]
	async void OnRowOrColumnValuePropertyChanged(
		object? sender,
		PropertyChangedEventArgs e
	)
	{
		// ここで「数値が編集された」ことを検知できる
		try
		{
			if (IsAnimationValue(e))
			{
				await UpdateCellsAsync(e)
					.ConfigureAwait(true);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine(
				$"Error occurred while updating cells: {ex.Message}"
			);
			Log.Default.Write(
				$"[{nameof(TableShapePlugin)}] {ex.Message}",
				ex
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool IsAnimationValue(
			PropertyChangedEventArgs e
		)
		{
			return string.Equals(
				e.PropertyName,
				nameof(AnimationValue.Value),
				StringComparison.Ordinal
			);
		}
	}

	[MemberNotNull(nameof(UpdateCommand))]
	void InitializeEventHandlers()
	{
		MainControlWell.Add<RoutedEventArgs>(
			"Loaded",
			async (e) =>
			{
				await UpdateCellsAsync(e)
					.ConfigureAwait(true);
			}
		);

		MainGridWell.Add<SizeChangedEventArgs>(
			"SizeChanged",
			e =>
			{
				if (
					e.Source is Grid grid
					&& grid.ActualHeight > 0
				)
				{
					EditorHeight =
						grid.ActualHeight + ThumbHeight;
				}
				return default;
			}
		);

		InitializeTextEditorEvents();

		InitializeThumbDragEvents();

		EndEdit += (s, e) =>
		{
			ForceRefresh();
		};

		UpdateCommand = Command.Factory.Create(() =>
		{
			ForceRefresh();
			return default;
		});
	}

	void InitializeTextEditorEvents()
	{
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

		TextEditorWell.Add<EventArgs>(
			"BeginEdit",
			(e) =>
			{
				//親コントロールのBeginEdit・EndEditを呼ぶ必要がある
				BeginEdit?.Invoke(this, e);
				return default;
			}
		);

		TextEditorWell.Add<EventArgs>(
			"EndEdit",
			(e) =>
			{
				//親コントロールのBeginEdit・EndEditを呼ぶ必要がある
				EndEdit?.Invoke(this, e);
				return default;
			}
		);
	}

	public void ForceRefresh()
	{
		//即時描画反映させるためのダミー処理
		if (_parameter is not null)
		{
			_parameter.IsDummy = !_parameter.IsDummy;
		}
	}

	/// <summary>
	/// Thumbのドラッグ操作のイベントハンドラを設定します。
	/// </summary>
	void InitializeThumbDragEvents()
	{
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

	async ValueTask UpdateCellsAsync<T>(T e)
		where T : EventArgs
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

		BeginEdit?.Invoke(this, e);

		// Row/Colを超えたCellデータは消す必要はないが、
		// 存在しなければ追加する必要がある

		(int maxRow, int maxCol) = GetMaxRowCol(param);
		Debug.WriteLine(
			$"UpdateCells(): r:{maxRow}, c:{maxCol}"
		);

		if (Cells.Count < maxRow)
		{
			// RowCountが変更された場合、行数を更新する
			for (int i = Cells.Count; i < maxRow; i++)
			{
				var newRow =
					new ObservableCollection<TableCell>();
				for (int col = 0; col < maxCol; col++)
				{
					newRow.Add(
						new TableCell
						{
							Row = i + 1,
							Col = col + 1,
							Text = string.Empty,
						}
					);
				}
				Cells.Add(newRow);
			}
		}

		// ColumnCountが変更された場合、列数を更新する
		for (var j = 0; j < Cells.Count; j++)
		{
			var row = Cells[j];
			for (int i = row.Count; i < maxCol; i++)
			{
				if (i >= row.Count)
				{
					//足りない列分のセルを追加
					row.Add(
						new TableCell()
						{
							Row = j + 1,
							Col = i + 1,
							Text = string.Empty,
						}
					);
				}
			}
			Debug.WriteLine($"new row[{j}]: {row.Count}");
		}

		//Cellsの更新
		if (!Cells.SequenceEqual(param.TableModel.Cells))
		{
			Cells.Clear();

			foreach (var row in param.TableModel.Cells)
			{
				var newRow =
					new ObservableCollection<TableCell>(
						row
					);

				// 列数が足りない場合は不足分を追加
				for (int i = newRow.Count; i < maxCol; i++)
				{
					newRow.Add(
						new TableCell
						{
							Row = Cells.Count + 1,
							Col = i + 1,
							Text = string.Empty,
						}
					);
				}

				Cells.Add(newRow);
			}
		}

		await UpdateFilteredCellsAsync()
			.ConfigureAwait(true);
		EndEdit?.Invoke(this, e);
	}

	async ValueTask UpdateFilteredCellsAsync()
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
			Debug.WriteLine(
				"filtered:" + FilteredCells.Count
			);
		}

		//Refresh()呼ばないと更新されないことがある
		await TablePile
			.RentAsync(table =>
			{
				foreach (var item in table.Items)
				{
					if (item is ItemsControl itemControl)
					{
						// itemの更新処理
						itemControl.Items.Refresh();
					}
				}
				table.Items.Refresh();
				return default;
			})
			.ConfigureAwait(true);
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

		ForceRefresh();

		return default;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				UnsubscribeFromCellsChanges();
				if (_parameter?.TableModel is not null)
				{
					_parameter.TableModel.PropertyChanged -=
						OnTableModelPropertyChanged;
				}
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
