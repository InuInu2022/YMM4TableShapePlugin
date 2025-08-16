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
public record TableCell
{
	public string Text { get; set; } = "";
	public required int Row { get; init; }
	public required int Col { get; init; }
	public int RowSpan { get; set; } = 1;
	public int ColSpan { get; set; } = 1;

	// 結合セルの代表（親）を指す
	public TableCell? ParentCell { get; set; }

	public bool IsMergedChild => ParentCell is not null;
	public bool IsMergeRoot => RowSpan > 1 || ColSpan > 1;
}
