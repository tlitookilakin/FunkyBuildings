using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.TokenizableStrings;
using System.Diagnostics.CodeAnalysis;

namespace BuildingsExpanded.Framework
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

		public static string Parse(this string text, Farmer? who = null)
			=> TokenParser.ParseText(text, player: who ?? Game1.player);

		public static Point NextPoint(this Random rand, Rectangle region)
		{
			return new(rand.Next(region.Width) + region.X, rand.Next(region.Height) + region.Y);
		}

		public static Effect LoadEffect(string path)
		{
			byte[] raw =
			#if DEBUG
			File.ReadAllBytes(Path.Join(HotReload.FolderPath, path));
			#else
			typeof(ModUtilities).Assembly.GetManifestResourceStream(path.Replace('/', '.').Replace('\\', '.')).ReadAllBytes();
			#endif

			return new(Game1.graphics.GraphicsDevice, raw);
		}

		public static byte[] ReadAllBytes(this Stream? s)
		{
			if (s is null)
				return [];

			using var reader = new BinaryReader(s);
			return reader.ReadBytes((int)s.Length);
		}

		public static Vector2 ToVector(this xTile.Dimensions.Location loc)
			=> new(loc.X, loc.Y);

		public static bool TryGetCustomField(this Building b, string key, [NotNullWhen(true)] out string? value)
		{
			return TryGetCustomField(b, b?.GetData(), key, out value);
		}

		public static bool TryGetCustomField(this Building b, BuildingData? data, string key, [NotNullWhen(true)] out string? value)
		{
			value = null;

			if (data is null)
				return false;

			if (data.CustomFields is not Dictionary<string, string> fields)
				return false;

			return fields.TryGetValue(key, out value);

		}
	}
}
