using FunkyBuildings.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Objects;
using StarModGen.Lib;
using System.Diagnostics;

namespace FunkyBuildings.Framework
{
	internal static class Debug
	{
		[ModEvent]
		public static void Init(object? s, SetupEventArgs e)
		{
			#if DEBUG

			helper = e.Helper;
			helper.ConsoleCommands.Add("fb_debug", "debug command", DoDebug);
			helper.ConsoleCommands.Add("fb_construct", "show free construction menu", ShowDebugConstruct);
            HotReload.SourceFileChanged += SourceChanged;
            HotReload.FileUpdated += FileUpdated;
			e.Harmony
				.With<CarpenterMenu>(nameof(CarpenterMenu.DoesFarmerHaveEnoughResourcesToBuild)).Postfix(ForceFree);

			#endif
		}

#if DEBUG

        private static readonly Chest chest = new();
		private static IModHelper helper = null!;
		private static bool ShouldBeFree = false;

		private static bool ForceFree(bool result)
			=> result || ShouldBeFree;

		private static void DoDebug(string cmd, string[] args)
		{
			//Game1.activeClickableMenu = new StockpileMenu(chest, 120);
			Game1.MasterPlayer.mailReceived.Remove(MOD_ID + "_IslandHouseUpgrade");
		}

		private static void ShowDebugConstruct(string cmd, string[] args)
		{
			if (args.Length == 0)
				return;

			ShouldBeFree = true;
			var menu = new CarpenterMenu(args[0], Game1.currentLocation);
			menu.behaviorBeforeCleanup += (s) => ShouldBeFree = false;
			Game1.activeClickableMenu = menu;
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
			if (file.EqualsIgnoreCase(Path.Join("assets", "buildings.json")))
				helper.GameContent.InvalidateCache("Data/Buildings");
			else if (Path.GetExtension(file).EqualsIgnoreCase(".mgfx"))
				Assets.assets.ReloadShaders();
		}
#endif

	}
}
