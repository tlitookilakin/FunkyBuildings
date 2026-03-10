using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StarModGen.Lib;
using xTile;
using xTile.Layers;

namespace FunkyBuildings.Buildings;

internal class VanillaIslandBuildings
{
	private static IModHelper helper = null!;

	[ModEvent]
	public static void Init(object? s, SetupEventArgs ev)
	{
		helper = ev.Helper;

		ev.Harmony
			.With<IslandWest>(nameof(IslandWest.ApplyFarmHouseRestore)).Postfix(ApplyFarmhouse)
			.With(nameof(IslandWest.ApplyFarmObeliskBuild)).Postfix(ApplyObelisk);
	}

	private static void ApplyFarmhouse(IslandWest __instance, NetHashSet<string> ____appliedMapOverrides)
	{
		if (____appliedMapOverrides.Contains("Island_House_Restored"))
		{
			DemolishMapBuilding(new(74, 37, 7, 5), 9, __instance);
			DemolishMapBuilding(new(__instance.shippingBinPosition, new(2, 1)), 2, __instance);
			__instance.AddDefaultBuilding(MOD_ID + "_IslandFarmhouse", new(74, 37));
		}

		if (!__instance.modData.ContainsKey(MOD_ID + "_AddedShippingBin"))
		{
			__instance.modData[MOD_ID + "_AddedShippingBin"] = "T";
			__instance.AddDefaultBuilding("ShippingBin", __instance.shippingBinPosition.ToVector2());
		}

		if (__instance.farmhouseMailbox.Value)
		{
			DemolishMapBuilding(new(81, 40, 1, 1), 2, __instance);
			__instance.AddDefaultBuilding(MOD_ID + "_Mailbox", new(81, 40));
		}
	}

	private static void ApplyObelisk(IslandWest __instance, NetHashSet<string> ____appliedMapOverrides)
	{
		if (____appliedMapOverrides.Contains("Island_W_Obelisk"))
		{
			DemolishMapBuilding(new(71, 36, 3, 2), 9, __instance);
			__instance.AddDefaultBuilding(MOD_ID + "_IslandFarmObelisk", new(71, 36));
		}
	}

	private static void DemolishMapBuilding(Rectangle foot, int height, GameLocation where)
	{
		if (where?.Map is not Map map)
			return;

		for (int y = 0; y < height; y++)
		{
			int tileY = foot.Bottom - y - 1;
			string layerName = y < foot.Height ? "Buildings" : y > foot.Height ? "AlwaysFront" : "Front";
			EraseLine(map.GetLayer(layerName), foot.X, tileY, foot.Width);
		}
	}

	private static void EraseLine(Layer layer, int x, int y, int width)
	{
		for (int i = 0; i < width; i++)
			layer.Tiles[x + i, y] = null;
	}

	[ModEvent]
	internal static void HandleAsset(object? s, AssetRequestedEventArgs e)
	{
		if (e.NameWithoutLocale.IsEquivalentTo("Maps/Island_House_Restored"))
			e.Edit(EditIslandFarm, AssetEditPriority.Late);
	}

	private static void EditIslandFarm(IAssetData data)
	{
		var map = data.AsMap();
		map.PatchMap(helper.ModContent.Load<Map>("assets/maps/island_house_patch.tmx"), targetArea: new(0, 8, 7, 2), patchMode: PatchMapMode.ReplaceByLayer);
	}
}
