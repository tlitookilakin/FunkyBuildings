using StardewModdingAPI;
using StarModGen.Lib;

namespace BuildingsExpanded.Framework
{
	[Config(false)]
	public partial class Config
	{
		#region static
		private static IManifest manifest = null!;
		private static ITranslationHelper i18n = null!;
		private static IModHelper helper = null!;

		internal static Config Init(IManifest Manifest, IModHelper Helper)
		{
            Registering += OnRegistering;
			Applied += OnApplied;

			helper = Helper;
			manifest = Manifest;
			i18n = Helper.Translation;

			var cfg = Create(Helper, Manifest);

			return cfg;
		}

        private static void OnRegistering(object? sender, StarModGen.Utils.IGMCMApi e)
        {
			// TODO add splash
        }

        private static void OnApplied(Config cfg)
		{
			helper.GameContent.InvalidateCache("Data/Buildings");
		}
		#endregion static

		[ConfigValue(true)]
		public bool EnableBirds { get; set; }
	}
}
