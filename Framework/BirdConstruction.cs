using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Mods;
using StarModGen.Lib;
using System.Reflection;
using System.Reflection.Emit;

namespace FunkyBuildings.Framework;

public class BirdConstruction
{
	private static readonly List<ParrotUpgradePerch> effects = [];
	private static readonly AccessTools.FieldRef<Building, NetInt> newTimer 
		= AccessTools.FieldRefAccess<Building, NetInt>("newConstructionTimer");

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs e)
	{
		e.Harmony
			.With<Building>(nameof(Building.performActionOnConstruction)).Postfix(DoBirdEffects)
			.With<CarpenterMenu>(nameof(CarpenterMenu.robinConstructionMessage)).Prefix(HideRobinYap);

		var hook = FindBuildHook(PatchProcessor.GetOriginalInstructions(typeof(CarpenterMenu).GetMethod(nameof(CarpenterMenu.receiveLeftClick))));
		if (hook != null)
			e.Harmony.Harmony.Patch(hook, transpiler: new(typeof(BirdConstruction), nameof(ModifyMenuDelay)));
	}

	private static void DoBirdEffects(Building __instance, GameLocation location)
	{
		if (__instance.GetData().Builder == "IslandBird")
		{
			StartConstructionAnimation(__instance, location);
			__instance.daysOfConstructionLeft.Value = 0;
			__instance.daysUntilUpgrade.Value = 0;
			newTimer(__instance).Value = 2000;
		}
	}

	private static bool HideRobinYap(CarpenterMenu __instance)
	{
		if (__instance.Builder != "IslandBird")
			return true;

		__instance.exitThisMenu();
		Game1.player.forceCanMove();

		return false;
	}

	private static MethodBase? FindBuildHook(IEnumerable<CodeInstruction> instructions)
	{
		var il = new CodeMatcher(instructions);

		il
			.MatchEndForward(
				new(OpCodes.Ldfld, typeof(FarmerTeam).GetField(nameof(FarmerTeam.buildLock))),
				new(OpCodes.Ldarg_0),
				new(OpCodes.Ldftn)
			);

		if (il.IsValid)
			return (MethodBase)il.Operand;

		return null;
	}

	private static IEnumerable<CodeInstruction> ModifyMenuDelay(IEnumerable<CodeInstruction> instructions)
	{
		var il = new CodeMatcher(instructions);

		il
			.MatchStartForward(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Call, typeof(CarpenterMenu).GetMethod(nameof(CarpenterMenu.tryToBuild)))
			).MatchStartForward(
				new CodeMatch(OpCodes.Ldc_I4, 2000)
			).Advance(1)
			.Insert(
				new(OpCodes.Ldarg_0),
				new(OpCodes.Call, typeof(BirdConstruction).GetMethod(nameof(ModifyTime), BindingFlags.Static | BindingFlags.NonPublic))
			);

		return il.InstructionEnumeration();
	}

	private static int ModifyTime(int original, CarpenterMenu menu)
	{
		return menu.Builder is "IslandBird" ? 4500 : original;
	}

	[ModEvent]
	internal static void DebugKeyPress(object? s, ButtonReleasedEventArgs e)
	{
		if (e.Button is StardewModdingAPI.SButton.OemOpenBrackets) 
		{
			Game1.currentLocation?.ShowConstructOptions("IslandBird");
		}
	}

	public static void StartConstructionAnimation(Building building, GameLocation? where = null)
	{
		var bounds = building.GetBounds();
		bounds.Inflate(1, 1);

		StartConstructionAnimation(
			where ??= building.GetParentLocation(),
			bounds,
			() => {
				building.daysUntilUpgrade.Value = 0;
				building.daysOfConstructionLeft.Value = 0;
			},
			"built_" + building.buildingType.Value
		);
	}

	public static void StartConstructionAnimation(GameLocation where, Rectangle region, Action apply, string id)
	{
		bool complete = false;
		var perch = new ParrotUpgradePerch(
			where, new(-10000, -10000), region, 0,
			() => {
				apply();
				complete = true;
			},
			() => complete,
			id
		);
		perch.upgradeCompleteEvent.onEvent += () =>
		{
			if (Game1.currentLocation == where)
				Game1.flashAlpha = 1f;
			effects.Remove(perch);
		};
		perch.StartAnimation();
		perch.currentState.Value = ParrotUpgradePerch.UpgradeState.Building;

		effects.Add(perch);
	}

	[ModEvent]
	internal static void Draw(object? s, RenderingStepEventArgs ev)
	{
		if (ev.Step is RenderSteps.World_AlwaysFront)
		{
			for (int i = effects.Count - 1; i >= 0; i--)
			{
				var perch = effects[i];
				perch.UpdateEvenIfFarmerIsntHere(Game1.currentGameTime);
				if (perch.locationRef.Value == Game1.currentLocation)
				{
					perch.Update(Game1.currentGameTime);
					perch.DrawAboveAlwaysFrontLayer(ev.SpriteBatch);
				}
			}
		}
	}
}
