using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows.Media;

using YMM4TableShapePlugin.Models;

using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor.CustomVisibilityAttributes;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YMM4TableShapePlugin;

internal class TableShapeParameter(
	SharedDataStore? sharedData
) : ShapeParameterBase(sharedData)
{


	[Display(Name = "幅")]
	[AnimationSlider("F0", "px", 0, 1000)]
	[DefaultValue(500)]
	public Animation Width { get; } =
		new Animation(500, 0, 10000);

	[Display(Name = "高さ")]
	[AnimationSlider("F0", "px", 0, 1000)]
	[DefaultValue(300)]
	public Animation Height { get; } =
		new Animation(300, 0, 10000);

	[Display(Name = "行数", Description = "テーブルの行数")]
	[AnimationSlider("F0", "", 1, 5)]
	[DefaultValue(1)]
	[Range(1, 100)]
	public Animation RowCount { get; } = new(1, 1, 100);

	[Display(Name = "列数", Description = "テーブルの列数")]
	[AnimationSlider("F0", "", 1, 5)]
	[DefaultValue(1)]
	[Range(1, 100)]
	public Animation ColumnCount { get; } = new(1, 1, 100);

	[Display(
		GroupName = "外観",
		Name = "枠の太さ",
		Description = "表の枠の太さ"
	)]
	[AnimationSlider("F0", "", 1, 10)]
	[DefaultValue(1)]
	[Range(1, 100000)]
	public Animation BorderWidth { get; } =
		new(1, 1, 100000);

	[Display(
		GroupName = "外観",
		Name = "外枠の太さ",
		Description = "表の外枠の太さ。0にすると消えます。"
	)]
	[AnimationSlider("F0", "", 0, 10)]
	[DefaultValue(1)]
	[Range(0, 100000)]
	public Animation OuterBorderWidth { get; } =
		new(0, 0, 100000);

	[Display(GroupName = "外観", Name = "枠の色")]
	[ColorPicker]
	public Color BorderColor
	{
		get => _borderColor;
		set => Set(ref _borderColor, value);
	}
	private Color _borderColor = Colors.Black;

	[Display(GroupName = "外観", Name = "外枠の色")]
	[ColorPicker]
	public Color OuterBorderColor
	{
		get => _outerBorderColor;
		set => Set(ref _outerBorderColor, value);
	}
	private Color _outerBorderColor = Colors.White;

	[Display(GroupName = "外観", Name = "背景色")]
	[ColorPicker]
	public Color BackgroundColor
	{
		get => _backgroundColor;
		set => Set(ref _backgroundColor, value);
	}

	private Color _backgroundColor = Colors.WhiteSmoke;

	#region header

	[Display(
		GroupName = "ヘッダー",
		Name = "ヘッダー列強調"
	)]
	[EnumComboBox]
	public ShowHeader HeaderDisplay
	{
		get { return _headerDisplay; }
		set { Set(ref _headerDisplay, value); }
	}
	ShowHeader _headerDisplay = ShowHeader.None;

	[Display(
		GroupName = "ヘッダー",
		Name = "ヘッダー行背景色",
		Order = 200
	)]
	[ColorPicker]
	[DefaultValue(typeof(Color), nameof(Colors.LightGray))]
	[ShowPropertyEditorWhen(
		nameof(HeaderDisplay),
		ShowHeader.RowHeader | ShowHeader.BothHeader
	)]
	public Color HeaderRowBackgroundColor
	{
		get => _headerRowBackgroundColor;
		set => Set(ref _headerRowBackgroundColor, value);
	}
	Color _headerRowBackgroundColor = Colors.LightGray;

	[Display(
		GroupName = "ヘッダー",
		Name = "ヘッダー列背景色",
		Order = 200
	)]
	[ColorPicker]
	[DefaultValue(typeof(Color), nameof(Colors.LightGray))]
	[ShowPropertyEditorWhen(
		nameof(HeaderDisplay),
		ShowHeader.ColumnHeader | ShowHeader.BothHeader
	)]
	public Color HeaderColumnBackgroundColor
	{
		get => _headerColumnBackgroundColor;
		set => Set(ref _headerColumnBackgroundColor, value);
	}
	Color _headerColumnBackgroundColor = Colors.LightGray;

	#endregion header

	[Display(AutoGenerateField = true)] //機能しない…
	public TableModel TableModel { get; set; } = new(1, 1);

	//TODO: 独自UIに置き換える
	[Display(AutoGenerateField = true)]
	public ImmutableList<Models.TableCell> Cells =>
		[.. TableModel.Cells.SelectMany(c => c)];

	public TableShapeParameter()
		: this(null)
	{
	}

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
		Debug.WriteLine(
			$"GetAnimatables[{nameof(TableShapeParameter)}]"
		);
		return
		[
			Width,
			Height,
			BorderWidth,
			OuterBorderWidth,
			RowCount,
			ColumnCount,
			TableModel,
			.. Cells,
			//.. TableModel.RowBoundaries,
			//.. TableModel.ColumnBoundaries,
		];
	}
}
