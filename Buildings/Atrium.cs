using FunkyBuildings.Framework;
using HarmonyLib;
using StardewValley;
using StardewValley.Buildings;
using StarModGen.Lib;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_Atrium")]
public class Atrium : Building
{
	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);

		if (GetIndoors() is not GameLocation where)
			return;

		where.loadPathsLayerObjectsInArea(0, 0, where.Map.DisplayWidth / 64, where.Map.DisplayHeight / 64);
	}

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<FarmAnimal>(nameof(FarmAnimal.behaviors)).Transpiler(ModifyBehavior);
	}

	public static bool ForceAllowGrassEat(bool grassEatAllowed, GameLocation where)
		=> grassEatAllowed || where.ParentBuilding is Atrium;

	private static IEnumerable<CodeInstruction> ModifyBehavior(IEnumerable<CodeInstruction> codes, ILGenerator gen)
	{
		var il = new CodeMatcher(codes, gen);

		il
			.MatchEndForward(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldfld, typeof(FarmAnimal).GetField(nameof(FarmAnimal.isSwimming)))
			).MatchEndForward(
				new(OpCodes.Ldloc_0),
				new(OpCodes.Ldfld),
				new(OpCodes.Callvirt, typeof(GameLocation).GetProperty(nameof(GameLocation.IsOutdoors))!.GetMethod)
			).Advance(-1);

		var targetField = il.Operand;

		il
			.Advance(2)
			.InsertAndAdvance(
				new(OpCodes.Ldloc_0),
				new(OpCodes.Ldfld, targetField),
				new(OpCodes.Call, typeof(Atrium).GetMethod(nameof(ForceAllowGrassEat)))
			);

		return il.InstructionEnumeration();
	}
}
