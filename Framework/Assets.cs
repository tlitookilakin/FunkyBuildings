using BuildingsExpanded.Data;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Locations;
using StarModGen.Lib;

namespace BuildingsExpanded.Framework
{
	internal partial class Assets
	{
		public static Assets assets = null!;

		[Asset("/BuildingData")]
		public partial ExtraData BuildingData { get; }

		[Asset("/UI/Cloche", "assets/ui/cloche.png")]
		public partial Texture2D ClocheUI { get; }

		[Asset("/Sprites", "assets/sprites.json")]
		public partial Dictionary<string, TemporaryAnimatedSpriteDefinition> Sprites { get; }

		public Effect StoneGlow
			=> stoneGlow ??= ModUtilities.LoadEffect("assets/effects/stoneglow.mgfx");
		private Effect? stoneGlow;

		public Effect GlassBeams
			=> glassBeams ??= ModUtilities.LoadEffect("assets/effects/glassbeams.mgfx");
		private Effect? glassBeams;

		public Effect WeatherOverlay
			=> weatherOverlay ??= ModUtilities.LoadEffect("assets/effects/weather.mgfx");
		private Effect? weatherOverlay;

		private static IModContentHelper mod = null!;

		[AssetEntry]
		public partial void Entry(IModHelper helper);

		[ModEvent]
		public static void Init(object? s, SetupEventArgs ev)
		{
			mod = ev.Helper.ModContent;
			assets = new();
			assets.Entry(ev.Helper);
		}

		public void ReloadShaders()
		{
			stoneGlow = null;
			glassBeams = null;
			weatherOverlay = null;
		}

		public static string LoadString(string key)
		{
			return Game1.content.LoadString($"{LANG_PATH}:{key}");
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

			if (data.TryGetValue("IslandFarmHouse", out loc))
			{
				if (Game1.MasterPlayer is Farmer f && f.mailReceived.Contains(MOD_ID + "_IslandHouseUpgrade"))
					loc.CreateOnLoad?.MapPath = $"Maps/{MOD_ID}_IslandFarmHouse2";
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

		[AssetEdit("Data/Buildings")]
		public void AddBirdSpots(IAssetData asset)
		{
			const string key = MOD_ID + "_BirdSpots";

			var data = asset.AsDictionary<string, BuildingData>().Data;
			var spots = mod.Load<Dictionary<string, string>>("assets/misc/birdspots.json");
			foreach ((var id, var spot) in spots)
			{
				if (!data.TryGetValue(id, out var d))
					continue;

				d.Metadata ??= [];
				if (!d.Metadata.ContainsKey(key))
					d.Metadata.Add(key, spot);
			}
		}
	}
}
