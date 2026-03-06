using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
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

		public static void Draw(this TemporaryAnimatedSpriteList list, SpriteBatch b, GameTime time, bool local = false)
		{
            for (int i = list.Count - 1; i >= 0; i--)
            {
				if (list[i].update(time))
					list.RemoveAt(i);
				else
					list[i].draw(b, local);
            }
        }
	}
}
