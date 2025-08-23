using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YMM4TableShapePlugin.Models;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum CellTextStyle
{
	[Display(Name = "なし")]
	Normal = 0,

	[Display(Name = "鋭角枠線")]
	ShapedBorder,

	[Display(Name = "角丸枠線")]
	RoundedBorder,

	//要望があれば影とかも
}
