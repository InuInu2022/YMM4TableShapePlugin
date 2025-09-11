using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Metadata;
using System.Windows.Documents;
using System.Windows.Media;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;
using YMM4TableShapePlugin.Enums;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using static YMM4TableShapePlugin.TableShapeSource;

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
			HeaderDisplay: Parameter.HeaderDisplay,
			HeaderRowBackgroundColor: Parameter.HeaderRowBackgroundColor,
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
				CellBackgroundBrush: null, // CellBackgroundBrushはDrawTableCells内で生成
				TextBrush: null, // TextBrushも同様
				WriteFactory: null, // WriteFactoryも同様
				Width: width,
				Height: height,
				RowCount: row,
				ColCount: col,
				(float)(outerBorderWidth * 2 + borderWidth),
				(float)(outerBorderWidth * 2 + borderWidth)
					/ 2f,
				bgColor,
				borderColor,
				outerBorderColor,
				Parameter.HeaderRowBackgroundColor,
				Parameter.HeaderColumnBackgroundColor
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



	const string DefaultFontName = "Yu Gothic UI";
	const float DefaultFontSize = 34f;

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
				foreach (var key in textFormatCache.Keys)
				{
					var b = textFormatCache[key];
					disposer.RemoveAndDispose(ref b);
				}
				foreach (var key in textBrushCache.Keys)
				{
					var brush = textBrushCache[key];
					disposer.RemoveAndDispose(ref brush);
				}
				foreach (var item in FontStyleCache.Keys)
				{
					var font = FontStyleCache[item];
					disposer.RemoveAndDispose(ref font);
				}
				textBrushCache.Clear();
				textFormatCache.Clear();
				FontStyleCache.Clear();
				disposer.DisposeAndClear();
			}

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
