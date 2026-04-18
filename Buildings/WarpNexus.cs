using BuildingsExpanded.Data;
using BuildingsExpanded.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.TokenizableStrings;
using StarModGen.Lib;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BuildingsExpanded.Buildings;

[XmlType("Mods_" + MOD_ID + "_WarpNexus")]
public class WarpNexus : Building
{
	private readonly PerScreen<int> lastHoverTick = new();
	private readonly PerScreen<List<Target>> targets = new(() => []);
	private readonly PerScreen<bool> hovered = new();

	private static IModHelper Helper = null!;

	public bool Hovered => hovered.Value;

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		Helper = ev.Helper;
	}

    public override void Update(GameTime time)
    {
        base.Update(time);

		if (isUnderConstruction())
			return;

		bool hovering = GetBoundingBox().Intersects(Game1.player.GetBoundingBox());
		if (hovering != hovered.Value)
		{
			hovered.Value = hovering;
			lastHoverTick.Value = Game1.ticks;

			var ctx = new GameStateQueryContext();

			if (hovering)
			{
				List<Target> targs = Assets.assets.BuildingData.NexusWarps.Where(w =>
					(w.RequiredMod is not string mod || Helper.ModRegistry.IsLoaded(mod)) &&
					(w.Condition is not string cond || GameStateQuery.CheckConditions(cond, ctx))
				)
				.Select(w => new Target(w))
				.ToList();
			}
			else
			{
				targets.Value = [];
			}
		}
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);

		if (hovered.Value)
			DrawTotems(b);
    }

	private void DrawTotems(SpriteBatch b)
	{

	}

	private class Target
	{
		private Action<Action<Action>> ActionImpl;
		private readonly Texture2D tex;
		private Rectangle src;
		private bool hovered = false;
		private int hoverTick = 0;
		private readonly string name;
		private Vector2 stringSize;

		public bool Hovered => hovered;

		public Target(WarpTarget source)
		{
			try
			{
				tex = Game1.content.Load<Texture2D>(source.Texture);
				src = source.TextureSource;
			}
			catch (Exception ex)
			{
				Print.Warn($"Could not load specified texture '{source.Texture}' on warp target '{source.Id}':\n{ex}");
				tex = Game1.mouseCursors;
				src = new(320, 496, 16, 16);
			}

			name = TokenParser.ParseText(source.DisplayName, player: Game1.player);
			stringSize = Game1.smallFont.MeasureString(name);

			string? err = null;
			if (source.WarpHandler is string handlerName && StaticDelegateBuilder.TryCreateDelegate<Action<Action<Action>>>(handlerName, out var handler, out err))
			{
				ActionImpl = handler;
			}
			else if (source.WarpLocation is string where)
			{
				ActionImpl = (a) => NormalWarp(a, where, source.WarpPosition);
			}
			else
			{
				ActionImpl = (a) => { };
				if (err != null)
					Print.Warn($"Could not create delegate for warp target '{source.Id}':\n{err}");
				else
					Print.Warn($"Warp target '{source.Id}' contains neither a handler name nor a target location and will do nothing!");
			}
		}

		public void Draw(SpriteBatch b, Point mouse, Vector2 labelPos, Vector2 pos, Color tint, float depth)
		{
			float mod = Math.Clamp(Game1.ticks - hoverTick, 0, 30) / 30f;
			float scale = 4f + .5f * (hovered ? mod : 1f - mod);

			b.Draw(tex, pos, src, tint, 0f, new(32f, 32f), scale, SpriteEffects.None, depth);

			if (hovered)
			{
				labelPos -= stringSize * .5f;
				// draw box
				b.DrawString(Game1.smallFont, name, labelPos, Color.White);
			}

			Rectangle bounds = new((int)pos.X - 32, (int)pos.Y - 32, 64, 64);
			bool hovering = bounds.Contains(mouse);
			if (hovering != hovered)
			{
				hovered = hovering;
				hoverTick = Game1.ticks;
			}
		}

		public void Action(Action<Action> DoEffect)
		{
			ActionImpl(DoEffect);
		}

		private static void NormalWarp(Action<Action> DoEffect, string where, Point pos)
		{
			(int tileX, int tileY) = pos;
			if (tileX == 0 && tileY == 0)
				Utility.getDefaultWarpLocation(where, ref tileX, ref tileY);

			DoEffect(() => Game1.warpFarmer(where, tileX, tileY, false));
		}
	}
}
