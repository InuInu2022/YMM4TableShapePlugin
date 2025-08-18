using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YMM4TableShapePlugin.Models;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[System.Flags]
public enum ShowHeader
{
	[Display(Name = "なし")]
	None = 0,

	[Display(Name = "行ヘッダー")]
	RowHeader,

	[Display(Name = "列ヘッダー")]
	ColumnHeader,

	[Display(Name = "両方ヘッダー")]
	BothHeader,
}
