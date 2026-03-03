using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Buildings;

namespace FunkyBuildings.Framework
{
	public static class ModUtilities
	{
		public static Vector2 GetCenter(this Building building)
			=> new(
					building.tileX.Value + building.tilesWide.Value / 2,
					building.tileY.Value + building.tilesHigh.Value / 2
				);

		public static Rectangle GetBounds(this Building b)
			=> new(b.tileX.Value, b.tileY.Value, b.tilesWide.Value, b.tilesHigh.Value);
	}
}
