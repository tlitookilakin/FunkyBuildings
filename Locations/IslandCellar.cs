using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Locations;
using StardewValley.Objects;
using System.Xml.Serialization;

namespace FunkyBuildings.Locations;

[XmlType("Mods_" + MOD_ID + "_IslandCellar")]
public class IslandCellar : DecoratableLocation
{
	private Texture2D? _parrotTextures;
	private PerchingBirds? _parrots;

	public IslandCellar(string map, string name) : base(map, name)
	{
		AddCasks();
	}

	public IslandCellar() : base() { }

	private void AddCasks()
	{
		int sy = 8;
		int len = 7;
		int[] sx = [1, 3, 4, 6, 7, 9, 10, 12, 13, 16, 17];

		foreach (int x in sx)
		{
			for (int y = len + sy - 1; y >= sy; y--)
			{
				Vector2 v = new(x, y);
				if (!Objects.ContainsKey(v))
					Objects.Add(v, new Cask(v));
			}
		}
	}

    public override void UpdateWhenCurrentLocation(GameTime time)
    {
        base.UpdateWhenCurrentLocation(time);
		_parrots?.Update(time);
    }

    public override void drawAboveAlwaysFrontLayer(SpriteBatch b)
    {
        base.drawAboveAlwaysFrontLayer(b);
		_parrots?.Draw(b);
    }

    protected override void resetLocalState()
    {
        base.resetLocalState();
		_parrotTextures = Game1.temporaryContent.Load<Texture2D>("LooseSprites\\parrots");
		var perches = GetParrotPerches();

		if (perches.Length < 2)
		{
			_parrots = null;
			return;
		}

		_parrots = new(_parrotTextures, 3, 24, 24, new Vector2(12f, 19f), perches, [])
        {
            peckDuration = 0
        };
        for (int i = 0; i < perches.Length / 2; i++)
		{
			_parrots.AddBird(Game1.random.Next(0, 4));
		}
	}

	private Point[] GetParrotPerches()
	{
		if (!TryGetMapProperty("ParrotPerches", out var s))
			return [];

		var split = s.Split(' ');
		var perches = new Point[split.Length / 2];

		for (int i = 0; i < split.Length - 1; i += 2)
		{
			if (int.TryParse(split[i], out int x) && int.TryParse(split[i + 1], out int y))
				perches[i / 2] = new(x, y);
		}

		return perches;
	}
}
