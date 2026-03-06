using FunkyBuildings.Framework;
using FunkyBuildings.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using System.Xml.Serialization;
using SObject = StardewValley.Object;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "ArborealCloche")]
public class ArborealCloche : Building
{
	private static readonly HashSet<FruitTree> clochedTrees = [];

	private readonly NetRef<FruitTree?> tree = new();
	public FruitTree? Tree
	{
		get => tree.Value;
		set
		{
			if (tree.Value is FruitTree ft)
				clochedTrees.Remove(ft);

			tree.Value = value;
			if (value is not null)
			{
				value.Tile = new(
					tileX.Value + tilesWide.Value / 2,
					tileY.Value + tilesHigh.Value / 2
				);
				value.Location = GetParentLocation();
				clochedTrees.Add(value);
			}
		}
	}

	public ArborealCloche() : base() { }
	public ArborealCloche(Vector2 tile) : base(MOD_ID + "_AborealCloche", tile) { }

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (base.doAction(tileLocation, who))
			return true;

		if (!this.GetBounds().Contains(tileLocation))
			return false;

		OpenMenu(who);
		return true;
	}

    public override bool isActionableTile(int xTile, int yTile, Farmer who)
    {
		if (base.isActionableTile(xTile, yTile, who))
			return true;

		if (this.GetBounds().Contains(xTile, yTile))
			return true;

		return false;
    }

	protected override void initNetFields()
	{
		base.initNetFields();
		NetFields.AddField(tree);
		tileX.fieldChangeVisibleEvent += PositionChanged;
		tileY.fieldChangeVisibleEvent += PositionChanged;
		tilesHigh.fieldChangeVisibleEvent += PositionChanged;
		tilesWide.fieldChangeVisibleEvent += PositionChanged;
		parentLocationName.fieldChangeVisibleEvent += LocationChanged;

	}

	private void LocationChanged(NetString field, string oldValue, string newValue)
	{
		if (oldValue == newValue)
			return;

		if (Tree is FruitTree ft)
			ft.Location = GetParentLocation();
	}

	private void PositionChanged(NetInt field, int oldValue, int newValue)
	{
		if (oldValue == newValue)
			return;

		if (Tree is FruitTree ft)
			ft.Tile = this.GetCenter();
	}

	private void OpenMenu(Farmer who)
	{
		if (who != Game1.player)
			return;

		Game1.activeClickableMenu = new ClocheMenu(this);
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		Tree?.draw(b);
	}

	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);

		if (Tree is null)
			return;

		Tree.dayUpdate();

		var output = GetBuildingChest("Output");
		if (output is null)
		{
			Print.Warn($"Chests mangled for arboreal cloche @ {tileX}, {tileY}, {GetParentLocation()?.DisplayName}");
			return;
		}

		foreach (var fruit in Tree.fruit)
			output.addItem(fruit);
		Tree.fruit.Clear();
	}

	public Item? GetSaplingItem()
	{
		if (Tree is not FruitTree ft)
			return null;

		return ItemRegistry.Create(ft.treeId.Value, 1, ft.GetQuality(), true);
	}

	public bool TrySetSapling(Item? sapling)
	{
		if (sapling is null)
		{
			Tree = null;
			return true;
		}

		if (sapling is not SObject sobj || !sobj.IsFruitTreeSapling())
			return false;

		Tree = new(sobj.ItemId);
		Tree.growthRate.Value = Math.Max(1, sobj.Quality + 1) * 2;
		return true;
	}

	public override void performActionOnDemolition(GameLocation location)
	{
		base.performActionOnDemolition(location);

		if (Tree is FruitTree ft)
		{
			clochedTrees.Remove(ft);
			var sapling = ItemRegistry.Create(ft.treeId.Value, 1, ft.GetQuality(), true);
			if (sapling is not null)
				Game1.createItemDebris(sapling, this.GetCenter() * 64f, -1, location);
		}
	}

	~ArborealCloche()
	{
		if (Tree is FruitTree ft)
			clochedTrees.Remove(ft);
	}
}
