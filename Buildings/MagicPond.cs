using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Tools;
using StarModGen.Lib;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_MagicPond")]
public class MagicPond : Building
{
	const string ID = MOD_ID + "_" + nameof(MagicPond);
	private static bool ForceMagicBait = false;

	public MagicPond(Vector2 tile) : base(ID, tile) { }
	public MagicPond() : base() { }

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<GameLocation>(nameof(GameLocation.getFish)).Prefix(DoMagicPondCatch)
			.With<FishingRod>(nameof(FishingRod.HasMagicBait)).Postfix(OverrideMagicBait);
	}

	public override void Update(GameTime time)
	{
		base.Update(time);
		if (Game1.currentLocation.NameOrUniqueName != parentLocationName.Value)
			return;

		//TODO add sparkles :3
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		DrawWater(b, this.GetBounds());
	}

    public override void drawInMenu(SpriteBatch b, int x, int y)
    {
        base.drawInMenu(b, x, y);
    }

	private static bool OverrideMagicBait(bool hasBait, FishingRod __instance)
		=> hasBait || ForceMagicBait;

	private static bool DoMagicPondCatch(ref Item __result, int waterDepth, Vector2 bobberTile, GameLocation __instance, Farmer who)
	{
		if (who.currentLocation?.NameOrUniqueName != __instance.NameOrUniqueName)
			return true;

		foreach (var building in __instance.buildings)
		{
			if (building is not MagicPond || building.isUnderConstruction())
				continue;

			if (!building.isTileFishable(bobberTile))
				continue;

			var fishablePlaces = DataLoader.Locations(Game1.content)
			.Where(static l => l.Value.Fish is List<SpawnFishData> fish && fish.Count != 0 && Game1.player.locationsVisited.Contains(l.Key))
			.Select(static l => l.Key)
			.ToList();

			var where = Game1.random.ChooseFrom(fishablePlaces);
			ForceMagicBait = true;
			__result = GameLocation.GetFishFromLocationData(where, bobberTile, waterDepth, Game1.player, false, false) ?? ItemRegistry.Create("(O)168");
			ForceMagicBait = false;

			return false;
		}

		return true;
	}

    public override bool isTileFishable(Vector2 tile)
    {
		var bounds = this.GetBounds();
		bounds.Inflate(-1, -1);
		return bounds.Contains(tile.X, tile.Y);
    }

	private static void DrawWater(SpriteBatch b, Rectangle bounds)
	{
		int right = bounds.Width - 1;
		int bottom = bounds.Height - 1;

		var where = Game1.currentLocation;
		var color = Utility.GetPrismaticColor();

		for (int y = bounds.Y; y <= bottom; y++)
		{
			float yOffset = -where.waterPosition;
			int height = 64;

			if (y == bounds.Y)
			{
				yOffset = 0;
				height = 64 - (int)where.waterPosition;
			}
			else if (y == bottom)
			{
				height = (int)where.waterPosition;
			}

			for (int x = bounds.X; x < right; x++)
			{
				b.Draw(
					Game1.mouseCursors, 
					Game1.GlobalToLocal(new(x * 64 + 32, y * 64 + yOffset)),
					new Rectangle(
						where.waterAnimationIndex * 64, 
						2064 + (((x + y) % 2 == 0) ^ Game1.currentLocation.waterTileFlip ? 0 : 128),
						64, height
					),
					color * .5f, 0f, Vector2.Zero, 1f, SpriteEffects.None,
					((bounds.Y + .5f) * 64 - 2f) / 10_000f
				);
			}
		}
	}
}
