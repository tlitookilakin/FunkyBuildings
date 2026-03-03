using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace FunkyBuildings.UI
{
	public class Scrollbar
	{
		public readonly ClickableTextureComponent UpArrow;
		public readonly ClickableTextureComponent DownArrow;

		private Rectangle scroller = default;
		const int ARROW_SIZE = 12;
		const int ARROW_SCALE = 3;
		private static readonly Rectangle bgSource = new(403, 383, 6, 6);
		private static readonly Rectangle thumbSource = new(435, 463, 6, 10);

		public event Action<int, int>? OffsetChanged;

		public int Offset
		{
			get => offset;
			set
			{
				value = Math.Clamp(value, 0, rowCount - visibleRowCount);

				if (offset == value)
					return;

				OffsetChanged?.Invoke(offset, value);
				offset = value;
			}
		}

		public int RowCount
		{
			get => rowCount;
			set
			{
				value = Math.Max(value, 1);

				if (rowCount == value)
					return;

				rowCount = value;
				visibleRowCount = Math.Min(visibleRowCount, rowCount);
				Offset = offset;
			}
		}

		public int VisibleRowCount
		{
			get => visibleRowCount;
			set
			{
				value = Math.Clamp(value, 1, rowCount);

				if (visibleRowCount == value)
					return;

				visibleRowCount = value;
				Offset = offset;
			}
		}

		private int offset = 0;
		private int rowCount = 1;
		private int visibleRowCount = 1;

		public Scrollbar(int idBase, ClickableComponent? TopNeighbor, ClickableComponent? BottomNeighbor)
		{
			UpArrow = new(
				new(0, 0, ARROW_SIZE * ARROW_SCALE, ARROW_SIZE * ARROW_SCALE), Game1.mouseCursors, 
				new(421, 459, ARROW_SIZE, ARROW_SIZE), ARROW_SCALE, false)
			{
				myID = idBase,
			};
			if (TopNeighbor != null)
			{
				UpArrow.leftNeighborID = TopNeighbor.myID;
				TopNeighbor.rightNeighborID = UpArrow.myID;
			}
			DownArrow = new(
				new(0, 0, ARROW_SIZE * ARROW_SCALE, ARROW_SIZE * ARROW_SCALE), Game1.mouseCursors, 
				new(421, 472, ARROW_SIZE, ARROW_SIZE), ARROW_SCALE, false)
			{
				myID = idBase + 1,
			};
			if (BottomNeighbor != null)
			{
				DownArrow.leftNeighborID = BottomNeighbor.myID;
				BottomNeighbor.rightNeighborID = DownArrow.myID;
			}
		}

		public void Resize(Rectangle region)
		{
			UpArrow.setPosition(region.Center.X - UpArrow.bounds.Width / 2, region.Top);
			DownArrow.setPosition(region.Center.X - DownArrow.bounds.Width / 2, region.Bottom - DownArrow.bounds.Height);
			scroller = new(
				region.X, UpArrow.bounds.Bottom,
				Math.Max(region.Width, ARROW_SIZE * ARROW_SCALE), region.Height - (UpArrow.bounds.Height + DownArrow.bounds.Height)
			);
			OffsetChanged?.Invoke(offset, offset);
		}

		public void Draw(SpriteBatch batch)
		{
			if (visibleRowCount == rowCount)
				return;

			Rectangle thumbRegion = new(
				scroller.X, scroller.Y + scroller.Height * offset / rowCount, 
				scroller.Width, scroller.Height * visibleRowCount / rowCount
			);

			UpArrow.draw(batch);
			DownArrow.draw(batch);

			DrawSegments(batch, Game1.mouseCursors, bgSource, scroller, 2, 3f);
			DrawSegments(batch, Game1.mouseCursors, thumbSource, thumbRegion, 3, 3f);
		}

		public bool ContainsPoint(int x, int y)
			=> scroller.Contains(x, y) || UpArrow.containsPoint(x, y) || DownArrow.containsPoint(x, y);

		public void Click(int x, int y)
		{
			if (UpArrow.containsPoint(x, y))
				Offset--;

			else if (DownArrow.containsPoint(x, y))
				Offset++;
		}

		public void Hover(int x, int y)
		{
			UpArrow.tryHover(x, y);
			DownArrow.tryHover(x, y);

			if (scroller.Contains(x, y) && Game1.input.GetMouseState().LeftButton is ButtonState.Pressed)
			{
				int my = Game1.getMouseY();
				Offset = (my * (RowCount - VisibleRowCount) / scroller.Height) - VisibleRowCount / 2;
			}
		}

		private static void DrawSegments(SpriteBatch batch, Texture2D texture, Rectangle source, Rectangle dest, int edge, float scale)
		{
			int width = (int)(source.Width * scale);
			int pad = (dest.Width - width) / 2;
			int top = (int)(edge * scale);

			batch.Draw(
				texture, new Rectangle(dest.X + pad, dest.Y, width, top),
				new Rectangle(source.X, source.Y, source.Width, edge), Color.White
			);

			batch.Draw(
				texture, new Rectangle(dest.X + pad, dest.Y + top, width, dest.Height - top * 2),
				new Rectangle(source.X, source.Y + edge, source.Width, source.Height - edge * 2), Color.White
			);

			batch.Draw(
				texture, new Rectangle(dest.X + pad, dest.Bottom - top, width, top),
				new Rectangle(source.X, source.Bottom - edge, source.Width, edge), Color.White
			);
		}
	}
}
