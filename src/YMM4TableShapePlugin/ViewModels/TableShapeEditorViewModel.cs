using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
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
	public ObservableCollection<TableCell>? SelectedRow { get; set; }
	public TableCell? SelectedCell { get; set; }
	public Well<TextEditor> TextEditorWell { get; set; } =
		Well.Factory.Create<TextEditor>();
	public DataTable CellTable { get; private set; } =
		new();

	public double EditorHeight { get; private set; } = 100;
	public event EventHandler? BeginEdit;
	public event EventHandler? EndEdit;

	readonly ItemProperty[] _properties;
	readonly INotifyPropertyChanged? _item;
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
			is INotifyPropertyChanged item
		)
		{
			_item = item;
			_item.PropertyChanged += Item_PropertyChanged;
		}

		InitializeEventHandlers();
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
		if (
			string.Equals(
				e.PropertyName,
				_properties[0].PropertyInfo.Name,
				StringComparison.Ordinal
			)
		)
		{
			//UpdateCells();
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
		// 空行を除外してデータ自体も更新
		var filtered = values
			.Where(row => row.Count > 0)
			.ToList();
		if (filtered.Count != values.Count)
		{
			// データモデルからも空行を除去
			param.TableModel.Cells.Clear();
			foreach (
				ref var row in CollectionsMarshal.AsSpan(
					filtered
				)
			)
			{
				param.TableModel.Cells.Add(row);
			}
		}
		if (!filtered.SequenceEqual(Cells))
		{
			Cells = new ObservableCollection<
				ObservableCollection<TableCell>
			>(filtered);
		}
		EndEdit?.Invoke(this, EventArgs.Empty);
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
				if (_item is not null)
				{
					_item.PropertyChanged -=
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
