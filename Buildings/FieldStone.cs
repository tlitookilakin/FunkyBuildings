using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.TerrainFeatures;
using StarModGen.Lib;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_FieldStone")]
public class FieldStone : EffectBuilding
{
	const string ID = MOD_ID + "_" + nameof(FieldStone);
	private static readonly Dictionary<GameLocation, List<Rectangle>> LocationBuildingCache = [];
	private static EffectState state = new() { VerticalOffset = true, DepthOffset = 1f };

	private readonly NetInt radius = new();

    protected override EffectState effectState 
	{ 
		get => state; 
		set => state = value;
	}
	public int Radius
	{
		get => radius.Value;
		set => radius.Value = value;
	}

    public FieldStone(Vector2 tile) : base(tile, ID) { }

	public FieldStone() : base() { }

	protected override void initNetFields()
	{
		base.initNetFields();
		NetFields.AddField(radius);
	}

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Helper.Events.GameLoop.DayStarted += ClearCache;
		ev.Helper.Events.GameLoop.ReturnedToTitle += ClearCache;

		ev.Harmony
			.With<HoeDirt>(nameof(HoeDirt.GetFertilizerWaterRetentionChance)).Postfix(ModifyWater)
			.With(nameof(HoeDirt.GetFertilizerSpeedBoost)).Postfix(ModifySpeed)
			.With(nameof(HoeDirt.GetFertilizerQualityBoostLevel)).Postfix(ModifyQuality);
	}

	private static float ModifyWater(float original, HoeDirt __instance)
		=> HasStoneInRange(__instance.Location, __instance.Tile) ? 1f : original;

	private static float ModifySpeed(float original, HoeDirt __instance)
		=> HasStoneInRange(__instance.Location, __instance.Tile) ? original + .15f : original;

	private static int ModifyQuality(int original, HoeDirt __instance)
		=> HasStoneInRange(__instance.Location, __instance.Tile) ? original + 1 : original;

	private static void ClearCache(object? s, object e)
		=> LocationBuildingCache.Clear();

	private static bool HasStoneInRange(GameLocation where, Vector2 tile)
	{
		var stones = GetFieldStones(where);
		for (int i = 0; i < stones.Count; i++)
			if (stones[i].Contains(tile))
				return true;
		return false;
	}

	private static List<Rectangle> GetFieldStones(GameLocation where)
	{
		if (LocationBuildingCache.TryGetValue(where, out var items))
			return items;

		items = [];

		foreach (var b in where.buildings)
		{
			if (b.isUnderConstruction())
				continue;

			if (b is not FieldStone stone)
				continue;

			var radius = stone.Radius;
			var bounds = b.GetBounds();
			items.Add(new(bounds.Left - radius, bounds.Top - radius, bounds.Width + radius * 2, bounds.Height + radius * 2));
		}

		LocationBuildingCache[where] = items;
		return items;
	}

	protected override Effect GetEffect()
	{
		return Assets.assets.StoneGlow;
	}

	protected override void UpdateParams(Effect effect)
	{
		effect.Parameters["Time"].SetValue((float)Game1.currentGameTime.TotalGameTime.TotalSeconds);
		if (state.Buffer != null)
			effect.Parameters["Resolution"].SetValue(state.Buffer.Bounds.Size.ToVector2());
	}
}
