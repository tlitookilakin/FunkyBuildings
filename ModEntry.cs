using FunkyBuildings.Framework;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardModGen.Utils;
using StarModGen.Lib;
using System.Reflection;
using System.Xml.Serialization;

namespace FunkyBuildings
{
	public class ModEntry : Mod
	{
		[ModEvent]
		internal static event EventHandler<SetupEventArgs>? Setup;

		private static IModHelper helper = null!;

		public override void Entry(IModHelper helper)
		{
			Print = new(Monitor);
			Debug.Init(helper);
			EventBus.Register(helper);

			Setup?.Invoke(this, new(this, Config.Init(ModManifest, Helper)));
			ModEntry.helper = helper;
		}

		[ModEvent]
		internal static void Launched(object? s, GameLaunchedEventArgs ev)
		{
			var spacecore = helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore")!;
			var asm = typeof(ModEntry).Assembly;
			var xmlType = typeof(XmlTypeAttribute);

			foreach (var type in asm.GetTypes())
				if (type.GetCustomAttribute(xmlType) is not null)
					spacecore.RegisterSerializerType(type);
		}
	}
}
