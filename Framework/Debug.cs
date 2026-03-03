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
		}

		private static void DoDebug(string cmd, string[] args)
		{
			Game1.activeClickableMenu = new StockpileMenu(chest, 120);
		}
	}
}
