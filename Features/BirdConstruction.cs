using FunkyBuildings.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Mods;
using StarModGen.Lib;
using System.Reflection;
using System.Reflection.Emit;

namespace FunkyBuildings.Features;

public class BirdConstruction
{
	private static readonly List<ParrotUpgradePerch> effects = [];
	private static readonly AccessTools.FieldRef<Building, NetInt> newTimer 
		= AccessTools.FieldRefAccess<Building, NetInt>("newConstructionTimer");
	const string FLAG = MOD_ID + "_IslandFarmhouseUpgraded";

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs e)
	{
		e.Harmony
			.With<Building>(nameof(Building.performActionOnConstruction)).Postfix(DoBirdEffects)
			.With<CarpenterMenu>(nameof(CarpenterMenu.robinConstructionMessage)).Prefix(HideRobinYap)
			.With<IslandNorth>(nameof(IslandNorth.checkAction)).Prefix(ReplaceTrader);

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

	private static List<Response> GetBirdOptions()
	{
		List<Response> opts = [
			new("shop", Assets.LoadString("birdmenu.shop")),
			new("construct", Assets.LoadString("birdmenu.construct"))
		];

		if (Game1.getLocationFromName("IslandWest") is IslandWest w && w.farmhouseRestored.Value && !Game1.MasterPlayer.mailReceived.Contains(FLAG))
			opts.Add(new("upgrade", Assets.LoadString("birdmenu.upgrade")));

		return opts;
	}

	private static void SelectBirdOption(Farmer who, string which)
	{
		switch (which)
		{
			case "construct":
				who.currentLocation.ShowConstructOptions("IslandBird");
				break;
			case "shop":
				Utility.TryOpenShopMenu("IslandTrade", null, playOpenSound: true);
				break;
			case "upgrade":
				//todo
				break;
		}
	}

	private static bool ReplaceTrader(GameLocation __instance, xTile.Dimensions.Location tileLocation, ref bool __result)
	{
		int tileIndexAt = __instance.getTileIndexAt(tileLocation.X, tileLocation.Y, "Buildings", "untitled tile sheet");
		if ((uint)(tileIndexAt - 2074) <= 4u)
		{
			var opts = GetBirdOptions();
			opts.Add(new("quit", Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_Leave")));

			__instance.createQuestionDialogue(Assets.LoadString("birdmenu.prompt"), [.. opts], SelectBirdOption);

			__result = true;
			return false;
		}
		return true;
	}
}
