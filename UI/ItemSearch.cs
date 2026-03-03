using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Internal;
using StardewValley.Menus;

namespace FunkyBuildings.UI
{
	internal class ItemSearch : TextBox
	{
		private enum SearchMode { None, Literal, Query, Tag, ID }

		private string[] whitelist = [];
		private string term = "";
		private SearchMode mode = SearchMode.None;
		private readonly ItemQueryContext queryContext;

		public ItemSearch() : base(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Color.Black)
		{
			queryContext = new(Game1.currentLocation, Game1.player, Game1.random, "item search");
		}

		public override void RecieveTextInput(char inputChar)
		{
			base.RecieveTextInput(inputChar);
			UpdateSearch();
		}

		public override void RecieveTextInput(string text)
		{
			base.RecieveTextInput(text);
			UpdateSearch();
		}

		public void UpdateSearch()
		{
			if (Text.Length == 0)
			{
				mode = SearchMode.None;
				return;
			}

			switch (Text[0])
			{
				case '$':
					mode = SearchMode.Query;
					var results = ItemQueryResolver.TryResolve(Text[1..], queryContext, ItemQuerySearchMode.AllOfTypeItem);
					whitelist = new string[results.Length];
					for (int i = 0; i < results.Length; i++)
						whitelist[i] = ((Item)results[i].Item).QualifiedItemId;
					break;
				case '#':
					mode = SearchMode.Tag;
					term = Text[1..];
					break;
				case '@':
					mode = SearchMode.ID;
					term = Text[1..];
					break;
				default:
					mode = SearchMode.Literal;
					term = Text;
					break;
			}
		}

		public bool MatchesQuery(Item item)
		{
			return mode switch {
				SearchMode.None => true,
				SearchMode.Literal => item.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase),
				SearchMode.Query => whitelist.Contains(item.QualifiedItemId),
				SearchMode.Tag => item.HasContextTag(term),
				SearchMode.ID => item.ItemId.StartsWith(term, StringComparison.OrdinalIgnoreCase),
				_ => true
			};
		}
	}
}
