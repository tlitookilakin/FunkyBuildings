using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StarModGen.Lib;

namespace FunkyBuildings.Integration
{
	public interface IBetterBeehouses
	{
		public static IBetterBeehouses? API = null;
		private static IModHelper helper = null!;

		[ModEvent]
		internal static void Init(object? s, SetupEventArgs ev)
		{
			helper = ev.Helper;
		}

		[ModEvent]
		internal static void Startup(object? s, GameLaunchedEventArgs ev)
		{
			if (helper.ModRegistry.IsLoaded("tlitookilakin.BetterBeehouses"))
				API = helper.ModRegistry.GetApi<IBetterBeehouses>("tlitookilakin.BetterBeehouses");
		}

		/// <summary>
		/// Get honey IDs for a set of tiles. Order of input tiles determines search order.
		/// </summary>
		/// <param name="where">The location to search in</param>
		/// <param name="tiles">The set of tiles to search</param>
		/// <returns>The item IDs of the found items and their tile locations</returns>
		public IEnumerable<KeyValuePair<Vector2, string>> GetAllHoneySources(GameLocation where, IEnumerable<Vector2> tiles);
	}
}
