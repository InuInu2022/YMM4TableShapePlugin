using System.Collections.Immutable;
using System.Windows.Documents;
using System.Windows.Media;

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using System.Numerics;

namespace YMM4TableShapePlugin;

internal class TableShapeSource : IShapeSource2
{
	public IEnumerable<VideoController> Controllers
	{
		get;
		private set;
	} = [];

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"SMA0040:Missing Using Statement",
		Justification = "<保留中>"
	)]
	public ID2D1Image Output =>
		commandList
		?? throw new InvalidOperationException(
			$"{nameof(commandList)}がnullです。事前にUpdateを呼び出す必要があります。"
		);

	readonly TableShapeParameter Parameter;
	readonly IGraphicsDevicesAndContext Devices;
	readonly DisposeCollector disposer = new();

	bool isFirst = true;
	ImmutableList<Animation> _rowBoundaries = [];
	ImmutableList<Animation> _columnBoundaries = [];
	TableModel? _tableModel;
	double? _borderWidth;
	bool _disposedValue;
	ID2D1CommandList? commandList;
	private int _row;
	private int _col;
	private double _width;
	private double _height;
	private Color _borderColor;
	private Color _backgroundColor;
	private Color _outerBorderColor;
	private double _outerBorderWidth;

	public TableShapeSource(
		IGraphicsDevicesAndContext devices,
		TableShapeParameter parameter
	)
	{
		Devices = devices;
		Parameter = parameter;
	}

	/// <summary>
	/// 表の描画を行う。TableModelのセル・境界線・テキストを描画する。
	/// </summary>
	/// <param name="timelineItemSourceDescription">タイムライン情報</param>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Major Code Smell",
		"S2589:Boolean expressions should not be gratuitous",
		Justification = "<保留中>"
	)]
	public void Update(
		TimelineItemSourceDescription timelineItemSourceDescription
	)
	{
		var frame = timelineItemSourceDescription
			.ItemPosition
			.Frame;
		var length = timelineItemSourceDescription
			.ItemDuration
			.Frame;
		var fps = timelineItemSourceDescription.FPS;

		var debugOuterBorderWidth =
			Parameter.OuterBorderWidth.GetValue(
				frame,
				length,
				fps
			);
		System.Diagnostics.Debug.WriteLine(
			$"Update called: OuterBorderWidth={debugOuterBorderWidth}, _outerBorderWidth={_outerBorderWidth}"
		);

		var model =
			Parameter.TableModel
			?? throw new InvalidOperationException(
				"TableModel is null"
			);

		// Width/Heightパラメータを境界値に反映
		UpdateBoundaryValues(
			frame,
			length,
			fps,
			model,
			out double width,
			out double height
		);

		var rBoundaries = model.RowBoundaries;
		var cBoundaries = model.ColumnBoundaries;

		var borderWidth = Parameter.BorderWidth.GetValue(
			frame,
			length,
			fps
		);
		var outerBorderWidth =
			Parameter.OuterBorderWidth.GetValue(
				frame,
				length,
				fps
			);

		var row = (int)
			Parameter.RowCount.GetValue(frame, length, fps);
		var col = (int)
			Parameter.ColumnCount.GetValue(
				frame,
				length,
				fps
			);
		model.Resize(row, col);

		var bgColor = Parameter.BackgroundColor;
		var borderColor = Parameter.BorderColor;
		var outerBorderColor = Parameter.OuterBorderColor;

		var textLists = Parameter.Cells.Select(c => c.Text);
		var cellLists = Parameter.Cells;
		//変更がない場合は戻る
		if (
			commandList is not null
			&& !isFirst
			&& _tableModel == model
			&& _rowBoundaries == rBoundaries
			&& _columnBoundaries == cBoundaries
			&& _rowBoundaries.SequenceEqual(rBoundaries)
			&& _columnBoundaries.SequenceEqual(cBoundaries)
			&& _borderWidth == borderWidth
			&& _row == row
			&& _col == col
			&& _width == width
			&& _height == height
			&& _borderColor == borderColor
			&& _backgroundColor == bgColor
			&& _outerBorderWidth == outerBorderWidth
			&& _outerBorderColor == outerBorderColor
			&& _textLists.SequenceEqual(
				textLists,
				StringComparer.Ordinal
			)
			&& _cellLists.SequenceEqual(cellLists)
		)
		{
			System.Diagnostics.Debug.WriteLine(
				"Update: cache hit, skip redraw"
			);
			return;
		}

		System.Diagnostics.Debug.WriteLine(
			"Update: cache miss, redraw"
		);
		var sw = System.Diagnostics.Stopwatch.StartNew();
		// セルを描画する
		DrawTableCells(
			new TableRenderContext(
				frame,
				length,
				fps,
				model,
				borderWidth,
				outerBorderWidth,
				Devices.DeviceContext,
				null, // CellBackgroundBrushはDrawTableCells内で生成
				null, // TextBrushも同様
				null, // WriteFactoryも同様
				Width: width,
				Height: height,
				RowCount: row,
				ColCount: col,
				(float)(outerBorderWidth * 2 + borderWidth),
				(float)(outerBorderWidth * 2 + borderWidth)
					/ 2f,
				bgColor,
				borderColor,
				outerBorderColor
			)
		);

		sw.Stop();
		System.Diagnostics.Debug.WriteLine(
			$"DrawTableCells took {sw.ElapsedMilliseconds} ms"
		);

		//制御点を作成する
		//UpdateControllerPoints(frame, length, fps);

		//キャッシュ用の情報を保存しておく
		isFirst = false;
		_rowBoundaries = rBoundaries;
		_columnBoundaries = cBoundaries;
		_tableModel = model;
		_borderWidth = borderWidth;
		_row = row;
		_col = col;
		_width = width;
		_height = height;
		_borderColor = borderColor;
		_backgroundColor = bgColor;
		_outerBorderColor = outerBorderColor;
		_outerBorderWidth = outerBorderWidth;
		_textLists = [.. textLists.ToList()];
		_cellLists =
		[
			.. cellLists
				.Select(c => c.Clone() as Models.TableCell)
				.OfType<Models.TableCell>()
				.ToList(),
		];
	}

	void UpdateBoundaryValues(
		int frame,
		int length,
		int fps,
		TableModel model,
		out double vWidth,
		out double vHeight
	)
	{
		vWidth = 0;
		vHeight = 0;
		var cols = Parameter.ColumnCount.GetValue(
			frame,
			length,
			fps
		);
		if (cols > 0)
		{
			vWidth = Parameter.Width.GetValue(
				frame,
				length,
				fps
			);

			var colStep = vWidth / cols;
			for (
				var c = 0;
				c < model.ColumnBoundaries.Count;
				c++
			)
			{
				model.ColumnBoundaries[c].Values[0].Value =
					c * colStep;
			}
		}
		var rows = Parameter.RowCount.GetValue(
			frame,
			length,
			fps
		);
		if (rows > 0)
		{
			vHeight = Parameter.Height.GetValue(
				frame,
				length,
				fps
			);
			var rowStep = vHeight / rows;
			for (
				var r = 0;
				r < model.RowBoundaries.Count;
				r++
			)
			{
				model.RowBoundaries[r].Values[0].Value =
					r * rowStep;
			}
		}
	}

	ID2D1SolidColorBrush? cachedOuterBorderBrush;
	Color? cachedOuterBorderColor;
	float? cachedOuterBorderWidth;
	IEnumerable<string> _textLists = [];
	ImmutableList<Models.TableCell> _cellLists = [];

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"SMA0040:Missing Using Statement",
		Justification = "<保留中>"
	)]
	void DrawTableCells(TableRenderContext context)
	{
		var ctx = Devices.DeviceContext;

		// outerBorderBrushキャッシュ（色・太さ変更時のみ再生成）
		if (
			cachedOuterBorderBrush is null
			|| cachedOuterBorderColor
				!= context.OuterBorderColor
			|| cachedOuterBorderWidth
				!= (float)context.OuterBorderWidth
		)
		{
			disposer.RemoveAndDispose(
				ref cachedOuterBorderBrush
			);
			cachedOuterBorderBrush =
				ctx.CreateSolidColorBrush(
					new(
						context.OuterBorderColor.R,
						context.OuterBorderColor.G,
						context.OuterBorderColor.B,
						context.OuterBorderColor.A
					)
				);
			disposer.Collect(cachedOuterBorderBrush);
			cachedOuterBorderColor =
				context.OuterBorderColor;
			cachedOuterBorderWidth = (float)
				context.OuterBorderWidth;
		}

		if (commandList is not null)
		{
			disposer.RemoveAndDispose(ref commandList);
		}
		commandList = ctx.CreateCommandList();
		disposer.Collect(commandList);

		var cellBgBrush = ctx.CreateSolidColorBrush(
			new(
				context.BackgroundColor.R,
				context.BackgroundColor.G,
				context.BackgroundColor.B,
				context.BackgroundColor.A
			)
		);
		var borderBrush = ctx.CreateSolidColorBrush(
			new(
				context.BorderColor.R,
				context.BorderColor.G,
				context.BorderColor.B,
				context.BorderColor.A
			)
		);
		var outerBorderBrush = cachedOuterBorderBrush;
		var textBrush = ctx.CreateSolidColorBrush(
			new(0f, 0f, 0f, 1f)
		);
		disposer.Collect(textBrush);
		disposer.Collect(cellBgBrush);
		disposer.Collect(borderBrush);
		disposer.Collect(outerBorderBrush);

		using var dwFactory =
			DWrite.DWriteCreateFactory<IDWriteFactory>(
				Vortice.DirectWrite.FactoryType.Shared
			);

		ctx.Target = commandList;
		ctx.BeginDraw();
		ctx.Clear(null);

		// セル背景・テキスト描画
		RenderTableCells(
			context with
			{
				CellBackgroundBrush = cellBgBrush,
				TextBrush = textBrush,
				WriteFactory = dwFactory,
			}
		);

		var cellWidth =
			(float)context.Width / context.ColCount;
		var cellHeight =
			(float)context.Height / context.RowCount;

		if (context.OuterBorderWidth > 0)
		{
			ctx.DrawRectangle(
				new Vortice.Mathematics.Rect(
					context.RealOuterWidth / 2f,
					context.RealOuterWidth / 2f,
					(float)context.Width
						- context.RealOuterWidth / 2f,
					(float)context.Height
						- context.RealOuterWidth / 2f
				),
				outerBorderBrush,
				context.RealOuterWidth
			);

			for (var c = 1; c < context.ColCount; c++)
			{
				var x = c * cellWidth;
				ctx.DrawLine(
					new Vector2(x, 0f),
					new Vector2(x, (float)context.Height),
					outerBorderBrush,
					context.RealOuterWidth
				);
			}
			for (var r = 1; r < context.RowCount; r++)
			{
				var y = r * cellHeight;
				ctx.DrawLine(
					new Vector2(0f, y),
					new Vector2((float)context.Width, y),
					outerBorderBrush,
					context.RealOuterWidth
				);
			}
		}

		if (context.BorderWidth > 0)
		{
			var margin = context.RealOuterWidth / 2f;

			ctx.DrawRectangle(
				new Vortice.Mathematics.Rect(
					margin,
					margin,
					(float)(context.Width - margin),
					(float)(context.Height - margin)
				),
				borderBrush,
				(float)context.BorderWidth
			);

			for (var c = 1; c < context.ColCount; c++)
			{
				var x = c * cellWidth;
				ctx.DrawLine(
					new Vector2(x, margin),
					new Vector2(x, (float)context.Height),
					borderBrush,
					(float)context.BorderWidth
				);
			}
			for (var r = 1; r < context.RowCount; r++)
			{
				var y = r * cellHeight;
				ctx.DrawLine(
					new Vector2(margin, y),
					new Vector2((float)context.Width, y),
					borderBrush,
					(float)context.BorderWidth
				);
			}
		}

		ctx.EndDraw();
		ctx.Target = null;
		commandList.Close();
	}

	/// <summary>
	/// テーブル描画に必要なパラメータをまとめた構造体
	/// </summary>
	readonly record struct TableRenderContext(
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
		int RowCount,
		int ColCount,
		float RealOuterWidth,
		float OuterMargin,
		Color BackgroundColor,
		Color BorderColor,
		Color OuterBorderColor
	);

	const string DefaultFontName = "Yu Gothic UI";
	const float DefaultFontSize = 34f;

	private static void RenderTableCells(
		TableRenderContext ctx
	)
	{
		var cellWidthBg =
			(float)(ctx.Width - ctx.RealOuterWidth)
			/ ctx.ColCount;
		var cellHeightBg =
			(float)(ctx.Height - ctx.RealOuterWidth)
			/ ctx.RowCount;

		for (var r = 0; r < ctx.RowCount; r++)
		{
			for (var c = 0; c < ctx.ColCount; c++)
			{
				if (
					r >= ctx.Model.Rows
					|| c >= ctx.Model.Cols
				)
				{
					continue;
				}

				var cell = ctx.Model.Cells[r][c];

				var left =
					c * cellWidthBg + ctx.OuterMargin;
				var top =
					r * cellHeightBg + ctx.OuterMargin;

				// セル背景
				ctx.DeviceContext.FillRectangle(
					new Vortice.Mathematics.Rect(
						left,
						top,
						cellWidthBg,
						cellHeightBg
					),
					ctx.CellBackgroundBrush!
				);

				// 文字描画（枠線＋padding分だけ内側にオフセット）
				if (string.IsNullOrEmpty(cell.Text))
					continue;

				var padding = 4f;
				var borderAndOuter =
					(float)ctx.BorderWidth
					+ (float)ctx.OuterBorderWidth;

				var fSize = (float)
					cell.FontSize.GetValue(
						ctx.Frame,
						ctx.Length,
						ctx.Fps
					);

				using var textFormat =
					ctx.WriteFactory!.CreateTextFormat(
						cell.Font is not ""
							? cell.Font
							: DefaultFontName,
						fSize > 0f ? fSize : DefaultFontSize
					);

				ctx.DeviceContext.DrawText(
					cell.Text,
					textFormat,
					new Vortice.Mathematics.Rect(
						left + borderAndOuter + padding,
						top + borderAndOuter + padding,
						MathF.Max(
							0f,
							cellWidthBg
								- (borderAndOuter + padding)
									* 2f
						),
						MathF.Max(
							0f,
							cellHeightBg
								- (borderAndOuter + padding)
									* 2f
						)
					),
					ctx.DeviceContext!.CreateSolidColorBrush(
						new(
							red: cell.FontColor.R,
							green: cell.FontColor.G,
							blue: cell.FontColor.B,
							alpha: cell.FontColor.A
						)
					)
				);
			}
		}
	}

	//TODO: 行列の線ごとに制御点を作成する
	void UpdateControllerPoints(
		int frame,
		int length,
		int fps
	)
	{
		var rBoundaries = Parameter
			.TableModel
			.RowBoundaries;
		var cBoundaries = Parameter
			.TableModel
			.ColumnBoundaries;

		var controllerPoints = new List<ControllerPoint>();

		for (var r = 0; r < rBoundaries.Count; r++)
		{
			for (var c = 0; c < cBoundaries.Count; c++)
			{
				var rowIndex = r;
				var colIndex = c;

				// Animation型で値取得・編集
				var x = (float)
					cBoundaries[colIndex]
						.GetValue(frame, length, fps);

				var y = (float)
					rBoundaries[rowIndex]
						.GetValue(frame, length, fps);

				controllerPoints.Add(
					new ControllerPoint(
						new(x, y, 0),
						a =>
						{
							cBoundaries[colIndex]
								.AddToEachValues(a.Delta.Y);

							rBoundaries[rowIndex]
								.AddToEachValues(a.Delta.X);
						}
					)
				);
			}
		}

		var controller = new VideoController(
			controllerPoints
		)
		{
			Connection =
				VideoControllerPointConnection.Line,
		};
		Controllers = [controller];
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				disposer.DisposeAndClear();
			}
			_rowBoundaries = [];
			_columnBoundaries = [];
			_tableModel = null;
			_disposedValue = true;
		}
	}

	// // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
	// ~SamplePolygonShapeSource()
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
