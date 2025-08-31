using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YMM4TableShapePlugin.Enums;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[Flags]
public enum AdvancedDisplay
{
	[Display(Name = "なし")]
	None = 1,

	[Display(Name = "表示")]
	Show = 4,
}
