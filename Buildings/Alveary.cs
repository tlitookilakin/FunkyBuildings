using FunkyBuildings.Data;
using FunkyBuildings.Framework;
using FunkyBuildings.Integration;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;
using System.Xml.Serialization;
using SObject = StardewValley.Object;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_Alveary")]
public class Alveary : Building
{
	public const int RANGE = 6;
	private readonly NetInt lastProduceDay = new(-1000);
	public int LastProduceDay
	{
		get => lastProduceDay.Value;
		set => lastProduceDay.Value = value;
	}

	public Alveary(Vector2 tile) : base(MOD_ID + "_Alveary", tile) { }

	public Alveary() : base() { }

	protected override void initNetFields()
	{
		base.initNetFields();
		NetFields.AddField(lastProduceDay);
	}

	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);
		
		if (isUnderConstruction())
			return;

		var chest = GetBuildingChest("Output");
		if (chest is null)
		{
			Print.Warn("Alveary is mangled; required chest not present.");
			return;
		}

		var tiles = GetRegionAround(new(
			tileX.Value + tilesWide.Value / 2,
			tileY.Value + tilesHigh.Value / 2
		));
		var where = GetParentLocation();
		var objectData = ItemRegistry.GetObjectTypeDefinition();

		var queryctx = new GameStateQueryContext(where, null, null, null, null);
		var validExtraDrops = Assets.assets.BuildingData.ExtraAlvearyItems
			.Where(i => i.Condition is null || GameStateQuery.CheckConditions(i.Condition, queryctx))
			.Select(s => new KeyValuePair<float, AlvearyDrop>(
				Utility.ApplyQuantityModifiers(s.Chance, s.ChanceModifiers, s.ChanceModifierMode, where), s
			))
			.ToList();
		var itemctx = new ItemQueryContext(where, null, null, "Alveary");

		foreach (var flower in IBetterBeehouses.API?.GetAllHoneySources(where, tiles) ?? GetAllFlowers(where, tiles))
		{
			chest.addItem(objectData.CreateFlavoredHoney(ItemRegistry.Create<SObject>(flower.Value)));
			foreach (var extra in validExtraDrops)
			{
				if (!Game1.random.NextBool(extra.Key))
					continue;

				var item = ItemQueryResolver.TryResolveRandomItem(extra.Value, itemctx);
				ItemQueryResolver.ApplyItemFields(item, extra.Value, itemctx);
				chest.addItem(item);
			}
		}
	}

	private static IEnumerable<Vector2> GetRegionAround(Vector2 center)
	{
		Rectangle area = new((int)center.X - RANGE, (int)center.Y - RANGE, RANGE * 2 + 1, RANGE * 2 + 1);

		for (int x = area.Left; x < area.Right; x++)
			for (int y = area.Top; y < area.Bottom; y++)
				yield return new Vector2(x, y);
	}

	private static IEnumerable<KeyValuePair<Vector2, string>> GetAllFlowers(GameLocation where, IEnumerable<Vector2> tiles)
	{
		foreach (var tile in tiles)
			if (
				where.terrainFeatures.TryGetValue(tile, out var tf) && tf is HoeDirt dirt && dirt.crop is Crop crop &&
				!crop.dead.Value && crop.currentPhase.Value >= crop.phaseDays.Count - 1 && crop.indexOfHarvest.Value is string id &&
				ItemRegistry.GetDataOrErrorItem(id).Category is -80
			)
				yield return new(tile, id);
	}
}
