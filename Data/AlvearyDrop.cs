using StardewValley.GameData;

namespace FunkyBuildings.Data;

public class AlvearyDrop : GenericSpawnItemDataWithCondition
{
	public float Chance { get; set; } = 1f;

	public List<QuantityModifier>? ChanceModifiers { get; set; }

	public QuantityModifier.QuantityModifierMode ChanceModifierMode { get; set; }
}
