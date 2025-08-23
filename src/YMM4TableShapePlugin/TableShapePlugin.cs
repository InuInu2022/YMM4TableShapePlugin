using System.Reflection;
using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YMM4TableShapePlugin;

[PluginDetails(AuthorName = "InuInu", ContentId = "")]
internal class TableShapePlugin : IShapePlugin
{
	public string Name => "表テーブル";
	public PluginDetailsAttribute Details =>
		GetType()
			.GetCustomAttribute<PluginDetailsAttribute>()
		?? new();
	public bool IsExoShapeSupported => false;
	public bool IsExoMaskSupported => false;

	public IShapeParameter CreateShapeParameter(
		SharedDataStore? sharedData
	)
	{
		return new TableShapeParameter(sharedData);
	}
}
