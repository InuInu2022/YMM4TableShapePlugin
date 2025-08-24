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
	ID2D1SolidColorBrush? cachedBorderBrush;
	System.Windows.Media.Color? cachedBorderColor;
	ID2D1SolidColorBrush? cachedOuterBorderBrush;
	System.Windows.Media.Color? cachedOuterBorderColor;
	ID2D1SolidColorBrush? cachedCellBgBrush;
	System.Windows.Media.Color? cachedCellBgColor;

	/// <summary>
	/// TextFormatキャッシュ用フィールド
	/// </summary>
	readonly Dictionary<
		(
			string fontFamily,
			float fontSize,
			FontStyle fontStyle,
			FontWeight fontWeight
		),
		IDWriteTextFormat
	> textFormatCache = new();

	/// <summary>
	/// TextBrushキャッシュ用フィールド
	/// </summary>
	readonly Dictionary<
		(byte R, byte G, byte B, byte A),
		ID2D1SolidColorBrush
	> textBrushCache = new();

	[MemberNotNull(nameof(cachedBorderBrush))]
	[MemberNotNull(nameof(cachedOuterBorderBrush))]
	[MemberNotNull(nameof(cachedCellBgBrush))]
	[SuppressMessage(
		"Usage",
		"SMA0040:Missing Using Statement",
		Justification = "<保留中>"
	)]
	void CacheTableProperties(TableRenderContext context)
	{
		//毎回ブラシ生成しないように、色が変わったらブラシ再生成

		CachePropertyCore(
			ref cachedBorderBrush,
			ref cachedBorderColor,
			context.BorderColor
		);

		CachePropertyCore(
			ref cachedOuterBorderBrush,
			ref cachedOuterBorderColor,
			context.OuterBorderColor
		);

		CachePropertyCore(
			ref cachedCellBgBrush,
			ref cachedCellBgColor,
			context.BackgroundColor
		);

		void CachePropertyCore(
			[NotNull] ref ID2D1SolidColorBrush? cachedBrush,
			ref System.Windows.Media.Color? cachedColor,
			System.Windows.Media.Color newColor
		)
		{
			if (
				cachedBrush is null
				|| cachedColor != newColor
			)
			{
				disposer.RemoveAndDispose(ref cachedBrush);
				cachedBrush =
					context.DeviceContext.CreateSolidColorBrush(
						new(
							newColor.R,
							newColor.G,
							newColor.B,
							newColor.A
						)
					);
				disposer.Collect(cachedBrush);
				cachedColor = newColor;
			}
		}
	}
}
