using StardewValley;
using StardewValley.Delegates;
using StarModGen.Lib;
using System.Reflection;
using static StardewValley.GameStateQuery;

namespace FunkyBuildings.Framework
{
	internal static class GSQ
	{
		[ModEvent]
		internal static void Init(object? s, SetupEventArgs ev)
		{
			foreach (var method in typeof(GSQ).GetMethods(BindingFlags.Public | BindingFlags.Static))
			{
				string id = MOD_ID + '_' + method.Name;
				var query = method.CreateDelegate<GameStateQueryDelegate>();
				if (query is not null)
				{
					Register(id, query);
					Print.Trace($"Registered game state query {id}.");
				}
				else
				{
					Print.Warn($"Failed to register game state query {id}.");
				}
			}
		}

		public static bool RAIN_TOTEM_ALLOWED(string[] args, GameStateQueryContext ctx)
		{
			GameLocation location = ctx.Location;
			if (!Helpers.TryGetLocationArg(args, 0, ref location, out var err))
				return Helpers.ErrorResult(args, err);

			return location.GetLocationContext().AllowRainTotem;
		}
	}
}
