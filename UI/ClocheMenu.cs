using FunkyBuildings.Buildings;
using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;

namespace FunkyBuildings.UI;

// TODO add close button
public class ClocheMenu : MenuWithInventory
{
	private InventoryMenu storage;
	private ArborealCloche cloche;
	private Item? sapling;
	private ClickableComponent saplingSlot = new(new(0, 0, 64, 64), (Item?)null);
	private Rectangle leftPanel = new();
	private Texture2D ui;
	private int lastSaplingClick = 0;
	private readonly TemporaryAnimatedSpriteList sprites = new();
	private readonly Color woodColor = new(170, 106, 46);

	public ClocheMenu(ArborealCloche source)
	{
		ui = Assets.assets.ClocheUI;
		cloche = source;
		var items = cloche.GetBuildingChest("Output")?.Items;
		storage = new(0, 0, false, items, capacity: 21, drawSlots: true);
		sapling = source.GetSaplingItem();

		Resize(Game1.uiViewport.ToXna());
	}

	public void DumpSapling()
	{
		if (sapling is null)
			return;

		cloche.Tree = null;

		//put in output
		var inv = cloche.GetBuildingChest("Output")?.Items;
		if (inv != null)
		{
			var remainder = Utility.addItemToThisInventoryList(sapling, inv, 21);
			if (remainder is null || remainder.Stack <= 0)
			{
				sapling = null;
				return;
			}

			sapling = remainder;
		}

		//add to hand
		if (heldItem == null)
		{
			heldItem = sapling;
			sapling = null;
			return;
		}

		//dump on floor
		var bounds = cloche.GetBoundingBox();
		Game1.createItemDebris(sapling, new Vector2(bounds.Center.X, bounds.Bottom), -1, cloche.GetParentLocation());
		sapling = null;
	}

	public bool TrySetSapling()
	{
		if (heldItem is null || sapling != null)
			return false;

		bool val = cloche.TrySetSapling(heldItem);
		sapling = cloche.GetSaplingItem();
		return val;
	}

	public override void draw(SpriteBatch b)
	{
		drawTextureBox(b, inventory.xPositionOnScreen - 12, inventory.yPositionOnScreen - 16, inventory.width + 24, inventory.height + 16, Color.White);
		drawTextureBox(b, storage.xPositionOnScreen - 12, storage.yPositionOnScreen - 16, storage.width + 24, storage.height + 16, Color.White);
		b.Draw(ui, leftPanel, Color.White);

		sapling?.drawInMenu(b, saplingSlot.bounds.Location.ToVector2(), .8f);
		storage.draw(b);
		inventory.draw(b);

		sprites.Draw(b, Game1.currentGameTime, true);

		heldItem?.drawInMenu(b, new(Game1.getMouseX(), Game1.getMouseY()), 1f);
		drawMouse(b);

		if (hoveredItem is Item i)
		{
			drawToolTip(b, i.getDescription(), i.DisplayName, i);
		}
	}

	private void Resize(Rectangle bounds)
	{
		inventory.SetPosition(bounds.Width / 2 - inventory.width / 2, bounds.Height / 2 + 64);
		storage.SetPosition(inventory.width + inventory.xPositionOnScreen - storage.width, inventory.yPositionOnScreen - 32 - storage.height);
		leftPanel = new(inventory.xPositionOnScreen - 20, storage.yPositionOnScreen - 16, 308, 232);
		saplingSlot.bounds = new(leftPanel.X + 8 + 148 - 32, leftPanel.Y + 76, 64, 64);
	}

	public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
	{
		Resize(newBounds);
	}

	public override void performHoverAction(int x, int y)
	{
		hoveredItem = null;

		base.performHoverAction(x, y);
		storage.performHoverAction(x, y);

		if (saplingSlot.containsPoint(x, y))
		{
			hoveredItem = sapling;
		}
	}

	public override void receiveLeftClick(int x, int y, bool playSound = true)
	{
		base.receiveLeftClick(x, y, playSound);

		if (heldItem == null)
		{
			var item = storage.leftClick(x, y, null, playSound); 
			if (item != null)
			{
				item = Game1.player.addItemToInventory(item);
				if (item != null && item.Stack > 0)
					heldItem = item;
			}
		}

		if (saplingSlot.containsPoint(x, y))
		{
			if (heldItem != null)
			{
				if (TrySetSapling())
				{
					heldItem.Stack--;
					if (heldItem.Stack <= 0)
						heldItem = null;

					if (playSound)
						Game1.playSound("stoneStep");
				}
			}
			else if (sapling != null)
			{
				if (playSound)
					Game1.playSound("axchop");

				if (Game1.ticks - lastSaplingClick <= 45)
				{
					DumpSapling();
					DoSaplingChopEffect();

					if (playSound)
						Game1.playSound("treecrack");
				}

				lastSaplingClick = Game1.ticks;
			}
		}
	}

	private void DoSaplingChopEffect()
	{
		var pos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
		for (int i = 0; i < 10; i++)
		{
			sprites.Add(
				new(
					Game1.debrisSpriteSheetName,
					Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, Game1.random.NextBool() ? 12 : 13, 16, 16),
					pos, false, .01f, woodColor
				)
				{
					acceleration = new(0f, .45f),
					motion = new((Game1.random.NextSingle() - .5f) * 8f, (Game1.random.NextSingle() - .5f) * 8f),
					scale = 4f
				}
			);
		}
		
	}
}
