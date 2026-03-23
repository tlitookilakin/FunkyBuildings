using BuildingsExpanded.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StarModGen.Lib;

namespace BuildingsExpanded.Features;

internal class RequiredBuildings
{
	[ModEvent]
	internal static void Init(object? _, SetupEventArgs e)
	{
		e.Harmony
			.WithAll<CarpenterMenu>(nameof(CarpenterMenu.CanDemolishThis), m => m.GetParameters().Length != 0).Postfix(OverrideDemolish)
			.With(nameof(CarpenterMenu.hasPermissionsToMove)).Postfix(OverrideMove);
	}

	private static bool OverrideDemolish(bool original, Building building)
	{
		if (!original)
			return false;

		if (building.TryGetCustomField(MOD_ID + "_NoDemolish", out var c))
			return c.Length is not 0 && !GameStateQuery.CheckConditions(c, building.GetParentLocation());

		return true;
	}

	private static bool OverrideMove(bool original, Building b)
	{
		if (!original)
			return false;

		if (b.TryGetCustomField(MOD_ID + "_NoMove", out var c))
			return c.Length is not 0 && !GameStateQuery.CheckConditions(c, b.GetParentLocation());

		return true;
	}
}
