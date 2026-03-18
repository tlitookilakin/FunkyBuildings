using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace FunkyBuildings.Framework;

public class SpriteEmitter
{
	public string Definition { get; set; }

	public bool ApplyWeather { get; set; }

	private readonly TemporaryAnimatedSpriteList sprites = [];
	private float _depth;

	public SpriteEmitter(string which)
	{
		Definition = which;
	}

	public void Update(GameTime time, float depthOffset)
	{
		for (int i = sprites.Count - 1; i >= 0; i--)
		{
			if (sprites[i].update(time))
			{
				sprites.RemoveAt(i);
				continue;
			}

			if (ApplyWeather)
				sprites[i].position.X += WeatherDebris.globalWind;

			if (_depth != depthOffset)
				sprites[i].layerDepth += depthOffset - _depth;
		}
		_depth = depthOffset;
	}

	public TemporaryAnimatedSprite Emit(Vector2 pos, float depth)
	{
		if (!Assets.assets.Sprites.TryGetValue(Definition, out var def))
			return new();

		var sprite = TemporaryAnimatedSprite.CreateFromData(def, pos.X / 64f, pos.Y / 64f, depth);
		sprites.Add(sprite);
		return sprite;
	}

	public void Draw(SpriteBatch b, Vector2 pos)
	{
		pos += new Vector2(Game1.viewport.X, Game1.viewport.Y);
		foreach(var sprite in sprites)
			sprite.draw(b, false, (int)pos.X, (int)pos.Y);
	}
}
