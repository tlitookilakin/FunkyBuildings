using StardewModdingAPI;
using StarModGen.Utils;

namespace FunkyBuildings.Framework
{
	internal class SetupEventArgs(Mod mod, Config cfg)
	{
		public IModHelper Helper => mod.Helper;
		public IMonitor Monitor => mod.Monitor;
		public IManifest Manifest => mod.ModManifest;
		public Config Config => cfg;
		public HarmonyHelper Harmony { get; init; }
			= new(new(mod.ModManifest.UniqueID), mod.Monitor);
	}
}
