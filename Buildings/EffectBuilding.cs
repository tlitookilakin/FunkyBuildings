using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;

namespace FunkyBuildings.Buildings;

public abstract class EffectBuilding : Building
{
	protected abstract RenderTarget2D? buffer { get; set; }
	protected abstract Effect GetEffect();
	protected abstract int lastDrawTick { get; set; }
	protected abstract bool verticalOffset { get; }
	protected abstract void UpdateParams(Effect effect);
	protected static SpriteBatch? bufferBatch;

	public EffectBuilding() : base () { }
	public EffectBuilding(Vector2 tile, string id) : base(id, tile) { }

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (isMoving)
			return;

		if (GetData() is not BuildingData data)
			return;

		DrawEffect(
			b,
			Game1.GlobalToLocal(new Vector2(tileX.Value * 64f, (tileY.Value + tilesHigh.Value) * 64f - data.SourceRect.Height * 4f) + data.DrawOffset * 4f),
			((tileY.Value + tilesHigh.Value) * 64f + .1f) * .0001f,
			data,
			GetParentLocation()?.GetSeasonIndex() ?? 0,
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

		float buildDepth = (tilesHigh.Value * 64 - data.SortTileOffset * 64f) / 10000f;

		Rectangle sourceRect = getSourceRect();
		DrawEffect(b, pos, MathF.BitIncrement(buildDepth), data, Game1.currentLocation?.GetSeasonIndex() ?? 0);
	}

	protected void DrawEffect(SpriteBatch b, Vector2 position, float depth, BuildingData data, int season = 0, float a = 1f)
	{
		if (isUnderConstruction())
			return;

		if (lastDrawTick != Game1.ticks)
			UpdateBuffer(texture.Value, data);

		if (buffer is null)
			return;

		var source = data.SourceRect;
		var seasonOffset = data.SeasonOffset;
		source = new(source.X + seasonOffset.X * season, source.Y + seasonOffset.Y * season, source.Width, source.Height);

		b.Draw(buffer, position, source, color * a, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth);
	}

	protected void UpdateBuffer(Texture2D texture, BuildingData data)
	{
		lastDrawTick = Game1.ticks;

		var size = new Point(data.SourceRect.Width * 4, data.SourceRect.Height);
		var device = Game1.graphics.GraphicsDevice;
		bufferBatch ??= new SpriteBatch(device);

		if (buffer is null || buffer.Bounds.Size != size)
		{
			buffer?.Dispose();
			buffer = new(device, size.X, size.Y);
		}

		var src = data.SourceRect;
		if (verticalOffset)
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

		device.SetRenderTarget(buffer);
		device.Clear(Color.Transparent);
		bufferBatch.Begin(effect: effect);
		bufferBatch.Draw(texture, Vector2.Zero, src, Color.White);
		bufferBatch.End();

		device.SetRenderTargets(targets);
	}
}
