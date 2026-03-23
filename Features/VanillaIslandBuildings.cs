using BuildingsExpanded.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StarModGen.Lib;
using System.Reflection.Emit;
using xTile;
using xTile.Layers;

namespace BuildingsExpanded.Features;

internal class VanillaIslandBuildings
{
	private static IModHelper helper = null!;

	[ModEvent]
	public static void Init(object? _, SetupEventArgs ev)
	{
		helper = ev.Helper;

		ev.Harmony
			.With<IslandWest>(nameof(IslandWest.ApplyFarmHouseRestore)).Postfix(ApplyFarmhouse)
			.With(nameof(IslandWest.ApplyFarmObeliskBuild)).Postfix(ApplyObelisk)
			.With(nameof(IslandWest.draw)).Transpiler(SkipBoxes)
			.With("openShippingBinLid").Prefix(SkipShippingBin);
	}

	private static void ApplyFarmhouse(IslandWest __instance, HashSet<string> ____appliedMapOverrides)
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
			__instance.AddDefaultBuilding("Shipping Bin", __instance.shippingBinPosition.ToVector2());
		}

		if (__instance.farmhouseMailbox.Value)
		{
			DemolishMapBuilding(new(81, 40, 1, 1), 2, __instance); 
			if (!__instance.modData.ContainsKey(MOD_ID + "_AddedMailbox"))
			{
				__instance.modData[MOD_ID + "_AddedMailbox"] = "T";
				__instance.AddDefaultBuilding(MOD_ID + "_Mailbox", new(81, 40));
			}
		}
	}

	private static void ApplyObelisk(IslandWest __instance, HashSet<string> ____appliedMapOverrides)
	{
		if (____appliedMapOverrides.Contains("Island_W_Obelisk"))
		{
			DemolishMapBuilding(new(71, 35, 3, 3), 9, __instance);
			if (!__instance.modData.ContainsKey(MOD_ID + "_AddedIslandObelisk"))
			{
				__instance.modData[MOD_ID + "_AddedIslandObelisk"] = "T";
				__instance.AddDefaultBuilding(MOD_ID + "_IslandFarmObelisk", new(71, 36));
			}
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
		map.PatchMap(helper.ModContent.Load<Map>("assets/maps/island_house_patch.tmx"), targetArea: new(0, 7, 7, 2), patchMode: PatchMapMode.ReplaceByLayer);
	}

	private static bool SkipShippingBin()
	{
		return false;
	}

	private static IEnumerable<CodeInstruction> SkipBoxes(IEnumerable<CodeInstruction> src, ILGenerator gen)
	{
		var il = new CodeMatcher(src, gen);
		var skip = gen.DefineLabel();

		il
			.MatchEndForward(
				new CodeMatch(OpCodes.Callvirt, typeof(SandDuggy).GetMethod(nameof(SandDuggy.Draw)))
			).Advance(1);

		Label[] labels = [.. il.Labels];
		il.Labels = [];

		il
			.InsertAndAdvance(
				new CodeInstruction(OpCodes.Br, skip).WithLabels(labels)
			)
			.MatchStartForward(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldarg_1),
				new(OpCodes.Call),
				new(OpCodes.Ret)
			).AddLabels([skip]);

		return il.InstructionEnumeration();
	}
}
