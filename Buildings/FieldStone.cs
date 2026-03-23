using BuildingsExpanded.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.TerrainFeatures;
using StarModGen.Lib;
using System.Xml.Serialization;

namespace BuildingsExpanded.Buildings;

[XmlType("Mods_" + MOD_ID + "_FieldStone")]
public class FieldStone : EffectBuilding
{
	const string ID = MOD_ID + "_" + nameof(FieldStone);
	private static readonly Dictionary<GameLocation, List<Rectangle>> LocationBuildingCache = [];
	private static EffectState state = new() { VerticalOffset = true, DepthOffset = 1f };

	private readonly NetInt radius = new();
	private readonly SpriteEmitter particles = new("Runes");
	private Color[] _colors = [];
	private Color[] Colors
	{
		get
		{
			if (_colors.Length is 0)
				_colors = LoadColors();
			return _colors;
		}
	}

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

	public override void Update(GameTime time)
	{
		base.Update(time);
		float depth = (tileY.Value * 64) / 10000f;
		particles.Update(time, depth);

		float chance = (Radius * 2 + tilesWide.Value) * (Radius * 2 + tilesHigh.Value) * .005f;
		if (Game1.random.NextBool(chance) && Game1.currentLocation is GameLocation where)
		{
			// effect bounds
			var bounds = this.GetBounds();
			bounds.Inflate(Radius, Radius);

			// tile space
			var pnt = Game1.random.NextPoint(bounds).ToVector2();
			if (where.terrainFeatures.TryGetValue(pnt, out var tf) && tf is HoeDirt)
			{
				// local world space
				pnt = new(
					(pnt.X - tileX.Value) * 64f + (float)Game1.random.NextDouble() * 16f - 8f, 
					(pnt.Y - tileY.Value) * 64f + (float)Game1.random.NextDouble() * 16f - 8f
				);
				var part = particles.Emit(pnt, depth + pnt.Y / 10000f);
				part.motion = new((float)Game1.random.NextDouble() * 1f - .5f, -1f);
				var which = Game1.random.Next(4) * 16;
				part.sourceRect.X += which;
				part.sourceRectStartingPos.X += which;
				part.color = Colors[where.GetSeasonIndex()];
			}
		}
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		particles.Draw(b, Game1.GlobalToLocal(new(tileX.Value * 64f, tileY.Value * 64f)));
	}


	public override BuildingData ReloadBuildingData(bool forUpgrade = false, bool forConstruction = false)
	{
		var data = base.ReloadBuildingData(forUpgrade, forConstruction);

		_colors = [];
		if (this.TryGetCustomField(data, MOD_ID + "_EffectRadius", out var s) && int.TryParse(s, out var r))
			Radius = r;

		return data;
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

	private Color[] LoadColors()
	{
		if (!this.TryGetCustomField(MOD_ID + "_ParticleColor", out var v))
			return [Color.White, Color.White, Color.White, Color.White];

		var split = v.Split(',');
		var ret = new Color[4];
		for (int i = 0; i < 4; i++)
		{
			if (i < split.Length)
				ret[i] = Utility.StringToColor(split[i]) ?? Color.White;
			else
				ret[i] = Color.White;
		}

		return ret;
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
			bounds.Inflate(radius, radius);
			items.Add(bounds);
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
	}
}
