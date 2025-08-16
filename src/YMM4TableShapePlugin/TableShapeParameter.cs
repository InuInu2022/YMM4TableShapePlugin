using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Documents;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;
using YukkuriMovieMaker.UndoRedo;

namespace YMM4TableShapePlugin;

internal class TableShapeParameter(
	SharedDataStore? sharedData
) : ShapeParameterBase(sharedData)
{
	[Display(Name = "幅")]
	[AnimationSlider("F0", "px", 0, 100)]
	public Animation Width { get; } =
		new Animation(500, 0, 10000);

	[Display(Name = "高さ")]
	[AnimationSlider("F0", "px", 0, 100)]
	public Animation Height { get; } =
		new Animation(300, 0, 10000);

	[Display(Name = "aaaaaa", Description = "aaaaaa")]
	[AnimationSlider("F0", "", 1, 30)]
	public Animation BorderWidth { get; } =
		new(1, 1, 100000);

	[Display(Name = "行数", Description = "テーブルの行数")]
	[AnimationSlider("F0", "", 1, 100)]
	public Animation RowCount { get; } = new(1, 1, 100);

	[Display(Name = "列数", Description = "テーブルの列数")]
	[AnimationSlider("F0", "", 1, 100)]
	public Animation ColumnCount { get; } = new(1, 1, 100);

	[Display(AutoGenerateField = true)]
	public TableModel TableModel { get; set; } = new(1, 1);

	public TableShapeParameter()
		: this(null) { }

	public override IEnumerable<string> CreateMaskExoFilter(
		int keyFrameIndex,
		ExoOutputDescription desc,
		ShapeMaskExoOutputDescription shapeMaskParameters
	)
	{
		return [];
	}

	public override IEnumerable<string> CreateShapeItemExoFilter(
		int keyFrameIndex,
		ExoOutputDescription desc
	)
	{
		return [];
	}

	public override IShapeSource CreateShapeSource(
		IGraphicsDevicesAndContext devices
	)
	{
		return new TableShapeSource(devices, this);
	}

	protected override void SaveSharedData(
		SharedDataStore store
	)
	{
		store.Save(new SharedData(this));
	}

	protected override void LoadSharedData(
		SharedDataStore store
	)
	{
		var data = store.Load<SharedData>();
		if (data is null)
			return;
		data.CopyTo(this);
	}

	protected override IEnumerable<IAnimatable> GetAnimatables()
	{
		return
		[
			Width,
			Height,
			BorderWidth,
			RowCount,
			ColumnCount,
			.. TableModel.RowBoundaries,
			.. TableModel.ColumnBoundaries,
		];
	}
}
