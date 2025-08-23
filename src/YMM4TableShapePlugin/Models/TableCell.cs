using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

using Vortice.DirectWrite;

using YMM4TableShapePlugin.Enums;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.ItemEditor.CustomVisibilityAttributes;
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
public sealed class TableCell
	: Animatable,
		ICloneable,
		IEquatable<TableCell>
{
	[Display(GroupName = "セル", Name = "テキスト")]
	[TextEditor(
		AcceptsReturn = true,
		PropertyEditorSize = PropertyEditorSize.Normal
	)]
	/*
	[RichTextEditor(
		DecorationPropertyName = "Decorations",
		FontPropertyName = "Font",
		ForegroundPropertyName = "FontColor"
	)]*/
	public string Text
	{
		get => _text;
		set
		{
			BeginEdit();
			Set(
				ref _text,
				value,
				"Text",
				"Label",
				"Description"
			);
			EndEditAsync().AsTask().Wait();
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
		Name = "セルスタイル",
		Description = "テーブル共通のスタイルに従うか個別に指定するかを選びます。"
	)]
	[EnumComboBox]
	public CellStylePriority StylePriority
	{
		get => _stylePriority;
		set { Set(ref _stylePriority, value); }
	}
	CellStylePriority _stylePriority =
		CellStylePriority.Inherit;

	[Display(
		GroupName = "セル",
		Name = "フォント",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[FontComboBox]
	public string Font
	{
		get => _font;
		set
		{
			BeginEdit();
			Set(ref _font, value);
			EndEditAsync().AsTask().Wait();
		}
	}
	string _font = "Yu Gothic UI";

	[Display(
		GroupName = "セル",
		Name = "文字色",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color FontColor
	{
		get => _fontColor;
		set
		{
			BeginEdit();
			Set(ref _fontColor, value);
			EndEditAsync().AsTask().Wait();
		}
	}
	private Color _fontColor = Colors.Black;

	[Display(
		GroupName = "セル",
		Name = "装飾色",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[ColorPicker(
		PropertyEditorSize = PropertyEditorSize.Half
	)]
	public Color FontOutlineColor
	{
		get => _fontOutlineColor;
		set { Set(ref _fontOutlineColor, value); }
	}
	private Color _fontOutlineColor = Colors.White;

	[Display(
		GroupName = "セル",
		Name = "文字装飾",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[EnumComboBox]
	public CellTextStyle TextStyle
	{
		get => _cellTextStyle;
		set { Set(ref _cellTextStyle, value); }
	}
	CellTextStyle _cellTextStyle = CellTextStyle.Normal;

	private int _rowSpan = 1;
	private int _colSpan = 1;


	[Display(
		GroupName = "セル",
		Name = "サイズ",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[AnimationSlider("F1", "", 1, 50)]
	[DefaultValue(34)]
	[Range(1.0, 50)]
	public Animation FontSize { get; set; } =
		new Animation(34, 1, 1000);

	[Display(
		GroupName = "セル",
		Name = "太字",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[ToggleSlider]
	public bool IsFontBold
	{
		get => isFontBold;
		set { Set(ref isFontBold, value); }
	}
	bool isFontBold;

	[Display(
		GroupName = "セル",
		Name = "イタリック",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[ToggleSlider]
	public bool IsFontItalic
	{
		get => isFontItalic;
		set { Set(ref isFontItalic, value); }
	}
	bool isFontItalic;



	public required int Row { get; init; }
	public required int Col { get; init; }
	public int RowSpan
	{
		get { return _rowSpan; }
		set
		{
			BeginEdit();
			Set(ref _rowSpan, value);
			EndEditAsync().AsTask().Wait();
		}
	}
	public int ColSpan
	{
		get { return _colSpan; }
		set
		{
			BeginEdit();
			Set(ref _colSpan, value);
			EndEditAsync().AsTask().Wait();
		}
	}

	[Display(
		GroupName = "セル",
		Name = "テキストの配置",
		Description = ""
	)]
	[ShowPropertyEditorWhen(
		nameof(StylePriority),
		CellStylePriority.Override
	)]
	[EnumComboBox]
	public CellContentAlign TextAlign
	{
		get => _textAlign;
		set
		{
			BeginEdit();
			Set(ref _textAlign, value);
			EndEditAsync().AsTask().Wait();
		}
	}
	private CellContentAlign _textAlign =
		CellContentAlign.MiddleCenter;

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
			IsFontBold = IsFontBold,
			IsFontItalic = IsFontItalic,
			TextStyle = TextStyle,
			FontOutlineColor = FontOutlineColor,
			StylePriority = StylePriority,
			Text = Text,
		};
	}

	public bool Equals(TableCell? other)
	{
		if (other is null)
		{
			return false;
		}

		return Col == other.Col
			&& Row == other.Row
			&& RowSpan == other.RowSpan
			&& ColSpan == other.ColSpan
			&& ParentCell?.Equals(other.ParentCell) is true
			&& string.Equals(
				Font,
				other.Font,
				StringComparison.OrdinalIgnoreCase
			)
			&& IsFontBold == other.IsFontBold
			&& IsFontItalic == other.IsFontItalic
			&& FontColor == other.FontColor
			&& TextStyle == other.TextStyle
			&& StylePriority == other.StylePriority
			&& FontOutlineColor == other.FontOutlineColor
			&& string.Equals(
				Text,
				other.Text,
				StringComparison.Ordinal
			);
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as TableCell);
	}

	[SuppressMessage(
		"Correctness",
		"SS008:GetHashCode() refers to mutable or static member",
		Justification = "<保留中>"
	)]
	public override int GetHashCode()
	{
		// HashCode.Combineは最大8引数までなのでネストして全プロパティをカバー
		return HashCode.Combine(
			HashCode.Combine(
				Col,
				Row,
				RowSpan,
				ColSpan,
				ParentCell,
				Font,
				FontColor,
				Text
			),
			HashCode.Combine(
				TextStyle,
				FontOutlineColor,
				IsFontBold,
				IsFontItalic,
				StylePriority
			)
		);
	}
}
