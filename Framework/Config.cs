using StardewModdingAPI;
using StardewValley.GameData.Buildings;
using StarModGen.Lib;

namespace FunkyBuildings.Framework
{
	[Config(false)]
	public partial class Config
	{
		#region static
		private static IManifest manifest = null!;
		private static ITranslationHelper i18n = null!;
		private static IModHelper helper = null!;
		private static List<string>? buildingIDs;

		internal static Config Init(IManifest Manifest, IModHelper Helper)
		{
			Registered += OnRegistered;
			Applied += OnApplied;

			helper = Helper;
			manifest = Manifest;
			i18n = Helper.Translation;

			buildingIDs = [.. Helper.ModContent.Load<Dictionary<string, BuildingData>>("assets/buildings.json").Keys];

			var cfg = Create(Helper, Manifest);

			if (cfg.PopulateBuildings())
				Helper.WriteConfig(cfg);

			return cfg;
		}

		private static void OnApplied(Config cfg)
		{
			helper.GameContent.InvalidateCache("Data/Buildings");
		}

		internal static void OnRegistered(object? sender, StarModGen.Utils.IGMCMApi gmcm)
		{
			var cfg = (Config)sender!;

			gmcm.SetTitleScreenOnlyForNextOptions(manifest, true);
			gmcm.AddPageLink(
				manifest, "BuildingsEnabled",
				() => i18n.Get("config.EnabledBuildings.name"),
				() => i18n.Get("config.EnabledBuildings.desc")
			);
			gmcm.AddPage(manifest, "BuildingsEnabled", () => i18n.Get("config.EnabledBuildings.name"));
			foreach (var id in cfg.EnabledBuildings.Keys)
			{
				string name = $"buildings.{id}.name";
				gmcm.AddBoolOption(manifest,
					() => cfg.EnabledBuildings[id],
					(v) => cfg.EnabledBuildings[id] = v,
					() => i18n.Get(name)
				);
			}
			gmcm.AddPage(manifest, "");
			gmcm.SetTitleScreenOnlyForNextOptions(manifest, false);
		}
		#endregion static

		public Dictionary<string, bool> EnabledBuildings
		{
			get => _enabledBuildings;
			set
			{
				_enabledBuildings = value;
				PopulateBuildings();
			}
		}
		private Dictionary<string, bool> _enabledBuildings = [];

		private bool PopulateBuildings()
		{
			if (buildingIDs is not List<string> ids || EnabledBuildings is null)
				return false;

			bool addedAny = false;

			foreach (var key in ids)
				addedAny = EnabledBuildings.TryAdd(key, true) || addedAny;

			return addedAny;
		}

		public bool IsEnabled(string id)
			=> !EnabledBuildings.TryGetValue(id, out bool enabled) || enabled;
	}
}
