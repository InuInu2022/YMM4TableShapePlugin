using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;

using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Resources.Localization;

namespace YMM4TableShapePlugin.Models;

/// <summary>
/// テーブル内のセルを表します。位置、内容、結合情報を含みます。
/// </summary>
/// <remarks>
/// <see cref="TableCell"/> は、結合セルグループの親（ルート）または子として扱われる場合があります。
/// </remarks>
/// <property name="Text">セルのテキスト内容。</property>
/// <property name="Row">セルのゼロ始まりの行インデックス。</property>
/// <property name="Col">セルのゼロ始まりの列インデックス。</property>
/// <property name="RowSpan">このセルがまたぐ行数。デフォルトは1。</property>
/// <property name="ColSpan">このセルがまたぐ列数。デフォルトは1。</property>
/// <property name="ParentCell">
/// このセルが結合セルの子の場合、親（ルート）セルへの参照。そうでなければ <c>null</c>。
/// </property>
/// <property name="IsMergedChild">
/// このセルが結合セルグループの子であるかどうかを示す値。
/// </property>
/// <property name="IsMergeRoot">
/// このセルが結合セルグループのルート（複数行または複数列にまたがる）であるかどうかを示す値。
/// </property>
public sealed class TableCell : Animatable, ICloneable
{
	[Display(GroupName = "セル", Name = "テキスト")]
	/*[TextEditor(
		AcceptsReturn = true,
		PropertyEditorSize = PropertyEditorSize.Normal
	)]*/
	[RichTextEditor(
		DecorationPropertyName = "Decorations",
		FontPropertyName = "Font",
		ForegroundPropertyName = "FontColor"
	)]
	public string Text
	{
		get => _text;
		set
		{
			//BeginEdit();
			Set(
				ref _text,
				value,
				"Text",
				"Label",
				"Description"
			);
			//EndEditAsync().AsTask().Wait();
		}
	}
	string _text = string.Empty;

	public ImmutableList<TextDecoration> Decorations
	{
		get { return _decorations; }
		set { Set(ref _decorations, value, "Decorations"); }
	}
	ImmutableList<TextDecoration> _decorations = [];

	[Display(
		GroupName = "セル",
		Name = "フォント",
		Description = ""
	)]
	[FontComboBox]
	public string Font
	{
		get => _font;
		set => Set(ref _font, value);
	}
	string _font = "Yu Gothic UI";

	[Display(
		GroupName = "セル",
		Name = "文字色",
		Description = ""
	)]
	[ColorPicker]
	public Color FontColor
	{
		get => _fontColor;
		set => Set(ref _fontColor, value);
	}
	private Color _fontColor = Colors.Black;

	[Display(
		GroupName = "セル",
		Name = "サイズ",
		Description = ""
	)]
	[AnimationSlider("F1", "", 1, 50)]
	[DefaultValue(34)]
	[Range(1.0, 50)]
	public Animation FontSize { get; set; } =
		new Animation(34, 1, 1000);

	public required int Row { get; init; }
	public required int Col { get; init; }
	public int RowSpan { get; set; } = 1;
	public int ColSpan { get; set; } = 1;

	// 結合セルの代表（親）を指す
	public TableCell? ParentCell { get; set; }

	public bool IsMergedChild => ParentCell is not null;
	public bool IsMergeRoot => RowSpan > 1 || ColSpan > 1;

	protected override IEnumerable<IAnimatable> GetAnimatables()
	{
		return [FontSize];
	}

	public object Clone()
	{
		return new TableCell()
		{
			Col = Col,
			Row = Row,
			ColSpan = ColSpan,
			RowSpan = RowSpan,
			ParentCell = ParentCell?.Clone() as TableCell,
			Font = Font,
			FontColor = FontColor,
			Text = Text,
		};
	}
}
