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
		if (
			Parameter.Width.GetValue(frame, length, fps)
				is double width
			&& Parameter.ColumnCount.GetValue(
				frame,
				length,
				fps
			)
				is double cols
			&& cols > 0
		)
		{
			var colStep = width / cols;
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
		if (
			Parameter.Height.GetValue(frame, length, fps)
				is double height
			&& Parameter.RowCount.GetValue(
				frame,
				length,
				fps
			)
				is double rows
			&& rows > 0
		)
		{
			var rowStep = height / rows;
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
			frame,
			length,
			fps,
			model,
			borderWidth,
			outerBorderWidth,
			bgColor,
			borderColor,
			outerBorderColor
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
	}

	ID2D1SolidColorBrush? cachedOuterBorderBrush;
	Color? cachedOuterBorderColor;
	float? cachedOuterBorderWidth;

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"SMA0040:Missing Using Statement",
		Justification = "<保留中>"
	)]
	void DrawTableCells(
		int frame,
		int length,
		int fps,
		TableModel model,
		double borderWidth,
		double outerBorderWidth,
		Color bgColor,
		Color borderColor,
		Color outerBorderColor
	)
	{
		var ctx = Devices.DeviceContext;

		// outerBorderBrushキャッシュ（色・太さ変更時のみ再生成）
		if (
			cachedOuterBorderBrush is null
			|| cachedOuterBorderColor != outerBorderColor
			|| cachedOuterBorderWidth
				!= (float)outerBorderWidth
		)
		{
			disposer.RemoveAndDispose(
				ref cachedOuterBorderBrush
			);
			cachedOuterBorderBrush =
				ctx.CreateSolidColorBrush(
					new(
						outerBorderColor.R,
						outerBorderColor.G,
						outerBorderColor.B,
						outerBorderColor.A
					)
				);
			disposer.Collect(cachedOuterBorderBrush);
			cachedOuterBorderColor = outerBorderColor;
			cachedOuterBorderWidth =
				(float)outerBorderWidth;
		}

		if (commandList is not null)
			disposer.RemoveAndDispose(ref commandList);
		commandList = ctx.CreateCommandList();
		disposer.Collect(commandList);

		var cellBgBrush = ctx.CreateSolidColorBrush(
			new(bgColor.R, bgColor.G, bgColor.B, bgColor.A)
		);
		var borderBrush = ctx.CreateSolidColorBrush(
			new(
				borderColor.R,
				borderColor.G,
				borderColor.B,
				borderColor.A
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

		var fontName = "Yu Gothic UI";
		var fontSize = 34;

		using var dwFactory =
			DWrite.DWriteCreateFactory<IDWriteFactory>(
				Vortice.DirectWrite.FactoryType.Shared
			);
		using var textFormat = dwFactory.CreateTextFormat(
			fontName,
			fontSize
		);

		ctx.Target = commandList;
		ctx.BeginDraw();
		ctx.Clear(null);

		var width = Parameter.Width.GetValue(
			frame,
			length,
			fps
		);
		var height = Parameter.Height.GetValue(
			frame,
			length,
			fps
		);
		var rowCount = (int)
			Parameter.RowCount.GetValue(frame, length, fps);
		var colCount = (int)
			Parameter.ColumnCount.GetValue(
				frame,
				length,
				fps
			);

		// セル背景・テキスト描画
		//   最背面に来るように先に描画
		var cellWidthBg = (float)width / colCount;
		var cellHeightBg = (float)height / rowCount;

		for (var r = 0; r < rowCount; r++)
		{
			for (var c = 0; c < colCount; c++)
			{
				if (r >= model.Rows || c >= model.Cols)
					continue;

				var cell = model.Cells[r][c];

				var left = c * cellWidthBg;
				var top = r * cellHeightBg;

				// セル背景
				ctx.FillRectangle(
					new Vortice.Mathematics.Rect(
						left,
						top,
						cellWidthBg,
						cellHeightBg
					),
					cellBgBrush
				);

				// 文字描画（枠線＋padding分だけ内側にオフセット）
				if (string.IsNullOrEmpty(cell.Text))
					continue;

				var padding = 4f;
				var borderAndOuter =
					(float)borderWidth
					+ (float)outerBorderWidth;

				ctx.DrawText(
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
					cell?.FontColor is not null
						? ctx.CreateSolidColorBrush(
							new(
								red: cell.FontColor.R,
								green: cell.FontColor.G,
								blue: cell.FontColor.B,
								alpha: cell.FontColor.A
							)
						)
						: textBrush
				);
			}
		}

		// 外枠線（outerBorder）を背面に描画
		var cellWidth = (float)width / colCount;
		var cellHeight = (float)height / rowCount;

		// 中心揃えて重ねるので外枠幅*2 + グリッド線幅
		var realOuterWidth = (float)(
			outerBorderWidth * 2 + borderWidth
		);
		if (outerBorderWidth > 0)
		{
			//一番外側はDrawRectで描画
			ctx.DrawRectangle(
				new Vortice.Mathematics.Rect(
					realOuterWidth / 2f,
					realOuterWidth / 2f,
					(float)width - realOuterWidth / 2f,
					(float)height - realOuterWidth / 2f
				),
				outerBorderBrush,
				realOuterWidth
			);

			// 縦線（列境界）
			for (var c = 1; c < colCount; c++)
			{
				var x = c * cellWidth;
				ctx.DrawLine(
					new Vector2(x, 0f),
					new Vector2(x, (float)height),
					outerBorderBrush,
					realOuterWidth
				);
			}
			// 横線（行境界）
			for (var r = 1; r < rowCount; r++)
			{
				var y = r * cellHeight;
				ctx.DrawLine(
					new Vector2(0f, y),
					new Vector2((float)width, y),
					outerBorderBrush,
					realOuterWidth
				);
			}
		}

		// グリッド線（border）を一括描画
		//  グリッド線（border）は外枠線の中心に描画されないとダメ
		//  重ねるためにグリッド線描がの前に外枠線も同じように描画が必要
		if (borderWidth > 0)
		{
			//一番外側はDrawRectで描画
			// DrawLineだと角が欠ける
			var margin = realOuterWidth / 2f;

			ctx.DrawRectangle(
				new Vortice.Mathematics.Rect(
					margin,
					margin,
					(float)(width - margin),
					(float)(height - margin)
				),
				borderBrush,
				(float)borderWidth
			);

			// 縦線（列境界）
			for (var c = 1; c < colCount; c++)
			{
				var x = c * cellWidth;
				ctx.DrawLine(
					new Vector2(x, margin),
					new Vector2(x, (float)height),
					borderBrush,
					(float)borderWidth
				);
			}
			// 横線（行境界）
			for (var r = 1; r < rowCount; r++)
			{
				var y = r * cellHeight;
				ctx.DrawLine(
					new Vector2(margin, y),
					new Vector2((float)width, y),
					borderBrush,
					(float)borderWidth
				);
			}
		}

		ctx.EndDraw();
		ctx.Target = null;
		commandList.Close();
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
