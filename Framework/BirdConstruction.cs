using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Mods;
using StarModGen.Lib;

namespace FunkyBuildings.Framework;

public class BirdConstruction
{
	private static readonly List<ParrotUpgradePerch> effects = [];

	[ModEvent]
	internal static void Init(object? s, SetupEventArgs e)
	{

	}

	public static void StartConstructionAnimation(Building building, bool isUpgrade)
	{
		var perch = new ParrotUpgradePerch(
			building.GetParentLocation(), 
			building.GetCenter().ToPoint(), 
			building.GetBounds(), 0,
			() => {
				building.daysUntilUpgrade.Value = 0;
				building.daysOfConstructionLeft.Value = 0;
			},
			() => building.daysUntilUpgrade.Value == 0 && building.daysOfConstructionLeft.Value == 0,
			"built_" + building.buildingType.Value
		);
		perch.upgradeCompleteEvent.onEvent += () => 
		{ 
			if (Game1.currentLocation == building.GetParentLocation())
				Game1.flashAlpha = 1f;
			effects.Remove(perch);
		};
		perch.upgradeMutex.RequestLock();
		perch.StartAnimation();
		perch.currentState.Value = ParrotUpgradePerch.UpgradeState.Building;

		effects.Add(perch);
	}

	[ModEvent]
	internal static void Draw(object? s, RenderingStepEventArgs ev)
	{
		if (ev.Step is RenderSteps.World_AlwaysFront)
		{
			foreach (var perch in effects)
			{
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
