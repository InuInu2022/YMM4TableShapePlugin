using System.Windows.Documents;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;

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
	IList<Animation> _rowBoundaries = [];
	IList<Animation> _columnBoundaries = [];
	TableModel? _tableModel;
	bool _disposedValue;
	ID2D1CommandList? commandList;
	readonly ID2D1CommandList emptyImage;

	public TableShapeSource(
		IGraphicsDevicesAndContext devices,
		TableShapeParameter parameter
	)
	{
		Devices = devices;
		Parameter = parameter;

		// ダミー画像（最低限の1x1矩形を描画したコマンドリスト）を生成して保持
		var ctx = Devices.DeviceContext;
		emptyImage = ctx.CreateCommandList();
		ctx.Target = emptyImage;
		ctx.BeginDraw();
		// 1x1ピクセルの透明矩形を描画
		using var brush = ctx.CreateSolidColorBrush(
			new Vortice.Mathematics.Color4(0, 0, 0, 0)
		);
		ctx.FillRectangle(
			new Vortice.Mathematics.Rect(0, 0, 1, 1),
			brush
		);
		ctx.EndDraw();
		ctx.Target = null;
		emptyImage.Close();
		// emptyImageはDisposeCollectorで管理しない
	}

	/// <summary>
	/// 表の描画を行う。TableModelのセル・境界線・テキストを描画する。
	/// </summary>
	/// <param name="timelineItemSourceDescription">タイムライン情報</param>
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

		var model =
			Parameter.TableModel
			?? throw new InvalidOperationException(
				"TableModel is null"
			);
		var rBoundaries = model.RowBoundaries;
		var cBoundaries = model.ColumnBoundaries;

		//変更がない場合は戻る
		if (
			commandList is not null
			&& !isFirst
			&& _tableModel == model
			&& _rowBoundaries == rBoundaries
			&& _columnBoundaries == cBoundaries
			&& _rowBoundaries.SequenceEqual(rBoundaries)
			&& _columnBoundaries.SequenceEqual(cBoundaries)
		)
		{
			return;
		}

		// Direct2Dリソース取得
		var ctx = Devices.DeviceContext;

		// コマンドリスト生成
#pragma warning disable SMA0040 // Missing Using Statement
		if (commandList is not null)
			disposer.RemoveAndDispose(ref commandList);
		commandList = ctx.CreateCommandList();
		disposer.Collect(commandList);

		// 描画開始
		var cellBgBrush = ctx.CreateSolidColorBrush(
			new(1f, 1f, 1f, 1f)
		);

		var borderBrush = ctx.CreateSolidColorBrush(
			new(0f, 0f, 0f, 1f)
		);
		var textBrush = ctx.CreateSolidColorBrush(
			new(0f, 0f, 0f, 1f)
		);
		disposer.Collect(textBrush);
		disposer.Collect(cellBgBrush);
		disposer.Collect(borderBrush);
#pragma warning restore SMA0040 // Missing Using Statement

		var borderWidth = 1.0f;
		var fontName = "Yu Gothic UI";
		var fontSize = 20;

		ctx.Target = commandList;
		ctx.BeginDraw();

		ctx.Clear(null);

		// セル描画
		for (int r = 0; r < model.Rows; r++)
		{
			for (int c = 0; c < model.Cols; c++)
			{
				var cell = model.Cells[r][c];

				// 境界座標取得（安全な範囲チェック）
				if (
					c + 1 >= model.ColumnBoundaries.Count
					|| r + 1 >= model.RowBoundaries.Count
				)
				{
					continue;
				}

				var left = model
					.ColumnBoundaries[c]
					.Values[0]
					.Value;
				var top = model
					.RowBoundaries[r]
					.Values[0]
					.Value;
				var right = model
					.ColumnBoundaries[c + 1]
					.Values[0]
					.Value;
				var bottom = model
					.RowBoundaries[r + 1]
					.Values[0]
					.Value;

				ctx.FillRectangle(
					new Vortice.Mathematics.Rect(
						(float)left,
						(float)top,
						(float)(right - left),
						(float)(bottom - top)
					),
					cellBgBrush
				);

				ctx.DrawRectangle(
					new Vortice.Mathematics.Rect(
						(float)left,
						(float)top,
						(float)(right - left),
						(float)(bottom - top)
					),
					borderBrush,
					borderWidth
				);

				if (!string.IsNullOrEmpty(cell.Text))
				{
					using var dwFactory =
						DWrite.DWriteCreateFactory<IDWriteFactory>(
							Vortice
								.DirectWrite
								.FactoryType
								.Shared
						);
					using var textFormat =
						dwFactory.CreateTextFormat(
							fontName,
							fontSize
						);

					if (textFormat is not null)
					{
						ctx.DrawText(
							cell.Text,
							textFormat,
							new Vortice.Mathematics.Rect(
								(float)left + 4,
								(float)top + 4,
								(float)(right - left - 8),
								(float)(bottom - top - 8)
							),
							textBrush
						);
					}
				}
			}
		}

		ctx.EndDraw();
		ctx.Target = null;
		commandList.Close();

		//制御点を作成する
		UpdateControllerPoints(frame, length, fps);

		//キャッシュ用の情報を保存しておく
		isFirst = false;
		_rowBoundaries = rBoundaries;
		_columnBoundaries = cBoundaries;
		_tableModel = model;
	}

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
