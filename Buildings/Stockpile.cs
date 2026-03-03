using FunkyBuildings.Framework;
using FunkyBuildings.UI;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StarModGen.Lib;

namespace FunkyBuildings.Buildings;

public class Stockpile
{
	[ModEvent]
	internal static void Init(object? s, SetupEventArgs ev)
	{
		GameLocation.RegisterTileAction(MOD_ID + "_Stockpile", DoAction);
	}

	public static bool DoAction(GameLocation where, string[] args, Farmer who, Point tile)
	{
		var building = where.getBuildingAt(tile.ToVector2());
		if (building is null || building.isUnderConstruction())
			return false;

		OpenStockpile(building);
		return true;
	}

	public static void OpenStockpile(Building b)
	{
		if (b.GetBuildingChest("storage") is not Chest chest)
			Print.Warn($"Storage missing for stockpile at {b.tileX.Value}, {b.tileY.Value} in {b.parentLocationName.Value}");

		else if (!Building.TryGetData(b.buildingType.Value, out var data) || !data.CustomFields.TryGetValue("Stockpile_Capacity", out var val))
			Print.Warn($"Missing capacity on stockpile at {b.tileX.Value}, {b.tileY.Value} in {b.parentLocationName.Value}");

		else if (!int.TryParse(val, out int capacity) || capacity <= 0)
			Print.Warn($"Invalid capacity detected on stockpile at {b.tileX.Value}, {b.tileY.Value} in {b.parentLocationName.Value}");

		else
			Game1.activeClickableMenu = new StockpileMenu(chest, capacity);
	}
}
