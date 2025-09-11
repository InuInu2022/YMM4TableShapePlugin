using System.Diagnostics.CodeAnalysis;

using Vortice.Direct2D1;
using Vortice.DirectWrite;

using YukkuriMovieMaker.Player.Video;

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
			FontWeight fontWeight,
			double lineHeightRate,
			LineSpacingMethod lineSpacingMethod
		),
		IDWriteTextFormat
	> textFormatCache = [];

	/// <summary>
	/// TextBrushキャッシュ用フィールド
	/// </summary>
	readonly Dictionary<
		(byte R, byte G, byte B, byte A),
		ID2D1SolidColorBrush
	> textBrushCache = [];

	/// <summary>
	/// Fontキャッシュ用フィールド
	/// </summary>
	static readonly Dictionary<
		(
			string fontFamily,
			FontStyle fontStyle,
			FontWeight fontWeight
		),
		IDWriteFont
	> FontStyleCache = [];

	static readonly Dictionary<string, FontWeight> FontWeightSuffixes = new(
		StringComparer.OrdinalIgnoreCase
	)
	{
		{ " ExtraBold", FontWeight.ExtraBold },
		{ " UltraBold", FontWeight.UltraBold },
		{ " Bold", FontWeight.Bold },
		{ " SemiBold", FontWeight.SemiBold },
		{ " DemiBold", FontWeight.DemiBold },
		{ " Medium", FontWeight.Medium },
		{ " Light", FontWeight.Light },
		{ " ExtraLight", FontWeight.ExtraLight },
		{ " UltraLight", FontWeight.UltraLight },
		{ " Thin", FontWeight.Thin },
		{ " Black", FontWeight.Black },
		{ " Heavy", FontWeight.Heavy },
		{ " UltraBlack", FontWeight.UltraBlack },
		{ " Normal", FontWeight.Normal },
		{ " Regular", FontWeight.Normal },
		{ " SemiLight", FontWeight.SemiLight },
		{ " ExtraBlack", FontWeight.ExtraBlack },
		{ " 太", FontWeight.Bold },
		{ " 標準", FontWeight.Medium },
		{ " 細", FontWeight.Light },
		{ " 極細", FontWeight.ExtraLight },
		{ "-Light", FontWeight.Light },
		{ "-Thin", FontWeight.Thin },
		{ "-Black", FontWeight.Black },
		{ "-Bold", FontWeight.Bold },
		{ "-Heavy", FontWeight.Heavy },
		{ "-Medium", FontWeight.Medium },
		{ "-Regular", FontWeight.Normal },
		{ "-Normal", FontWeight.Normal },
		{ "-SemiBold", FontWeight.SemiBold },
		{ "-DemiBold", FontWeight.DemiBold },
		{ "-ExtraBold", FontWeight.ExtraBold },
		{ "-UltraBold", FontWeight.UltraBold },
		{ "-UltraBlack", FontWeight.UltraBlack },
		{ "-ExtraLight", FontWeight.ExtraLight },
		{ "-UltraLight", FontWeight.UltraLight },
	};

	static readonly Dictionary<string, FontStyle> FontStyleSuffixes = new(StringComparer.OrdinalIgnoreCase)
	{
		{ " Italic", FontStyle.Italic },
		{ " Oblique", FontStyle.Oblique },
	};

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
