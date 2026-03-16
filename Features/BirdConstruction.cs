using FunkyBuildings.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Mods;
using StardewValley.Objects;
using StarModGen.Lib;
using System.Reflection;
using System.Reflection.Emit;

namespace FunkyBuildings.Features;

public class BirdConstruction
{
	private static readonly List<ParrotUpgradePerch> effects = [];
	private static readonly AccessTools.FieldRef<Building, NetInt> newTimer 
		= AccessTools.FieldRefAccess<Building, NetInt>("newConstructionTimer");
	private static IModHelper helper = null!;

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs e)
	{
		helper = e.Helper;
		e.Harmony
			.With<Building>(nameof(Building.performActionOnConstruction)).Postfix(DoBirdEffects)
			.With<CarpenterMenu>(nameof(CarpenterMenu.robinConstructionMessage)).Prefix(HideRobinYap)
			.With<IslandNorth>(nameof(IslandNorth.checkAction)).Prefix(ReplaceTrader);

		var hook = FindBuildHook(PatchProcessor.GetOriginalInstructions(typeof(CarpenterMenu).GetMethod(nameof(CarpenterMenu.receiveLeftClick))));
		if (hook != null)
			e.Harmony.Harmony.Patch(hook, transpiler: new(typeof(BirdConstruction), nameof(ModifyMenuDelay)));
	}

	internal static void NewDay(object? _, DayStartedEventArgs e)
	{
		if (Game1.MasterPlayer is Farmer f && f.mailReceived.Contains(MOD_ID + "_IslandHouseUpgrade"))
			AddCasksIfNeeded();
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

		if (Game1.getLocationFromName("IslandWest") is IslandWest w && w.farmhouseRestored.Value && 
			!Game1.MasterPlayer.mailReceived.Contains(MOD_ID + "_IslandHouseUpgrade"))
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
				IslandUpgradePrompt();
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

	private static void IslandUpgradePrompt()
	{
		if (!Game1.player.Items.ContainsId("(O)791", 10))
		{
			Game1.drawObjectDialogue(Assets.LoadString("birdmenu.cantAffordUpgrade").Parse());
		}
		else
		{
			DelayedAction.functionAfterDelay(() =>
			{
				Game1.currentLocation.createQuestionDialogue(
					Assets.LoadString("birdmenu.upgradePrompt").Parse(),
					Game1.currentLocation.createYesNoResponses(),
					(f, s) =>
					{
						if (s == "Yes")
						{
							f.removeFirstOfThisItemFromInventory("(O)791", 10);
							Game1.globalFadeToBlack(DoIslandUpgrade);
						}
					}
				);
			}, 1);
		}
	}

	private static void DoIslandUpgrade()
	{
		var target = Game1.RequireLocation("IslandWest");
		var b = target.getBuildingByType(MOD_ID + "_IslandFarmhouse");

		if (b is null)
			return;

		var oldLoc = Game1.currentLocation.NameOrUniqueName;
		var oldPort = Game1.viewport.Location;

		Game1.currentLocation.cleanupBeforePlayerExit();
		Game1.currentLocation = target;
		Game1.player.viewingLocation.Value = target.NameOrUniqueName;
		Game1.currentLocation.resetForPlayerEntry();
		Game1.globalFadeToClear();
		Game1.displayHUD = false;
		Game1.viewportFreeze = true;
		Game1.viewport.Location = GetViewportPosition(target, b);
		Game1.clampViewportToGameMap();
		Game1.panScreen(0, 0);
		Game1.displayFarmer = false;

		DelayedAction.functionAfterDelay(() => {
			var bounds = b.GetBounds();
			bounds.Inflate(1, 1);
			StartConstructionAnimation(target, bounds, () => ApplyFarmhouseUpgrade(oldLoc, oldPort), "IslandHouseUpgrade");
		}, 1000);
	}

	private static void ApplyFarmhouseUpgrade(string oldLocation, xTile.Dimensions.Location oldViewport)
	{
		Game1.addMail(MOD_ID + "_IslandHouseUpgrade", true, true);
		helper.GameContent.InvalidateCache("Data/Locations");
		Game1.getLocationFromName("IslandFarmHouse")?.mapPath?.Value = $"Maps/{MOD_ID}_IslandFarmHouse2";
		AddCasksIfNeeded();

		DelayedAction.functionAfterDelay(() => 
		{
			LocationRequest locationRequest = Game1.getLocationRequest(oldLocation);
			locationRequest.OnWarp += delegate
			{
				Game1.displayHUD = true;
				Game1.player.viewingLocation.Value = null;
				Game1.viewportFreeze = false;
				Game1.viewport.Location = oldViewport;
				Game1.displayFarmer = true;
			};
			Game1.warpFarmer(locationRequest, Game1.player.TilePoint.X, Game1.player.TilePoint.Y, Game1.player.FacingDirection);
		}, 1000);

	}

	private static void AddCasksIfNeeded()
	{
		var loc = Game1.getLocationFromName(MOD_ID + "_IslandCellar");
		if (loc is null || loc.modData.ContainsKey(MOD_ID + "_CasksAdded"))
			return;

		int sy = 8;
		int len = 7;
		int[] sx = [1, 3, 4, 6, 7, 9, 10, 12, 13, 16, 17];

		foreach (int x in sx)
		{
			for (int y = len + sy - 1; y >= sy; y--)
			{
				Vector2 v = new(x, y);
				if (!loc.Objects.ContainsKey(v))
					loc.Objects.Add(v, new Cask(v));
			}
		}
	}

	private static xTile.Dimensions.Location GetViewportPosition(GameLocation where, Building b)
	{
		Point p;
		if (b is null)
			p = where.GetData()?.DefaultArrivalTile ?? new(0, 0);
		else
			p = b.GetBounds().Center;

		return new((int)(p.X * 64f - Game1.viewport.Width / 2f), (int)(p.Y * 64f - Game1.viewport.Height / 2f));
	}
}
