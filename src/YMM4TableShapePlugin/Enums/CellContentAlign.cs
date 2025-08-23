using System.ComponentModel.DataAnnotations;
using System.Reflection;
using YukkuriMovieMaker.Resources.Localization;

namespace YMM4TableShapePlugin.Enums;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public enum CellContentAlign
{
	[Display(
		Name = nameof(Texts.BasePointLeftTopName),
		Description = nameof(Texts.BasePointLeftTopDesc),
		ResourceType = typeof(Texts)
	)]
	TopLeft,

	[Display(
		Name = nameof(Texts.BasePointCenterTopName),
		Description = nameof(Texts.BasePointCenterTopDesc),
		ResourceType = typeof(Texts)
	)]
	TopCenter,

	[Display(
		Name = nameof(Texts.BasePointRightTopName),
		Description = nameof(Texts.BasePointRightTopDesc),
		ResourceType = typeof(Texts)
	)]
	TopRight,

	[Display(
		Name = nameof(Texts.BasePointLeftCenterName),
		Description = nameof(Texts.BasePointLeftCenterDesc),
		ResourceType = typeof(Texts)
	)]
	MiddleLeft,

	[Display(
		Name = nameof(Texts.BasePointCenterCenterName),
		Description = nameof(
			Texts.BasePointCenterCenterDesc
		),
		ResourceType = typeof(Texts)
	)]
	MiddleCenter,

	[Display(
		Name = nameof(Texts.BasePointRightCenterName),
		Description = nameof(
			Texts.BasePointRightCenterDesc
		),
		ResourceType = typeof(Texts)
	)]
	MiddleRight,

	[Display(
		Name = nameof(Texts.BasePointLeftBottomName),
		Description = nameof(Texts.BasePointLeftBottomDesc),
		ResourceType = typeof(Texts)
	)]
	BottomLeft,

	[Display(
		Name = nameof(Texts.BasePointCenterBottomName),
		Description = nameof(
			Texts.BasePointCenterBottomDesc
		),
		ResourceType = typeof(Texts)
	)]
	BottomCenter,

	[Display(
		Name = nameof(Texts.BasePointRightBottomName),
		Description = nameof(
			Texts.BasePointRightBottomDesc
		),
		ResourceType = typeof(Texts)
	)]
	BottomRight,
}
