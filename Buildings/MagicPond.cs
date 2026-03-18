using FunkyBuildings.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Tools;
using StarModGen.Lib;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_MagicPond")]
public class MagicPond : Building
{
	const string ID = MOD_ID + "_" + nameof(MagicPond);
	private static bool ForceMagicBait = false;
	private readonly SpriteEmitter particle = new("BigSparkle") { ApplyWeather = true };

	public MagicPond(Vector2 tile) : base(ID, tile) { }
	public MagicPond() : base() { }

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<GameLocation>(nameof(GameLocation.getFish)).Prefix(DoMagicPondCatch)
			.With<FishingRod>(nameof(FishingRod.HasMagicBait)).Postfix(OverrideMagicBait)
			.With(nameof(FishingRod.DoFunction)).Transpiler(KillInstantCatch);
	}

	public override void Update(GameTime time)
	{
		base.Update(time);

		if (isUnderConstruction() || newConstructionTimer.Value > 0)
			return;

		var depth = (tileY.Value * 64) / 10000f;
		particle.Update(time, depth);
		UpdateParticles(depth);
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);

		if (isUnderConstruction() || newConstructionTimer.Value > 0)
			return;

		var pos = Game1.GlobalToLocal(new(tileX.Value * 64, tileY.Value * 64));
		DrawWaterEffect(b, new((int)pos.X + 32, (int)pos.Y + 32, (tilesWide.Value - 1) * 64, (tilesHigh.Value - 1) * 64));
		particle.Draw(b, pos);
	}

	private void UpdateParticles(float depth)
	{
		if (Game1.random.NextBool(.1))
		{
			var bounds = this.GetBounds();
			bounds.Inflate(-1, -1);
			var size = bounds.Size;
			int y = Game1.random.Next(size.Y * 64) + 64;
			var pos = new Vector2(Game1.random.Next(size.X * 64) + 32, y);

			var p = particle.Emit(pos, depth + (y + 16) / 10000f);
			p.motion = new((float)Game1.random.NextDouble() -.5f, (float)Game1.random.NextDouble() * -.25f -.5f);
		}
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		base.drawInMenu(b, x, y);

		DrawWaterEffect(b, new(x + 32, y + 32 + 64, (tilesWide.Value - 1) * 64, (tilesHigh.Value - 1) * 64));
		particle.Update(Game1.currentGameTime, 0f);
		particle.Draw(b, new(x, y));
		UpdateParticles(0f);
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

	public static bool IsFromActualFishPond(bool original, Vector2 tile, GameLocation location)
	{
		if (!original)
			return false;

		return location.getBuildingAt(tile) is not MagicPond;
	}

	private static IEnumerable<CodeInstruction> KillInstantCatch(IEnumerable<CodeInstruction> codes)
	{
		var il = new CodeMatcher(codes);

		il
			.MatchStartForward(
				new CodeMatch(OpCodes.Callvirt, typeof(GameLocation).GetMethod(nameof(GameLocation.isTileBuildingFishable)))
			).Advance(1)
			.InsertAndAdvance(
				new(OpCodes.Ldloc_1),
				new(OpCodes.Ldarg_1),
				new(OpCodes.Call, typeof(MagicPond).GetMethod(nameof(IsFromActualFishPond)))
			);

		return il.InstructionEnumeration();
	}

	private void DrawWaterEffect(SpriteBatch b, Rectangle bounds)
	{
		var where = Game1.currentLocation;
		var color = Utility.GetPrismaticColor();

		if (where is null)
			return;

		int tx = 0;
		for (int x = bounds.X; x < bounds.Right;)
		{
			int ty = 0;
			int w = bounds.Right - x < 64 ? bounds.Right - x : 64;

			for (int y = bounds.Y; y < bounds.Bottom;)
			{
				int h = (int)(
					y == bounds.Y ? where.waterPosition % 64f :
					bounds.Bottom - y < 64 ? bounds.Bottom - y :
					64
				);

				if (h is 0)
					h = 1;

				var src = new Rectangle(
					where.waterAnimationIndex * 64, 
					2064 + (((tx + ty) % 2 != 0) ? ((!where.waterTileFlip) ? 128 : 0) : (where.waterTileFlip ? 128 : 0)), 
					w, h
				);

				if (y == bounds.Y)
					src.Y -= h;

				b.Draw(Game1.mouseCursors,	new Vector2(x, y), src, color * .8f, 0f, Vector2.Zero, 1f, SpriteEffects.None, .00001f);
				ty++;
				y += h;
			}
			x += w;
			tx++;
		}
	}
}
