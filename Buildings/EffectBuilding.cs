using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

namespace BuildingsExpanded.Buildings;

public abstract class EffectBuilding : Building
{
	protected abstract EffectState effectState { get; set; }
	protected abstract Effect GetEffect();
	protected abstract void UpdateParams(Effect effect);
	private static SpriteBatch? bufferBatch;

	public EffectBuilding() : base () { }
	public EffectBuilding(Vector2 tile, string id) : base(id, tile) { }

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (isMoving)
			return;

		if (GetData() is not BuildingData data)
			return;

		float buildDepth = (tileY.Value * 64f + tilesHigh.Value * 64f - data.SortTileOffset * 64f) / 10000f;

		buildDepth =
			effectState.IncrementDepth ?
				effectState.DepthOffset < 0f ? MathF.BitDecrement(buildDepth) :
				effectState.DepthOffset > 0f ? MathF.BitIncrement(buildDepth) :
				buildDepth :
			buildDepth + effectState.DepthOffset;

		DrawEffect(
			b,
			Game1.GlobalToLocal(new Vector2(tileX.Value * 64f, (tileY.Value + tilesHigh.Value) * 64f - data.SourceRect.Height * 4f) + data.DrawOffset * 4f),
			buildDepth,
			data,
			GetParentLocation()?.GetSeasonIndex() ?? Game1.seasonIndex,
			alpha
		);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		base.drawInMenu(b, x, y);
		if (GetData() is not BuildingData data)
			return;

		x += (int)(data.DrawOffset.X * 4f);
		y += (int)(data.DrawOffset.Y * 4f);
		var pos = new Vector2(x, y);

		float buildDepth = (tilesHigh.Value * 64f - data.SortTileOffset * 64f) / 10000f;

		buildDepth = 
			effectState.IncrementDepth ?
				effectState.DepthOffset < 0f ? MathF.BitDecrement(buildDepth) :
				effectState.DepthOffset > 0f ? MathF.BitIncrement(buildDepth) : 
				buildDepth :
			buildDepth + effectState.DepthOffset;

		DrawEffect(b, pos, buildDepth, data, Game1.currentLocation?.GetSeasonIndex() ?? Game1.seasonIndex);
	}

	protected void DrawEffect(SpriteBatch b, Vector2 position, float depth, BuildingData data, int season = 0, float a = 1f)
	{
		if (isUnderConstruction())
			return;

		if (effectState.lastDrawTick != Game1.ticks)
			UpdateBuffer(texture.Value, data);

		if (effectState.Buffer is null)
			return;

		var source = data.SourceRect;
		var seasonOffset = data.SeasonOffset;
		source = new(source.X + seasonOffset.X * season, source.Y + seasonOffset.Y * season, source.Width, source.Height);

		b.Draw(effectState.Buffer, position, source, color * a, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
	}

	protected void UpdateBuffer(Texture2D texture, BuildingData data)
	{
		effectState.lastDrawTick = Game1.ticks;

		var size = new Point(data.SourceRect.Width * 4, data.SourceRect.Height);
		var device = Game1.graphics.GraphicsDevice;
		bufferBatch ??= new SpriteBatch(device);

		if (effectState.Buffer is null || effectState.Buffer.Bounds.Size != size)
		{
			effectState.Buffer?.Dispose();
			effectState.Buffer = new(device, size.X, size.Y);
		}

		var src = data.SourceRect;
		if (effectState.VerticalOffset)
			src = new(src.X, src.Y + src.Height + data.SeasonOffset.Y * 3, src.Width + data.SeasonOffset.X * 3, src.Height + data.SeasonOffset.Y * 3);
		else
			src = new(src.X + src.Width + data.SeasonOffset.X * 3, src.Y, src.Width + data.SeasonOffset.X * 3, src.Height + data.SeasonOffset.Y * 3);

		var effect = GetEffect();
		try 
		{
			UpdateParams(effect);
		}
		catch { }

		var targets = device.GetRenderTargets();

		device.SetRenderTarget(effectState.Buffer);
		device.Clear(Color.Transparent);
		bufferBatch.Begin(effect: effect);
		bufferBatch.Draw(texture, Vector2.Zero, src, Color.White);
		bufferBatch.End();

		device.SetRenderTargets(targets);
	}

	public class EffectState
	{
		public RenderTarget2D? Buffer { get; set; }
		public bool VerticalOffset { get; set; }
		public float DepthOffset { get; set; }
		public int lastDrawTick { get; set; }
		public bool IncrementDepth { get; set; } = true;
	}
}
