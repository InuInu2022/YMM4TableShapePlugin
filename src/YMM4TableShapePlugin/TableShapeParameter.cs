using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows.Media;
using YMM4TableShapePlugin.Enums;
using YMM4TableShapePlugin.Models;
using YMM4TableShapePlugin.View;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor.CustomVisibilityAttributes;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YMM4TableShapePlugin;

internal class TableShapeParameter : ShapeParameterBase
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
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color BorderColor
	{
		get => _borderColor;
		set => Set(ref _borderColor, value);
	}
	private Color _borderColor = Colors.Black;

	[Display(GroupName = "外観", Name = "外枠の色")]
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color OuterBorderColor
	{
		get => _outerBorderColor;
		set => Set(ref _outerBorderColor, value);
	}
	private Color _outerBorderColor = Colors.White;

	[Display(GroupName = "外観", Name = "背景色")]
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color BackgroundColor
	{
		get => _backgroundColor;
		set => Set(ref _backgroundColor, value);
	}

	private Color _backgroundColor = Colors.WhiteSmoke;

	#region common_cell_style	//-------------------------------//

	[Display(
		GroupName = "共通セルスタイル",
		Name = "フォント"
	)]
	[FontComboBox]
	public string CellFont
	{
		get => _font;
		set => Set(ref _font, value);
	}
	string _font = "Yu Gothic UI";

	[Display(
		GroupName = "共通セルスタイル",
		Name = "サイズ",
		Description = ""
	)]
	[AnimationSlider("F1", "", 1, 50)]
	[DefaultValue(34)]
	[Range(1.0, 50)]
	public Animation CellFontSize { get; set; } =
		new Animation(34, 1, 1000);

	[Display(
		GroupName = "共通セルスタイル",
		Name = "文字色"
	)]
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color CellFontColor
	{
		get => _fontColor;
		set => Set(ref _fontColor, value);
	}
	Color _fontColor = Colors.Black;

	[Display(
		GroupName = "共通セルスタイル",
		Name = "装飾色"
	)]
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color CellFontOutlineColor
	{
		get => _fontOutlineColor;
		set => Set(ref _fontOutlineColor, value);
	}
	Color _fontOutlineColor = Colors.White;

	[Display(
		GroupName = "共通セルスタイル",
		Name = "文字装飾"
	)]
	[EnumComboBox]
	public CellTextStyle CellTextStyle
	{
		get => _cellTextStyle;
		set => Set(ref _cellTextStyle, value);
	}
	CellTextStyle _cellTextStyle = CellTextStyle.Normal;

	[Display(
		GroupName = "共通セルスタイル",
		Name = "テキストの配置",
		Description = ""
	)]
	[EnumComboBox]
	public CellContentAlign CellTextAlign
	{
		get => _textAlign;
		set { Set(ref _textAlign, value); }
	}
	CellContentAlign _textAlign =
		CellContentAlign.MiddleCenter;

	[Display(
		GroupName = "共通セルスタイル",
		Name = "太字",
		Description = ""
	)]
	[ToggleSlider]
	public bool IsCellFontBold
	{
		get => _isFontBold;
		set { Set(ref _isFontBold, value); }
	}
	bool _isFontBold;

	[Display(
		GroupName = "共通セルスタイル",
		Name = "イタリック",
		Description = ""
	)]
	[ToggleSlider]
	public bool IsCellFontItalic
	{
		get => _isFontItalic;
		set { Set(ref _isFontItalic, value); }
	}
	bool _isFontItalic;

	#endregion common_cell_style

	#region header	//------------------------------------------//

	[Display(GroupName = "ヘッダー", Name = "ヘッダー強調")]
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
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
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
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
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

	[Display(GroupName = "", Name = "")]
	[TableShapeEditor(
		PropertyEditorSize = PropertyEditorSize.FullWidth
	)]
	public TableModel TableModel { get; set; } = new(1, 1);

	[Display(
		GroupName = "高度な設定",
		Name = "セルリストの表示",
		Description = ""
	)]
	[ToggleSlider]
	public bool IsShowCellList
	{
		get => _isShowCellList;
		set { Set(ref _isShowCellList, value); }
	}
	bool _isShowCellList;

	[Display(
		GroupName = "高度な設定",
		Name = "セルリスト",
		AutoGenerateField = true
	)]
	[ShowPropertyEditorWhen(nameof(IsShowCellList), false)]
	public ImmutableList<Models.TableCell> Cells =>
		[.. TableModel.Cells.SelectMany(c => c)];

	public TableShapeParameter(SharedDataStore? sharedData)
		: base(sharedData)
	{
		RowCount.PropertyChanged += (_, e) =>
		{
			Debug.WriteLine(
				"this.GetHashCode():"
					+ this.GetHashCode()
			);
			OnPropertyChanged(nameof(RowCount));
		};
		ColumnCount.PropertyChanged += (_, e) =>
		{
			Debug.WriteLine(
				"this.GetHashCode():" + this.GetHashCode()
			);
			OnPropertyChanged(nameof(ColumnCount));
		};
	}

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
			CellFontSize,
			.. Cells,
			//.. TableModel.RowBoundaries,
			//.. TableModel.ColumnBoundaries,
		];
	}
}
