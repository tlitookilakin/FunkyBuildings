using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StarModGen.Lib;

namespace FunkyBuildings.Buildings;

internal class FieldStone
{
	const int RADIUS = 4;
	const string ID = MOD_ID + "_" + nameof(FieldStone);

	private static readonly Dictionary<GameLocation, List<Rectangle>> LocationBuildingCache = [];

	[ModEvent]
	public static void Init(object? s, SetupEventArgs ev)
	{
		ev.Helper.Events.GameLoop.DayStarted += ClearCache;
		ev.Helper.Events.GameLoop.ReturnedToTitle += ClearCache;

		ev.Harmony
			.With<HoeDirt>(nameof(HoeDirt.GetFertilizerWaterRetentionChance)).Postfix(ModifyWater)
			.With(nameof(HoeDirt.GetFertilizerSpeedBoost)).Postfix(ModifySpeed)
			.With(nameof(HoeDirt.GetFertilizerQualityBoostLevel)).Postfix(ModifyQuality);
	}

	private static float ModifyWater(float original, HoeDirt __instance)
		=> HasStoneInRange(__instance.Location, __instance.Tile) ? 1f : original;

	private static float ModifySpeed(float original, HoeDirt __instance)
		=> HasStoneInRange(__instance.Location, __instance.Tile) ? original + .15f : original;

	private static int ModifyQuality(int original, HoeDirt __instance)
		=> HasStoneInRange(__instance.Location, __instance.Tile) ? original + 1 : original;

	private static void ClearCache(object? s, object e)
		=> LocationBuildingCache.Clear();

	private static bool HasStoneInRange(GameLocation where, Vector2 tile)
	{
		var stones = GetFieldStones(where);
		for (int i = 0; i < stones.Count; i++)
			if (stones[i].Contains(tile))
				return true;
		return false;
	}

	private static List<Rectangle> GetFieldStones(GameLocation where)
	{
		if (LocationBuildingCache.TryGetValue(where, out var items))
			return items;

		items = [];

		foreach (var b in where.buildings)
		{
			if (b.isUnderConstruction())
				continue;

			if (!b.modData.TryGetValue(ID, out var rad))
				continue;

			if (!int.TryParse(rad, out int radius))
			{
				Print.Warn($"Building type '{b.buildingType.Value}' has misconfigured field stone radius (must be integer)");
				continue;
			}

			var bounds = b.GetBounds();
			items.Add(new(bounds.Left - radius, bounds.Top - radius, bounds.Width + radius * 2, bounds.Height + radius * 2));
		}

		LocationBuildingCache[where] = items;
		return items;
	}
}
