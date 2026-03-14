using FunkyBuildings.Data;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StarModGen.Lib;

namespace FunkyBuildings.Framework
{
	internal partial class Assets
	{
		public static Assets assets = null!;

		[Asset("/BuildingData")]
		public partial ExtraData BuildingData { get; }

		[Asset("/UI/Cloche", "assets/ui/cloche.png")]
		public partial Texture2D ClocheUI { get; }

		public Effect StoneGlow
			=> stoneGlow ??= ModUtilities.LoadEffect("assets/effects/stoneglow.mgfx");
		private Effect? stoneGlow;

		public Effect GlassBeams
			=> glassBeams ??= ModUtilities.LoadEffect("assets/effects/glassbeams.mgfx");
		private Effect? glassBeams;

		[AssetEntry]
		public partial void Entry(IModHelper helper);

		[ModEvent]
		public static void Init(object? s, SetupEventArgs ev)
		{
			assets = new();
			assets.Entry(ev.Helper);
		}

		public void ReloadShaders()
		{
			stoneGlow = null;
			glassBeams = null;
		}

		public static string LoadString(string key)
		{
			return Game1.content.LoadString($"Mods/{MOD_ID}/Strings:{key}");
		}

		[AssetEdit("Data/Locations")]
		public void EditLocationData(IAssetData asset)
		{
			var data = asset.AsDictionary<string, LocationData>().Data;
			if (data.TryGetValue("IslandWest", out var loc))
			{
				loc.CreateOnLoad?.AlwaysActive = true;
				loc.DisplayName ??= $"[LocalizedText Mods/{MOD_ID}/Strings:general.islandFarmName]";
			}
		}

		[AssetEdit("Maps/Island_W")]
		public void EditIslandFarm(IAssetData asset)
		{
			var data = asset.AsMap().Data;

			data.Properties["CanBuildHere"] = "T";
			data.Properties["LooserBuildRestrictions"] = "T";
			data.Properties["BuildConditions"] = "PLAYER_HAS_MAIL Any willyBoatFixed";

			if (!data.Properties.ContainsKey("ValidBuildRect"))
				data.Properties["ValidBuildRect"] = "32 34 68 51";
		}
	}
}
