using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.TerrainFeatures;
using StarModGen.Lib;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_FieldStone")]
public class FieldStone : Building
{
	const int RADIUS = 4;
	const string ID = MOD_ID + "_" + nameof(FieldStone);
	private static readonly Dictionary<GameLocation, List<Rectangle>> LocationBuildingCache = [];
	private static RenderTarget2D? GlowBuffer;
	private static int lastDrawTick;
	private static SpriteBatch? glowBatch;

	private readonly NetInt radius = new();
	public int Radius
	{
		get => radius.Value; 
		set => radius.Value = value;
	}

	public FieldStone(Vector2 tile) : base(ID, tile) { }

	public FieldStone() : base() { }

	protected override void initNetFields()
	{
		base.initNetFields();
		NetFields.AddField(radius);
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (isMoving)
			return;

		DrawGlow(
			b,
			Game1.GlobalToLocal(new(tileX.Value * 64f, (tileY.Value + tilesHigh.Value) * 64f - GetData().SourceRect.Height * 4f)),
			((tileY.Value + tilesHigh.Value) * 64f + .1f) * .0001f,
			GetParentLocation()?.GetSeasonIndex() ?? 0
		);
	}

	private void DrawGlow(SpriteBatch b, Vector2 position, float depth, int season = 0)
	{
		if (GetData() is not BuildingData data)
			return;

		if (lastDrawTick != Game1.ticks)
			UpdateBuffer(texture.Value, data);

		if (GlowBuffer is null)
			return;

		var source = data.SourceRect;
		var seasonOffset = data.SeasonOffset;
		source = new(source.X + seasonOffset.X * season, source.Y + seasonOffset.Y * season, source.Width, source.Height);
		var pos = new Vector2(tileX.Value * 64f, tileY.Value * 64f + tilesHigh.Value * 64f - source.Height * 4f) + data.DrawOffset * 4f;

		b.Draw(GlowBuffer, Game1.GlobalToLocal(Game1.viewport, pos), source, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
	}

	private static void UpdateBuffer(Texture2D texture, BuildingData data)
	{
		lastDrawTick = Game1.ticks;

		var size = new Point(data.SourceRect.Width * 4, data.SourceRect.Height);
		var device = Game1.graphics.GraphicsDevice;
		glowBatch ??= new SpriteBatch(device);

		if (GlowBuffer is null || GlowBuffer.Bounds.Size != size)
		{
			GlowBuffer?.Dispose();
			GlowBuffer = new(device, size.X, size.Y);
		}

		var src = data.SourceRect;
		src = new(src.X, src.Y + src.Height + data.SeasonOffset.Y * 3, src.Width + data.SeasonOffset.X * 3, src.Height + data.SeasonOffset.Y * 3);

		var effect = Assets.assets.StoneGlow;
		effect.Parameters["Time"].SetValue((float)Game1.ticks);

		var targets = device.GetRenderTargets();

		device.SetRenderTarget(GlowBuffer);
		device.Clear(Color.Transparent);
		glowBatch.Begin(effect: effect);
		glowBatch.Draw(texture, Vector2.Zero, src, Color.White);
		glowBatch.End();

		device.SetRenderTargets(targets);
	}

	[ModEvent]
	internal static void Init(object? s, SetupEventArgs ev)
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
}
