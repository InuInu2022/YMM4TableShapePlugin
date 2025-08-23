using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YMM4TableShapePlugin.Enums;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum ShowHeader
{
	[Display(Name = "なし")]
	None = 99,

	[Display(Name = "行ヘッダー")]
	RowHeader = 1,

	[Display(Name = "列ヘッダー")]
	ColumnHeader = 2,

	[Display(Name = "両方ヘッダー")]
	BothHeader = 4,
}
