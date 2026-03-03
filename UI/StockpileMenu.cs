using FunkyBuildings.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Objects;

namespace FunkyBuildings.UI
{
	public class StockpileMenu : MenuWithInventory
	{
		const int COLUMNS = 12;
		const int SLOT_SIZE = 64;
		const int SCROLL_WIDTH = 48;

		private readonly Scrollbar scroll;
		private readonly Chest chest;
		private InventoryMenu storage;
		private readonly ListSlice<Item> slice;
		private readonly KeybindList quickModifier = new(new(SButton.LeftShift),  new(SButton.RightShift));
		private readonly int Capacity;
		private string? hoverTitle;
		private readonly Building? owner;
		private readonly ItemSearch search = new();

		// sorting : ItemGrabMenu.organizeItemsInList

		public StockpileMenu(Chest chest, int capacity, Building? owner = null) : base(null, false, true, -36, 0, 0)
		{
			this.chest = chest;
			this.owner = owner;
			capacity -= capacity % COLUMNS;
			Capacity = capacity;
			scroll = new(capacity + 1000, null, null)
			{
				RowCount = capacity / COLUMNS,
				VisibleRowCount = 1
			};
			width = COLUMNS * SLOT_SIZE + 24;
			height = SLOT_SIZE + borderWidth * 2;
			yPositionOnScreen = spaceToClearTopBorder / 2;
			scroll.OffsetChanged += UpdateOffset;
			slice = new ListSlice<Item>(chest.Items, 0..);
			FillEmpties(chest.Items, capacity);
			storage = new(0, 0, false, slice);
			
			initializeUpperRightCloseButton();
			Resize(new(Game1.uiViewport.X, Game1.uiViewport.Y, Game1.uiViewport.Width, Game1.uiViewport.Height));
			UpdateOffset(0, 0);
		}

		private static void FillEmpties(IList<Item?> inv, int capacity)
		{
			for (int c = inv.Count; c < capacity; c++)
				inv.Add(null);
		}

		private void Resize(Rectangle screen)
		{
			int targetX = (screen.Width / 2) - (width / 2);
			int targetHeight = screen.Height - spaceToClearTopBorder;
			int rows = (targetHeight - (inventory.height + 18 + 18)) / SLOT_SIZE;
			targetHeight = rows * SLOT_SIZE + inventory.height;
			int offsetX = targetX - xPositionOnScreen;
			int top = yPositionOnScreen;

			movePosition(offsetX, targetHeight - (inventory.yPositionOnScreen + spaceToClearTopBorder - 36));
			yPositionOnScreen = top;
			height = targetHeight;

			int pheight = rows * SLOT_SIZE;

			scroll.VisibleRowCount = rows;
			scroll.Resize(new(xPositionOnScreen + width, yPositionOnScreen + 32, SCROLL_WIDTH, pheight));
			upperRightCloseButton.setPosition(xPositionOnScreen + width, yPositionOnScreen - 16);

			slice.Range = (scroll.Offset * COLUMNS)..;
			storage = new(xPositionOnScreen, yPositionOnScreen, false, slice, search.MatchesQuery, COLUMNS * rows, rows);
			// TODO resize/move search
		}

		public override void performHoverAction(int x, int y)
		{
			base.performHoverAction(x, y);
			scroll.Hover(x, y);
			if (hoveredItem is null)
			{
				hoveredItem = storage.hover(x, y, heldItem);
				hoverText = storage.hoverText;
				hoverTitle = storage.hoverTitle;
			}
			else
			{
				hoverText = inventory.hoverText;
				hoverTitle = inventory.hoverTitle;
			}
		}

		private void UpdateOffset(int oldOffset, int offset)
		{
			slice.Range = (offset * COLUMNS)..;
		}

		public override void receiveScrollWheelAction(int direction)
		{
			scroll.Offset -= Math.Sign(direction);
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			Resize(newBounds);
		}

		public override void draw(SpriteBatch b)
		{
			if (Game1.options.showMenuBackground)
				drawBackground(b);

			int vBorderFix = Math.Max(storage.rows - 5, 0) * 4;

			drawTextureBox(b, inventory.xPositionOnScreen - 12, inventory.yPositionOnScreen - 16, inventory.width + 24, inventory.height + 16, Color.White);
			inventory.draw(b);

			drawTextureBox(b, storage.xPositionOnScreen - 12, storage.yPositionOnScreen - 16, storage.width + 24, storage.height + 16 + 8 + vBorderFix, Color.White);
			storage.draw(b);

			upperRightCloseButton.draw(b);
			scroll.Draw(b);
			trashCan.draw(b);
			b.Draw(Game1.mouseCursors, new Vector2(trashCan.bounds.X + 60, trashCan.bounds.Y + 40), new Rectangle(564 + Game1.player.trashCanLevel * 18, 129, 18, 10), Color.White, trashCanLidRotation, new Vector2(16f, 10f), 4f, SpriteEffects.None, 0.86f);

			Vector2 mouseItem = new(Game1.getMouseX() + 16, Game1.getMouseY() + 16);
			heldItem?.drawInMenu(b, mouseItem, 1f);
			if (hoveredItem != null)
				drawToolTip(b, hoverText, hoverTitle, hoveredItem);
			drawMouse(b);
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			scroll.Click(x, y);

			Item? oldHeldItem = heldItem;
			base.receiveLeftClick(x, y, playSound);
			if (heldItem != oldHeldItem && quickModifier.IsDown())
			{
				heldItem = AddToChestUnderlying(heldItem, chest.Items, storage, Capacity);
				if (heldItem != null)
					heldItem = inventory.tryToAddItem(heldItem);
			}

			oldHeldItem = heldItem;
			heldItem = storage.leftClick(x, y, heldItem);
			if (heldItem != oldHeldItem && quickModifier.IsDown())
			{
				heldItem = inventory.tryToAddItem(heldItem);
				if (heldItem != null)
					heldItem = AddToChestUnderlying(heldItem, chest.Items, storage, Capacity);
			}
		}

		private static Item? AddToChestUnderlying(Item? item, IList<Item> items, InventoryMenu menu, int capacity, string sound = "coin")
		{
			if (item is null)
				return item;

			foreach(var slot in items)
			{
				if (slot == null || !slot.canStackWith(item))
					continue;

				item.Stack = slot.addToStack(item);
				if (item.Stack <= 0)
				{
					try
					{
						Game1.playSound(sound);
						menu.onAddItem?.Invoke(item, menu.playerInventory ? Game1.player : null);
					}
					catch { }

					return null;
				}
			}

			for (int i = 0; i < capacity; i++)
			{
				if (i >= items.Count)
					items.Add(item);

				else if (items[i] == null)
					items[i] = item;

				else
					continue;

				try
				{
					Game1.playSound(sound);
					menu.onAddItem?.Invoke(item, menu.playerInventory ? Game1.player : null);
				}
				catch { }

				return null;
			}

			return item;
		}

		public void DisplayReskinMenu()
		{
			if (owner == null)
				return;

			Game1.activeClickableMenu = new BuildingSkinMenu(owner) { exitFunction = () => {
				Game1.activeClickableMenu = new StockpileMenu(chest, Capacity, owner);
			}};
		}

		public static void QuickStack(IList<Item?> from, IList<Item?> to, int capacity)
		{
			for (int i = 0; i < to.Count; i++)
			{
				Item? chest_item = to[i];

				if (chest_item == null || chest_item.maximumStackSize() <= 1)
					continue;

				for (int j = 0; j < from.Count; j++)
				{
					Item? inventory_item = from[j];

					if (inventory_item == null || !chest_item.canStackWith(inventory_item))
						continue;

					int stack_count = chest_item.getRemainingStackSpace() > 0 ? 
						chest_item.addToStack(inventory_item) : 
						inventory_item.Stack;

					inventory_item.Stack = stack_count;
					while (inventory_item.Stack > 0)
					{
						Item? overflow_stack = null;
						if (!Utility.canItemBeAddedToThisInventoryList(chest_item.getOne(), from, capacity))
							break;

						if (overflow_stack == null)
						{
							for (int l = 0; l < to.Count; l++)
							{
								if (to[l] is Item n && n.canStackWith(chest_item) && n.getRemainingStackSpace() > 0)
								{
									overflow_stack = to[l];
									break;
								}
							}
						}
						if (overflow_stack == null)
						{
							for (int k = 0; k < to.Count; k++)
							{
								if (to[k] == null)
								{
									overflow_stack = to[k] = chest_item.getOne();
									overflow_stack.Stack = 0;
									break;
								}
							}
						}
						if (overflow_stack == null && to.Count < capacity)
						{
							overflow_stack = chest_item.getOne();
							overflow_stack.Stack = 0;
							to.Add(overflow_stack);
						}

						if (overflow_stack == null)
							break;

						stack_count = overflow_stack.addToStack(inventory_item);
						inventory_item.Stack = stack_count;
					}

					if (inventory_item.Stack == 0)
						from[j] = null;
				}
			}
		}
	}
}
