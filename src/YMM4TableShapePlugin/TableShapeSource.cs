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
using System.Diagnostics;
using Vortice.Mathematics;

namespace YMM4TableShapePlugin;

internal partial class TableShapeSource : IShapeSource2
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
	bool _disposedValue;
	ID2D1CommandList? commandList;

	TableDrawCache? _lastDrawCache;
	TableDrawCollections? _lastDrawCollections;

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

		var screen =
			timelineItemSourceDescription.ScreenSize;

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

		var currentCache = new TableDrawCache(
			Model: model,
			Row: row,
			Col: col,
			Width: width,
			Height: height,
			BackgroundColor: bgColor,
			BorderColor: borderColor,
			BorderWidth: borderWidth,
			OuterBorderWidth: outerBorderWidth,
			OuterBorderColor: outerBorderColor,
			IsShowHeaderRow: Parameter.IsShowHeaderRow,
			HeaderRowBackgroundColor: Parameter.HeaderRowBackgroundColor,
			IsShowHeaderColumn: Parameter.IsShowHeaderColumn,
			HeaderColumnBackgroundColor: Parameter.HeaderColumnBackgroundColor
		);
		var currentListCache = new TableDrawCollections(
			rBoundaries,
			cBoundaries,
			textLists,
			cellLists
		);

		//変更がない場合は戻る
		if (
			commandList is not null
			&& !isFirst
			&& _lastDrawCache is not null
			&& _lastDrawCache.Equals(currentCache)
			&& _lastDrawCollections is not null
			&& _lastDrawCollections.Equals(currentListCache)
		)
		{
			Debug.WriteLine(
				"Update: cache hit, skip redraw"
			);
			return;
		}

		Debug.WriteLine(
			$"""Update: cache miss, redraw {1}"""
		);
		var sw = Stopwatch.StartNew();
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
		Debug.WriteLine(
			$"DrawTableCells took {sw.ElapsedMilliseconds} ms"
		);

		//制御点を作成する
		//UpdateControllerPoints(frame, length, fps);

		//キャッシュ用の情報を保存しておく
		isFirst = false;
		_lastDrawCache = currentCache;
		_lastDrawCollections = currentListCache;
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
	System.Windows.Media.Color? cachedOuterBorderColor;
	float? cachedOuterBorderWidth;

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

		//一番外側のテーブルの外枠線(OuterBorder)+セルの内枠線
		//	枠線幅0の場合は描画しない
		if (context.OuterBorderWidth > 0)
		{
			DrawTableOuterBorders(
				context,
				outerBorderBrush
			);
		}

		//一番外側のテーブルの枠線(Border)+グリッド線
		//	これも幅0なら描画しない
		//	線の中心座標は <see cref="DrawTableOuterBorder"/> と同じ
		//	`DrawTableOuterBorders`の **あとに** 描画して同じ位置に重ねる
		if (context.BorderWidth > 0)
		{
			DrawTableBorders(
				context,
				borderBrush
			);
		}

		ctx.EndDraw();
		ctx.Target = null;
		commandList.Close();
	}

	/// <summary>
	/// 一番外側のテーブルの枠線(OuterBorder)+グリッド線を描画する
	///
	/// <para>
	/// - 線の中心座標は  <see cref="DrawTableOuterBorder"/> と同じ
	/// - <see cref="DrawTableOuterBorder"/> の **あとに** 描画して同じ位置に重ねる
	/// </para>
	/// </summary>
	///
	/// <param name="context"></param>
	/// <param name="borderBrush"></param>
	private static void DrawTableBorders(
		TableRenderContext context,
		ID2D1SolidColorBrush borderBrush
	)
	{
		var ctx = context.DeviceContext;
		//一番外側のテーブルの枠線
		ctx.DrawRectangle(
			CalcTableOuterBorderRect(context),
			borderBrush,
			(float)context.BorderWidth
		);

		//セルの間のグリッド線
		var brush = borderBrush;
		var bWidth = (float)context.BorderWidth;

		DrawTableGridLines(
			context,
			brush,
			bWidth
		);
	}

	/// <summary>
	/// 一番外側のテーブルの外枠線(OuterBorder)+セルの内枠線を描画する
	/// </summary>
	/// <param name="context"></param>
	/// <param name="outerBorderBrush"></param>
	private static void DrawTableOuterBorders(
		TableRenderContext context,
		ID2D1SolidColorBrush outerBorderBrush
	)
	{
		var ctx = context.DeviceContext;
		//一番外側のテーブルの外枠線
		ctx.DrawRectangle(
			CalcTableOuterBorderRect(context),
			outerBorderBrush,
			context.RealOuterWidth
		);

		//セルの間のグリッド線
		//先に描画しておくことでセルの内枠線に見える
		var brush = outerBorderBrush;
		var bWidth = context.RealOuterWidth;

		DrawTableGridLines(
			context,
			brush,
			bWidth
		);
	}

	/// <summary>
	/// セルの間のグリッド線描画
	/// </summary>
	/// <param name="context"></param>
	/// <param name="brush"></param>
	/// <param name="borderWidth"></param>
	static void DrawTableGridLines(
		TableRenderContext context,
		ID2D1SolidColorBrush brush,
		float borderWidth
	)
	{
		var ctx = context.DeviceContext;
		var margin = context.RealOuterWidth / 2f;
		var rect = CalculateCellRect(
			1,
			1,
			context.RowCount,
			context.ColCount,
			context.Width,
			context.Height,
			context.RealOuterWidth
		);
		for (var c = 1; c < context.ColCount; c++)
		{
			var x =
				rect.Width * c
				//テーブル外枠
				+ context.RealOuterWidth
				//セル間分
				+ context.RealOuterWidth * (c - 1)
				//枠線offset
				+ margin;
			ctx.DrawLine(
				new Vector2(x, margin),
				new Vector2(
					x,
					(float)context.Height - margin
				),
				brush,
				borderWidth
			);
		}
		for (var r = 1; r < context.RowCount; r++)
		{
			var y =
				rect.Height * r
				//テーブル外枠
				+ context.RealOuterWidth
				//セル間分
				+ context.RealOuterWidth * (r - 1)
				//枠線offset
				+ margin;
			ctx.DrawLine(
				new Vector2(margin, y),
				new Vector2(
					(float)context.Width - margin,
					y
				),
				brush,
				borderWidth
			);
		}
	}

	/// <summary>
	/// 一番外側のテーブルの枠線Rectを計算
	/// </summary>
	/// <param name="context"></param>
	/// <returns>一番外側のテーブルの枠線Rect</returns>
	internal static Rect CalcTableOuterBorderRect(
		TableRenderContext context
	)
	{
		var margin = context.RealOuterWidth / 2f;
		return new Rect(
			margin,
			margin,
			(float)context.Width - margin * 2,
			(float)context.Height - margin * 2
		);
	}

	const string DefaultFontName = "Yu Gothic UI";
	const float DefaultFontSize = 34f;

	private static void RenderTableCells(
		TableRenderContext ctx
	)
	{
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

				// ここで座標計算メソッドを利用
				var cellRect = CalculateCellRect(
					r,
					c,
					ctx.RowCount,
					ctx.ColCount,
					ctx.Width,
					ctx.Height,
					ctx.RealOuterWidth
				);

				// セル背景
				ctx.DeviceContext.FillRectangle(
					cellRect,
					ctx.CellBackgroundBrush!
				);

				var padding = 4f;

				var leftText =
					cellRect.Left
					+ padding;
				var topText =
					cellRect.Top + padding;
				var widthText = MathF.Max(
					0f,
					cellRect.Width
						- padding * 2
				);
				var heightText = MathF.Max(
					0f,
					cellRect.Height
						- padding * 2
				);
				var textRect = new Rect(
					leftText,
					topText,
					widthText,
					heightText
				);

				var rightText = leftText + widthText;
				var bottomText = topText + heightText;
				var rightCell =
					cellRect.Left + cellRect.Width;
				var bottomCell =
					cellRect.Top + cellRect.Height;

				var leftGap = leftText - cellRect.Left;
				var rightGap = rightCell - rightText;
				var topGap = topText - cellRect.Top;
				var bottomGap = bottomText - bottomCell;

				var fSize = (float)
					cell.FontSize.GetValue(
						ctx.Frame,
						ctx.Length,
						ctx.Fps
					);

				using var textFormat =
					ctx.WriteFactory!.CreateTextFormat(
						fontFamilyName: cell.Font is not ""
							? cell.Font
							: DefaultFontName,
						fontSize: fSize > 0f
							? fSize
							: DefaultFontSize,
						fontStretch: FontStretch.Normal,
						fontStyle: cell.IsFontItalic switch
						{
							true => FontStyle.Italic,
							false => FontStyle.Normal,
						},
						fontWeight: cell.IsFontBold switch
						{
							true => FontWeight.Bold,
							false => FontWeight.Normal,
						}
					);

				textFormat.TextAlignment =
					cell.TextAlign switch
					{
						CellContentAlign.TopLeft
						or CellContentAlign.MiddleLeft
						or CellContentAlign.BottomLeft =>
							TextAlignment.Leading,
						CellContentAlign.TopCenter
						or CellContentAlign.MiddleCenter
						or CellContentAlign.BottomCenter =>
							TextAlignment.Center,
						CellContentAlign.TopRight
						or CellContentAlign.MiddleRight
						or CellContentAlign.BottomRight =>
							TextAlignment.Trailing,
						_ => TextAlignment.Leading,
					};
				textFormat.ParagraphAlignment =
					cell.TextAlign switch
					{
						CellContentAlign.TopLeft
						or CellContentAlign.TopCenter
						or CellContentAlign.TopRight =>
							ParagraphAlignment.Near,
						CellContentAlign.MiddleLeft
						or CellContentAlign.MiddleCenter
						or CellContentAlign.MiddleRight =>
							ParagraphAlignment.Center,
						CellContentAlign.BottomLeft
						or CellContentAlign.BottomCenter
						or CellContentAlign.BottomRight =>
							ParagraphAlignment.Far,
						_ => ParagraphAlignment.Near,
					};

				Debug.WriteLine(
					$"Cell[{r},{c}] Text=\"{cell.Text}\" Align={cell.TextAlign} "
						+ $"Rect=({leftText},{topText},{widthText},{heightText}) CellRect=({cellRect.Left},{cellRect.Top},{cellRect.Width},{cellRect.Height}) "
						+ $"Gaps: left={leftGap}, right={rightGap}, top={topGap}, bottom={bottomGap} "
						+ $"TextAlignment={textFormat.TextAlignment} ParagraphAlignment={textFormat.ParagraphAlignment} FontSize={fSize}"
				);

				ctx.DeviceContext.DrawText(
					cell.Text,
					textFormat,
					textRect,
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

	internal static Rect CalculateCellRect(
		int row,
		int col,
		int rowCount,
		int colCount,
		double width,
		double height,
		double realOuterBorderWidth
	)
	{
		//grid線分の計算をいれる

		//高さと幅
		// col/rowにかかわらず同じ高さ・幅
		var cellWidth =
			(float)(
				width
				//外枠分
				- realOuterBorderWidth * 2
				//セル間分(count - 1)
				- realOuterBorderWidth * (colCount - 1)
			) / colCount;
		var cellHeight =
			(float)(
				height
				- realOuterBorderWidth * 2
				- realOuterBorderWidth * (rowCount - 1)
			) / rowCount;

		//左上の座標
		var left =
			col * cellWidth
			//外枠分
			+ (float)realOuterBorderWidth
			//セル間分
			+ (float)realOuterBorderWidth * col;
		var top =
			row * cellHeight
			+ (float)realOuterBorderWidth
			+ (float)realOuterBorderWidth * row;
		return new Rect(left, top, cellWidth, cellHeight);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				disposer.DisposeAndClear();
			}
			//_rowBoundaries = [];
			//_columnBoundaries = [];
			//_tableModel = null;
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
