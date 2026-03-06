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

		[AssetEntry]
		public partial void Entry(IModHelper helper);

		[ModEvent]
		public static void Init(object? s, SetupEventArgs ev)
		{
			assets = new();
			assets.Entry(ev.Helper);
		}
	}
}
