using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Windows.Documents;
using System.Windows.Media;
using Epoxy;
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
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"SMA0040:Missing Using Statement",
		Justification = "<保留中>"
	)]
	void DrawTableCells(TableRenderContext context)
	{
		var ctx = Devices.DeviceContext;

		// outerBorderBrushキャッシュ（色・太さ変更時のみ再生成）
		CacheTableProperties(context);

		if (commandList is not null)
		{
			disposer.RemoveAndDispose(ref commandList);
		}
		commandList = ctx.CreateCommandList();
		disposer.Collect(commandList);

		var cellBgBrush = cachedCellBgBrush;
		var borderBrush = cachedBorderBrush;
		var outerBorderBrush = cachedOuterBorderBrush;
		var headerRowBackgroundBrush =
			context.HeaderRowBackgroundColor is not null
				? ctx.CreateSolidColorBrush(
					new(
						context
							.HeaderRowBackgroundColor
							.Value
							.R,
						context
							.HeaderRowBackgroundColor
							.Value
							.G,
						context
							.HeaderRowBackgroundColor
							.Value
							.B,
						context
							.HeaderRowBackgroundColor
							.Value
							.A
					)
				)
				: null;
		var headerColumnBackgroundBrush =
			context.HeaderColumnBackgroundColor is not null
				? ctx.CreateSolidColorBrush(
					new(
						context
							.HeaderColumnBackgroundColor
							.Value
							.R,
						context
							.HeaderColumnBackgroundColor
							.Value
							.G,
						context
							.HeaderColumnBackgroundColor
							.Value
							.B,
						context
							.HeaderColumnBackgroundColor
							.Value
							.A
					)
				)
				: null;

		disposer.Collect(cellBgBrush);
		disposer.Collect(borderBrush);
		disposer.Collect(outerBorderBrush);
		if (headerRowBackgroundBrush is not null)
			disposer.Collect(headerRowBackgroundBrush);
		if (headerColumnBackgroundBrush is not null)
			disposer.Collect(headerColumnBackgroundBrush);

		var dwFactory =
			DWrite.DWriteCreateFactory<IDWriteFactory>(
				Vortice.DirectWrite.FactoryType.Shared
			);
		disposer.Collect(dwFactory);

		ctx.Target = commandList;
		ctx.BeginDraw();
		ctx.Clear(clearColor: null);

		// セル背景・テキスト描画
		var sw = Stopwatch.StartNew();
		RenderTableCells(
			context with
			{
				CellBackgroundBrush = cellBgBrush,
				WriteFactory = dwFactory,
			},
			headerRowBackgroundBrush,
			headerColumnBackgroundBrush
		);
		sw.Stop();
		Debug.WriteLine(
			"- RenderTableCells:" + sw.ElapsedMilliseconds
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
			DrawTableBorders(context, borderBrush);
		}

		ctx.EndDraw();
		ctx.Target = null;
		commandList.Close();
	}

	// セルスタイル決定用メソッド
	// CellStyle型は存在しないため、必要なプロパティをValueTupleで返す
	static (
		string font,
		Animation fontSize,
		System.Windows.Media.Color fontColor,
		System.Windows.Media.Color fontOutlineColor,
		CellTextStyle textStyle,
		CellContentAlign textAlign,
		Animation padding,
		bool isFontBold,
		bool isFontItalic,
		Animation lineHeightRate
	) GetEffectiveCellStyle(
		Models.TableCell cell,
		TableShapeParameter param
	)
	{
		// セル個別スタイル優先か共通スタイルかを分岐
		return cell.StylePriority switch
		{
			CellStylePriority.Inherit => (
				param.CellFont,
				param.CellFontSize,
				param.CellFontColor,
				param.CellFontOutlineColor,
				param.CellTextStyle,
				param.CellTextAlign,
				param.CellPadding,
				param.IsCellFontBold,
				param.IsCellFontItalic,
				param.CellLineHeightRate
			),
			CellStylePriority.Override => (
				cell.Font,
				cell.FontSize,
				cell.FontColor,
				cell.FontOutlineColor,
				cell.TextStyle,
				cell.TextAlign,
				cell.FontPadding,
				cell.IsFontBold,
				cell.IsFontItalic,
				cell.FontLineHeightRate
			),
			_ => (
				param.CellFont,
				param.CellFontSize,
				param.CellFontColor,
				param.CellFontOutlineColor,
				param.CellTextStyle,
				param.CellTextAlign,
				param.CellPadding,
				param.IsCellFontBold,
				param.IsCellFontItalic,
				param.CellLineHeightRate
			),
		};
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Usage",
		"SMA0040:Missing Using Statement",
		Justification = "<保留中>"
	)]
	void RenderTableCells(
		TableRenderContext ctx,
		ID2D1SolidColorBrush? headerRowBackgroundBrush,
		ID2D1SolidColorBrush? headerColumnBackgroundBrush
	)
	{
		for (var r = 0; r < ctx.RowCount; r++)
		{
			for (var c = 0; c < ctx.ColCount; c++)
			{
				if (
					r >= ctx.Model.Rows
					|| c >= ctx.Model.Cols
					|| r >= ctx.Model.Cells.Count
					|| c >= ctx.Model.Cells[r].Count
				)
				{
					continue;
				}

				var cell = ctx.Model.Cells[r][c];

				var cellRect = CalculateCellRect(
					r,
					c,
					ctx.RowCount,
					ctx.ColCount,
					ctx.Width,
					ctx.Height,
					ctx.RealOuterWidth
				);
				ctx.DeviceContext.FillRectangle(
					cellRect,
					GetCellBackgroundBrush(
						ctx,
						r,
						c,
						headerRowBackgroundBrush,
						headerColumnBackgroundBrush
					)
				);

				// --- ここからスタイル決定 ---
				var (
					fontFamily,
					fontSizeAnim,
					fontColor,
					fontOutlineColor,
					textStyle,
					textAlign,
					fontPadding,
					isFontBold,
					isFontItalic,
					lineHeightRate
				) = GetEffectiveCellStyle(cell, Parameter);

				var fSize = (float)
					fontSizeAnim.GetValue(
						ctx.Frame,
						ctx.Length,
						ctx.Fps
					);
				var fontFamilyNameRaw =
					!string.IsNullOrEmpty(fontFamily)
						? fontFamily
						: DefaultFontName;
				var (
					fontFamilyName,
					fontWeight,
					fontStyle
				) = ParseFontFamilyAndStyle(
					fontFamilyNameRaw,
					isFontBold,
					isFontItalic
				);
				var fontSize =
					fSize > 0f ? fSize : DefaultFontSize;

				var lineRate = lineHeightRate
					.GetValue(ctx.Frame, ctx.Length, ctx.Fps);
				var actualLineSpacing = (float)lineRate / 100.0f * fontSize;

				// 1行用と複数行用でキャッシュkeyを分離
				var key = (
					fontFamilyName,
					fontSize,
					fontStyle,
					fontWeight,
					lineRate,
					LineSpacingMethod.Default // 仮でDefault、後で切り替え
				);

				// textFormatキャッシュ取得
				if (!textFormatCache.TryGetValue(key, out var textFormat))
				{
					textFormat = ctx.WriteFactory!.CreateTextFormat(
						fontFamilyName: fontFamilyName,
						fontSize: fontSize,
						fontStretch: FontStretch.Normal,
						fontStyle: fontStyle,
						fontWeight: fontWeight
					);
					textFormatCache[key] = textFormat;
					disposer.Collect(textFormat);
				}

				textFormat.TextAlignment = textAlign switch
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
					textAlign switch
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

				var padding = fontPadding.GetValue(
					ctx.Frame,
					ctx.Length,
					ctx.Fps
				);
				var textRect = CalcInnerRect(
					cellRect,
					(float)padding
				);

				//行間スペース
				var textLayout = CreateTextLayout(
					ctx,
					cell,
					textFormat,
					textRect
				);
				var actualLineCount =
					GetLineMetricsAndSetSpacing(
						fontSize,
						textFormat,
						textLayout,
						actualLineSpacing
					);

				if (
					textStyle == CellTextStyle.ShapedBorder
					|| textStyle
						== CellTextStyle.RoundedBorder
				)
				{
					// 余分な高さ分を補正（中央揃え時のみ、1行は補正しない）
					if (
						textFormat.ParagraphAlignment
							== ParagraphAlignment.Center
						&& actualLineCount > 1
					)
					{
						var totalTextHeight =
							actualLineCount
							* actualLineSpacing;
						var cellCenterY =
							textRect.Top
							+ textRect.Height / 2;
						var newTop =
							cellCenterY
							- totalTextHeight / 2;
						var newBottom =
							newTop + totalTextHeight;
						textRect = new Rect(
							textRect.Left,
							newTop,
							textRect.Right,
							newBottom
						);
					}

					var outlineWidth = (float)
						ctx.OuterBorderWidth;
					var origin = new Vector2(
						textRect.Left,
						textRect.Top
					);

					// テキスト領域でクリッピングして描画（領域をはみ出す文字を切る）
					ctx.DeviceContext.PushAxisAlignedClip(
						cellRect,
						AntialiasMode.Aliased
					);
					try
					{
						var renderer =
							new Renderers.OutlineTextRenderer(
								ctx.DeviceContext,
								GetTextBrush(
									fontOutlineColor
								),
								GetTextBrush(fontColor),
								origin,
								outlineWidth,
								textStyle
							);
						disposer.Collect(renderer);
						textLayout.Draw(
							nint.Zero,
							renderer,
							0.0f,
							0.0f
						);
					}
					finally
					{
						ctx.DeviceContext.PopAxisAlignedClip();
					}
				}
				else if (textStyle == CellTextStyle.Normal)
				{
					// 通常描画も同様にテキスト矩形でクリップ
					ctx.DeviceContext.PushAxisAlignedClip(
						cellRect,
						AntialiasMode.Aliased
					);
					try
					{
						ctx.DeviceContext.DrawText(
							cell.Text,
							textFormat,
							textRect,
							GetTextBrush(fontColor)
						);
					}
					finally
					{
						ctx.DeviceContext.PopAxisAlignedClip();
					}
				}
			}
		}
	}

	static int GetLineMetricsAndSetSpacing(
		float fontSize,
		IDWriteTextFormat textFormat,
		IDWriteTextLayout textLayout,
		float actualLineSpacing
	)
	{
		var metrics = new LineMetrics[10];
		textLayout.GetLineMetrics(
			metrics,
			out var actualLineCount
		);

		if (actualLineCount > 1)
		{
			textFormat.SetLineSpacing(
				LineSpacingMethod.Uniform,
				actualLineSpacing,
				fontSize * 0.85f
			);
		}
		else
		{
			textFormat.SetLineSpacing(
				LineSpacingMethod.Default,
				fontSize,
				fontSize * 0.85f
			);
		}

		return actualLineCount;
	}

	[SuppressMessage("Usage", "SMA0040:Missing Using Statement", Justification = "<保留中>")]
	private IDWriteTextLayout CreateTextLayout(
		TableRenderContext ctx,
		Models.TableCell cell,
		IDWriteTextFormat textFormat,
		Rect textRect
	)
	{
		var textLayout = ctx.WriteFactory!.CreateTextLayout(
			cell.Text,
			textFormat,
			textRect.Width,
			textRect.Height
		);
		disposer.Collect(textLayout);
		return textLayout;
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

		DrawTableGridLines(context, brush, bWidth);
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

		DrawTableGridLines(context, brush, bWidth);
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

	[SuppressMessage("Usage", "SMA0040")]
	ID2D1SolidColorBrush GetTextBrush(
		System.Windows.Media.Color color
	)
	{
		var key = (color.R, color.G, color.B, color.A);
		if (!textBrushCache.TryGetValue(key, out var brush))
		{
			brush =
				Devices.DeviceContext.CreateSolidColorBrush(
					new(color.R, color.G, color.B, color.A)
				);
			textBrushCache[key] = brush;
			disposer.Collect(brush);
		}
		return brush;
	}

	[SuppressMessage("Usage", "SMA0040")]
	ID2D1SolidColorBrush GetCellBackgroundBrush(
		TableRenderContext ctx,
		int r,
		int c,
		ID2D1SolidColorBrush? headerRowBackgroundBrush,
		ID2D1SolidColorBrush? headerColumnBackgroundBrush
	)
	{
		// セル背景
		return (
			Parameter.HeaderDisplay,
			isHeaderRow: r == 0,
			isHeaderCol: c == 0
		) switch
		{
			(
				ShowHeader.RowHeader
					or ShowHeader.BothHeader,
				true,
				_
			) => headerRowBackgroundBrush
				?? ctx.CellBackgroundBrush!,
			(
				ShowHeader.ColumnHeader
					or ShowHeader.BothHeader,
				_,
				true
			) => headerColumnBackgroundBrush
				?? ctx.CellBackgroundBrush!,
			(_, _, _) => ctx.CellBackgroundBrush!,
		};
	}

	static (
		string family,
		FontWeight weight,
		FontStyle style
	) ParseFontFamilyAndStyle(
		string fontFamilyName,
		bool isBold,
		bool isItalic
	)
	{
		var family = fontFamilyName;
		var weight = isBold
			? FontWeight.Bold
			: FontWeight.Normal;
		var style = isItalic
			? FontStyle.Italic
			: FontStyle.Normal;

		// FontWeightサフィックス対応表
		var weightSuffixes = new Dictionary<
			string,
			FontWeight
		>(StringComparer.OrdinalIgnoreCase)
		{
			{
				$" {nameof(FontWeight.ExtraBold)}",
				FontWeight.ExtraBold
			},
			{
				$" {nameof(FontWeight.UltraBold)}",
				FontWeight.UltraBold
			},
			{
				$" {nameof(FontWeight.Bold)}",
				FontWeight.Bold
			},
			{
				$" {nameof(FontWeight.SemiBold)}",
				FontWeight.SemiBold
			},
			{
				$" {nameof(FontWeight.DemiBold)}",
				FontWeight.DemiBold
			},
			{
				$" {nameof(FontWeight.Medium)}",
				FontWeight.Medium
			},
			{
				$" {nameof(FontWeight.Light)}",
				FontWeight.Light
			},
			{
				$" {nameof(FontWeight.ExtraLight)}",
				FontWeight.ExtraLight
			},
			{
				$" {nameof(FontWeight.UltraLight)}",
				FontWeight.UltraLight
			},
			{
				$" {nameof(FontWeight.Thin)}",
				FontWeight.Thin
			},
			{
				$" {nameof(FontWeight.Black)}",
				FontWeight.Black
			},
			{
				$" {nameof(FontWeight.Heavy)}",
				FontWeight.Heavy
			},
			{
				$" {nameof(FontWeight.UltraBlack)}",
				FontWeight.UltraBlack
			},
			{
				$" {nameof(FontWeight.Normal)}",
				FontWeight.Normal
			},
			{
				$" {nameof(FontWeight.Regular)}",
				FontWeight.Normal
			},
			{
				$" {nameof(FontWeight.SemiLight)}",
				FontWeight.SemiLight
			},
			{
				$" {nameof(FontWeight.ExtraBlack)}",
				FontWeight.ExtraBlack
			},
		};

		// FontStyleサフィックス対応表
		var styleSuffixes = new Dictionary<
			string,
			FontStyle
		>(StringComparer.OrdinalIgnoreCase)
		{
			{
				$" {nameof(FontStyle.Italic)}",
				FontStyle.Italic
			},
			{
				$" {nameof(FontStyle.Oblique)}",
				FontStyle.Oblique
			},
		};

		// 複数サフィックス対応: whileで繰り返し除去
		var found = true;
		while (found)
		{
			found = false;
			foreach (
				var kv in weightSuffixes.Where(kv =>
					family.EndsWith(
						kv.Key,
						StringComparison.OrdinalIgnoreCase
					)
				)
			)
			{
				weight = kv.Value;
				family = family[..^kv.Key.Length];
				found = true;
			}
			foreach (
				var kv in styleSuffixes.Where(kv =>
					family.EndsWith(
						kv.Key,
						StringComparison.OrdinalIgnoreCase
					)
				)
			)
			{
				style = kv.Value;
				family = family[..^kv.Key.Length];
				found = true;
			}
		}

		family = family.TrimEnd();
		return (family, weight, style);
	}
}
