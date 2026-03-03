using FunkyBuildings.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Diagnostics;

namespace FunkyBuildings.Framework
{
	internal static class Debug
	{
		private static Chest chest = new();

		[Conditional("DEBUG")]
		public static void Init(IModHelper help)
		{
			help.ConsoleCommands.Add("fb_debug", "debug command", DoDebug);
			help.ConsoleCommands.Add("fb_parrot_build", "open parrot build menu", ParrotBuild);
		}

		private static void DoDebug(string cmd, string[] args)
		{
			Game1.activeClickableMenu = new StockpileMenu(chest, 120);
		}

		private static void ParrotBuild(string cmd, string[] args)
		{
			Game1.currentLocation?.ShowConstructOptions("FB_Parrot");
		}
	}
}
