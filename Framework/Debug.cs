using FunkyBuildings.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;
using System.Diagnostics;

namespace FunkyBuildings.Framework
{
	internal static class Debug
	{
		[Conditional("DEBUG")]
		public static void Init(IModHelper help)
		{
			#if DEBUG

			helper = help;
			help.ConsoleCommands.Add("fb_debug", "debug command", DoDebug);
            HotReload.SourceFileChanged += SourceChanged;
            HotReload.FileUpdated += FileUpdated; ;

			#endif
		}

#if DEBUG

        private static readonly Chest chest = new();
		private static IModHelper helper = null!;

		private static void DoDebug(string cmd, string[] args)
		{
			Game1.activeClickableMenu = new StockpileMenu(chest, 120);
			var test = Assets.assets.StoneGlow;
		}

		private static void SourceChanged(string file)
		{
			if (Path.GetExtension(file).EqualsIgnoreCase(".fx"))
			{
				var full = Path.Join(HotReload.SourceFolder, file);
				Process.Start("mgfxc", $"{full} {Path.ChangeExtension(full, ".mgfx")} /Profile:OpenGL");
			}
		}

		private static void FileUpdated(string file)
		{
			if (file.EqualsIgnoreCase("assets/buildings.json"))
				helper.GameContent.InvalidateCache("Data/Buildings");
			else if (Path.GetExtension(file).EqualsIgnoreCase(".mgfx"))
				Assets.assets.ReloadShaders();
		}
#endif

	}
}
