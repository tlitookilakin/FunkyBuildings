using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

// TODO data entry
[XmlType("Mods_" + MOD_ID + "_Telepad")]
public class Telepad : Building
{
	public const string ID = MOD_ID + "_Telepad";

	private readonly NetString displayName = new();
	public string DisplayName
	{
		get => displayName.Value;
		set => displayName.Value = value;
	}

	public Telepad(Vector2 tile) : base(ID, tile) { }
	public Telepad() : base() { }

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (base.doAction(tileLocation, who))
			return true;

		if (isUnderConstruction())
			return false;

		if (DisplayName is null)
			Game1.activeClickableMenu = new NamingMenu(
				SetName,
				Game1.content.LoadString(LANG_PATH + ":ui.telepad.entername"),
				Game1.currentLocation.DisplayName
			);
		else
			OpenTeleportMenu();

		return true;
	}

	public void OpenTeleportMenu()
	{
		var warps = GetOtherWarpPads(this);
		Game1.currentLocation.ShowPagedResponses(Game1.content.LoadString(LANG_PATH + ":ui.telepad.prompt"), warps, WarpPlayer, true);
	}

	private void SetName(string s)
	{
		DisplayName = s;
		OpenTeleportMenu();
	}

	private static List<KeyValuePair<string, string>> GetOtherWarpPads(Building exclude)
	{
		var items = new List<KeyValuePair<string, string>>();
		Utility.ForEachBuilding(b => {
			if (b != exclude && b is Telepad pad)
				items.Add(new(pad.DisplayName, $"{b.parentLocationName.Value} {b.tileX.Value} {b.tileY.Value}"));
			return true;
		});
		return items;
	}

	private static void WarpPlayer(string target)
	{
		Game1.currentLocation.performTouchAction("MagicWarp " + target, Game1.player.getStandingPosition());
	}
}
