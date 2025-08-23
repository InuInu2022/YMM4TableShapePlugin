using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YMM4TableShapePlugin.Models;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[Flags]
public enum CellTextStyle
{
	[Display(Name = "なし")]
	Normal = 1,

	[Display(Name = "鋭角枠線")]
	ShapedBorder = 4,

	[Display(Name = "角丸枠線")]
	RoundedBorder = 8,

	// 追加例
	//[Display(Name = "影")]
	//Shadow = 16
}
