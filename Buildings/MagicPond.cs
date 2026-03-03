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

	public MagicPond(Vector2 tile) : base(ID, tile) { }
	public MagicPond() : base() { }

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<GameLocation>(nameof(GameLocation.getFish)).Transpiler(InsertFishCatch)
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

	private static bool OverrideMagicBait(bool hasBait, FishingRod __instance)
		=> hasBait || Game1.currentLocation.getBuildingAt(__instance.bobber.Value / 64f) is MagicPond p && !p.isUnderConstruction();

	public static bool TryCatchMagicFish(Building building, ref Item result, Vector2 tile, int depth)
	{
		string? s = null;

		if (result is not null)
			return false;

		if (building.isUnderConstruction() || building is not MagicPond)
			return false;

		if (!building.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Buildings", ref s) || s is null || s.Length is 0)
			return false;

		var fishablePlaces = DataLoader.Locations(Game1.content)
			.Where(static l => l.Value.Fish is List<SpawnFishData> fish && fish.Count != 0 && Game1.player.locationsVisited.Contains(l.Key))
			.Select(static l => l.Key)
			.ToList();

		var where = Game1.random.ChooseFrom(fishablePlaces);
		result = GameLocation.GetFishFromLocationData(where, tile, depth, Game1.player, false, false) ?? ItemRegistry.Create("(O)168");

		return true;
	}

	private static IEnumerable<CodeInstruction> InsertFishCatch(IEnumerable<CodeInstruction> codes, ILGenerator gen)
	{
		var il = new CodeMatcher(codes, gen);
		var building = gen.DeclareLocal(typeof(Building));
		var miss = gen.DefineLabel();

		il.MatchStartForward(
			new CodeMatch(OpCodes.Isinst, typeof(FishPond))
		);
		var start = il.Pos;
		var block = il.Instruction.ExtractBlocks();

		il.MatchStartForward(
			new CodeMatch(OpCodes.Brfalse_S)
		);
		var exit = il.Operand;

		il.MatchStartForward(
			new CodeMatch(OpCodes.Leave)
		);
		var retjump = il.Operand;
		il.Advance(-1);
		var ret = il.Operand;

		il
			.Advance(start - il.Pos)
			.InsertAndAdvance(
				new(OpCodes.Stloc, building),
				new(OpCodes.Ldloc, building),
				new(OpCodes.Ldloca, ret),
				new(OpCodes.Ldarg, 6),
				new(OpCodes.Ldarg_3),
				new(OpCodes.Call, typeof(MagicPond).GetMethod(nameof(TryCatchMagicFish))),
				new(OpCodes.Brfalse, miss),
				new(OpCodes.Leave, retjump),
				new(OpCodes.Ldloc, building) { labels = [miss] }
			);

		return il.InstructionEnumeration();
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
					color, 0f, Vector2.Zero, 1f, SpriteEffects.None,
					((bounds.Y + .5f) * 64 - 2f) / 10_000f
				);
			}
		}
	}
}
