using System.Diagnostics;
using YukkuriMovieMaker.Player.Video;

namespace YMM4TableShapePlugin;

internal partial class TableShapeSource : IShapeSource2
{
	[Conditional("DEBUG")]
	private void DebugOuterBorderWidth(
		int frame,
		int length,
		int fps,
		TableShapeParameter parameter,
		double _outerBorderWidth
	)
	{
		var debugOuterBorderWidth =
			parameter.OuterBorderWidth.GetValue(
				frame,
				length,
				fps
			);
		System.Diagnostics.Debug.WriteLine(
			$"Update called: OuterBorderWidth={debugOuterBorderWidth}, _outerBorderWidth={_outerBorderWidth}"
		);
	}
}
