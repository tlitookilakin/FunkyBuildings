using FunkyBuildings.Data;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
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
	}
}
