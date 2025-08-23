using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YMM4TableShapePlugin.Enums;

/// <summary>
/// セルのスタイルを共通に従うか、個別に上書きするか
/// </summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
[Flags]
public enum CellStylePriority
{
	[Display(Name = "テーブル共通に従う")]
	Inherit = 1,

	[Display(Name = "個別に上書きする")]
	Override = 2,
}
