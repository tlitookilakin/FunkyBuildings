using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using System.Xml.Serialization;

namespace BuildingsExpanded.Buildings;

[XmlType("Mods_" + MOD_ID + "_Mailbox")]
public class Mailbox : Building
{
	public Mailbox(Vector2 tile) : base(MOD_ID + "_Mailbox", tile) { }
	public Mailbox() : base() { }

	public override void draw(SpriteBatch b)
	{
		base.draw(b);

		if (Game1.mailbox.Count is 0)
			return;

		float draw_layer = (tileX.Value + 1) * 64 / 10000f + tileY.Value * 64 / 10000f;
		float yOffset = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);

		b.Draw(
			Game1.mouseCursors, 
			Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64, tileY.Value * 64 - 96 - 48 + yOffset)), 
			new Rectangle(141, 465, 20, 24), 
			Color.White * 0.75f, 
			0f, 
			Vector2.Zero, 
			4f, 
			SpriteEffects.None, 
			draw_layer + 1E-06f
		);
		b.Draw(
			Game1.mouseCursors, 
			Game1.GlobalToLocal(Game1.viewport, new Vector2(tileX.Value * 64 + 32 + 4, tileY.Value * 64 - 64 - 24 - 8 + yOffset)),
			new Rectangle(189, 423, 15, 13), 
			Color.White, 
			0f, 
			new Vector2(7f, 6f), 
			4f, 
			SpriteEffects.None, 
			draw_layer + 1E-05f
		);
	}
}
