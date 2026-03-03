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

[XmlType("Mods_" + MOD_ID + "_LightningRod")]
public class LightningRod : Building
{
	const string ID = MOD_ID + "_LightningRod";

	private readonly NetInt strikeCount = new();
	private readonly NetInt totalStrikes = new();

	public LightningRod(Vector2 tile) : base(ID, tile) { }
	public LightningRod() : base() { }

	public int StrikeCount
	{
		get => strikeCount.Value;
		set => strikeCount.Value = Math.Max(value, 0);
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		NetFields.AddField(strikeCount);
		NetFields.AddField(totalStrikes);
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (base.doAction(tileLocation, who))
			return true;

		if (isUnderConstruction())
			return false;

		if (totalStrikes.Value is 0)
			Game1.drawObjectDialogue(Game1.content.LoadString(LANG_PATH + ":ui.lightning.none"));
		else if (totalStrikes.Value is 1)
			Game1.drawObjectDialogue(Game1.content.LoadString(LANG_PATH + ":ui.lightning.single"));
		else
			Game1.drawObjectDialogue(Game1.content.LoadString(LANG_PATH + ":ui.lightning.multiple", totalStrikes.Value));

		return true;
	}

	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);

		var output = GetBuildingChest("Output");

		if (output is null)
		{
			Print.Warn("Lightning Attractor is mangled; required chests not present.");
		}
		else
		{
			while (strikeCount.Value > 0)
			{
				int stackSize = Math.Min(999, strikeCount.Value);
				strikeCount.Value -= stackSize;
				output.addItem(ItemRegistry.Create("(O)787", stackSize));
			}
		}
	}

	public void ApplyStrike(Farm.LightningStrikeEvent? ev)
	{
		totalStrikes.Value++;
		strikeCount.Value++;

		if (ev is not null)
		{
			ev.createBolt = true;
			ev.boltPosition = this.GetCenter();
			Game1.getFarm().lightningStrikeEvent.Fire(ev);
		}
	}

	[ModEvent]
	internal static void Init(object? s, SetupEventArgs ev)
	{
		ev.Harmony
			.With<Utility>(nameof(Utility.performLightningUpdate)).Transpiler(InsertCheck);
	}

	private static IEnumerable<CodeInstruction> InsertCheck(IEnumerable<CodeInstruction> source, ILGenerator gen)
	{
		var il = new CodeMatcher(source, gen);

		il
			// find and store return
			.End()
			.MatchStartBackwards(
				new CodeMatch(OpCodes.Ret)
			)
			.CreateLabel(out var ret)

			// find getFarm() and inject after
			.Start()
			.MatchStartForward(
				new CodeMatch(OpCodes.Call, typeof(Game1).GetMethod(nameof(Game1.getFarm)))
			)
			.ThrowIfInvalid("Could not find injection point")

			// call TryStrikeAttractor. if true, return
			.CreateLabel(out var jump)
			.InsertAndAdvance(
				new(OpCodes.Ldloc_2),
				new(OpCodes.Call, typeof(LightningRod).GetMethod(nameof(TryStrikeAttractor))),
				new(OpCodes.Brfalse, jump),
				new(OpCodes.Br, ret)
			);

		return il.InstructionEnumeration();
	}

	public static bool TryStrikeAttractor(Farm.LightningStrikeEvent strike)
	{
		var where = Game1.getFarm();
		if (where.getBuildingByType(ID) is not LightningRod attractor || attractor.isUnderConstruction())
			return false;

		attractor.ApplyStrike(strike);
		return true;
	}
}
