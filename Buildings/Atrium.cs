using BuildingsExpanded.Framework;
using HarmonyLib;
using StardewValley;
using StarModGen.Lib;
using System.Reflection.Emit;

namespace BuildingsExpanded.Buildings;

public class Atrium
{

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<FarmAnimal>(nameof(FarmAnimal.behaviors)).Transpiler(ModifyBehavior);
	}

	public static bool ForceAllowGrassEat(bool grassEatAllowed, GameLocation where)
		=> grassEatAllowed || where.HasMapPropertyWithValue(MOD_ID + "_AllowEatGrass");

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
			)
			.MatchStartForward(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Call, typeof(FarmAnimal).GetMethod(nameof(FarmAnimal.GetHarvestType)))
			)
			.MatchEndBackwards(
				new(OpCodes.Ldfld, targetField),
				new(OpCodes.Callvirt, typeof(GameLocation).GetProperty(nameof(GameLocation.IsOutdoors))!.GetMethod)
			)
			.Advance(1)
			.InsertAndAdvance(
				new(OpCodes.Ldloc_0),
				new(OpCodes.Ldfld, targetField),
				new(OpCodes.Call, typeof(Atrium).GetMethod(nameof(ForceAllowGrassEat)))
			);

		return il.InstructionEnumeration();
	}
}
