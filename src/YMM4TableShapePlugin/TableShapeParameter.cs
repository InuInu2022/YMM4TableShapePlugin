using System.ComponentModel;
using System.Windows.Documents;
using YMM4TableShapePlugin.Models;
using YukkuriMovieMaker.Commons;
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
			.. TableModel.RowBoundaries,
			.. TableModel.ColumnBoundaries,
		];
	}
}
