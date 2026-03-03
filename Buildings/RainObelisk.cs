using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using StardewValley;
using StarModGen.Lib;

namespace FunkyBuildings.Buildings;

// TODO building data entry
public class RainObelisk
{
	[ModEvent]
	internal static void Init(object? s, SetupEventArgs ev)
	{
		GameLocation.RegisterTileAction(MOD_ID + "_ActivateRain", ActivateRain);
	}

	private static bool ActivateRain(GameLocation where, string[] args, Farmer who, Point tile)
	{
		var context = where.GetLocationContext();

		if (!context.AllowRainTotem)
			return false;

		if (!Utility.isFestivalDay(Game1.dayOfMonth + 1, Game1.season))
		{
			Game1.netWorldState.Value.WeatherForTomorrow = Game1.weatherForTomorrow = "Rain";
			Game1.pauseThenMessage(2000, Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12822"));
		}

		var pos = who.Position;

		Game1.screenGlow = false;
		where.playSound("thunder");
		Game1.screenGlowOnce(Color.SlateBlue, hold: false);
		for (int i = 0; i < 6; i++)
		{
			Game1.Multiplayer.broadcastSprites(where, 
				new(
					"LooseSprites\\Cursors", new(648, 1045, 52, 33), 9999f, 1, 999, pos + new Vector2(0f, -128f), 
					flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 2f, 0.01f, 0f, 0f
				)
				{
					motion = new(Game1.random.Next(-10, 11) / 10f, -2f),
					delayBeforeAnimationStart = i * 200
				},
				new(
					"LooseSprites\\Cursors", new(648, 1045, 52, 33), 9999f, 1, 999, pos + new Vector2(0f, -128f), 
					flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f
				)
				{
					motion = new(Game1.random.Next(-30, -10) / 10f, -1f),
					delayBeforeAnimationStart = 100 + i * 200
				},
				new(
					"LooseSprites\\Cursors", new(648, 1045, 52, 33), 9999f, 1, 999, pos + new Vector2(0f, -128f), 
					flicker: false, flipped: false, 1f, 0.01f, Color.White * 0.8f, 1f, 0.01f, 0f, 0f
				)
				{
					motion = new(Game1.random.Next(10, 30) / 10f, -1f),
					delayBeforeAnimationStart = 200 + i * 200
				}
			);
		}
		DelayedAction.playSoundAfterDelay("rainsound", 2000);
		return true;
	}
}
