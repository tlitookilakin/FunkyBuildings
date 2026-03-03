using FunkyBuildings.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StarModGen.Lib;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

// TODO customize building in json
[XmlType("Mods_" + MOD_ID + "_Effigy")]
public class OminousEffigy : Building
{
	public const string ID = MOD_ID + "_OminousEffigy";

	private readonly NetInt crowCount = new();
	public int CrowCount
	{
		get => crowCount.Value; 
		set => crowCount.Value = value;
	}

	public OminousEffigy(Vector2 tile) : base(ID, tile) { }
	public OminousEffigy() : base() { }

	[ModEvent]
	internal static void Init(object? s, SetupEventArgs ev)
	{
		ev.Harmony
			.With<Farm>(nameof(Farm.addCrows)).Transpiler(ModifyCrowCheck);
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (base.doAction(tileLocation, who))
			return true;

		if (isUnderConstruction())
			return false;

		if (crowCount.Value is 0)
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12926"));
		else if (crowCount.Value is 1)
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12927"));
		else
			Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12929", crowCount.Value));

		return true;
	}

	public static void IncrementEffigy(Building building)
	{
		if (building is OminousEffigy effigy)
			effigy.CrowCount++;
	}

	private static IEnumerable<CodeInstruction> ModifyCrowCheck(IEnumerable<CodeInstruction> source, ILGenerator gen)
	{
		var il = new CodeMatcher(source, gen);
		var effigy = gen.DeclareLocal(typeof(Building));
		var skipScarecrowTiles = gen.DefineLabel();

		il
			.MatchStartForward(
				new(OpCodes.Newobj),
				new(OpCodes.Stloc_1)
			)
			.Advance(2)
			.InsertAndAdvance(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldstr, ID),
				new(OpCodes.Callvirt, typeof(GameLocation).GetMethod(nameof(GameLocation.getBuildingByType))),
				new(OpCodes.Stloc, effigy),
				new(OpCodes.Ldloc, effigy),
				new(OpCodes.Brtrue, skipScarecrowTiles)
			)
			.MatchStartForward(
				new CodeMatch(OpCodes.Endfinally)
			)
			.Advance(1)
			.AddLabels([skipScarecrowTiles])
			.MatchStartForward(
				new(OpCodes.Ldc_R8),
				new(OpCodes.Bge_Un)
			)
			.Advance(2)
			.CreateLabel(out var noEffigy)
			.InsertAndAdvance(
				new(OpCodes.Ldloc, effigy),
				new(OpCodes.Brfalse, noEffigy),
				new(OpCodes.Ldloc, effigy),
				new(OpCodes.Call, typeof(OminousEffigy).GetMethod(nameof(IncrementEffigy))),
				new(OpCodes.Br, il.InstructionAt(-1).operand)
			);

		return il.InstructionEnumeration();
	}
}
