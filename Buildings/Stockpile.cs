using BuildingsExpanded.Framework;
using BuildingsExpanded.UI;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StarModGen.Lib;

namespace BuildingsExpanded.Buildings;

public class Stockpile
{
	[ModEvent]
	internal static void Init(object? s, SetupEventArgs ev)
	{
		GameLocation.RegisterTileAction(MOD_ID + "_Stockpile", DoAction);
		ev.Harmony
			.With<Building>(nameof(Building.BeforeDemolish)).Postfix(DropThingsOnFloor);
	}

	public static bool DoAction(GameLocation where, string[] args, Farmer who, Point tile)
	{
		var building = where.getBuildingAt(tile.ToVector2());
		if (building is null || building.isUnderConstruction())
			return false;

		OpenStockpile(building);
		return true;
	}

	public static void DropThingsOnFloor(Building __instance)
	{
		if (!__instance.TryGetCustomField(MOD_ID + "_StockpileCapacity", out _))
			return;

		var where = __instance.GetParentLocation();
		var pos = __instance.GetBounds().Center.ToVector2() * 64f;

		if (__instance.GetBuildingChest("storage") is Chest c)
		{
			foreach (var item in c.Items)
			{
				var i = item.getOne();
				i.Stack = item.Stack;
				Game1.createItemDebris(i, pos, Game1.random.Next(4), where);
			}
		}
	}

	public static void OpenStockpile(Building b)
	{
		if (b.GetBuildingChest("storage") is not Chest chest)
			Print.Warn($"Storage missing for stockpile at {b.tileX.Value}, {b.tileY.Value} in {b.parentLocationName.Value}");

		else if (!b.TryGetCustomField(MOD_ID + "_StockpileCapacity", out var val))
			Print.Warn($"Missing capacity on stockpile at {b.tileX.Value}, {b.tileY.Value} in {b.parentLocationName.Value}");

		else if (!int.TryParse(val, out int capacity) || capacity <= 0)
			Print.Warn($"Invalid capacity detected on stockpile at {b.tileX.Value}, {b.tileY.Value} in {b.parentLocationName.Value}");

		else
			Game1.activeClickableMenu = new StockpileMenu(chest, capacity);
	}
}
