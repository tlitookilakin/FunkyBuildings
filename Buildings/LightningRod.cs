using FunkyBuildings.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StarModGen.Lib;
using System.Reflection.Emit;
using System.Xml.Serialization;

namespace FunkyBuildings.Buildings;

[XmlType("Mods_" + MOD_ID + "_LightningRod")]
public class LightningRod : Building
{
	const string ID = MOD_ID + "_LightningRod";

	private readonly NetInt strikeCount = new();
	private readonly NetInt totalStrikes = new();

	public LightningRod(Vector2 tile) : base(ID, tile) { }
	public LightningRod() : base() { }

	public int StrikeCount
	{
		get => strikeCount.Value;
		set
		{
			int old = strikeCount.Value;
			strikeCount.Value = Math.Max(value, 0);

			if (Game1.currentLocation?.NameOrUniqueName == parentLocationName.Value)
			{
				if (old == 0 && strikeCount.Value != 0)
					AmbientLocationSounds.addSound(new(tileX.Value + tilesWide.Value / 2, tileY.Value + tilesHigh.Value), AmbientLocationSounds.sound_engine);
				else if (old != 0 && strikeCount.Value == 0)
					AmbientLocationSounds.removeSound(new(tileX.Value + tilesWide.Value / 2, tileY.Value + tilesHigh.Value));
			}
		}
	}

	public int TotalStrikes
	{
		get => totalStrikes.Value;
		set => totalStrikes.Value = Math.Max(value, 0);
	}

	protected override void initNetFields()
	{
		base.initNetFields();
		NetFields.AddField(strikeCount);
		NetFields.AddField(totalStrikes);
	}

	public override void OnStartMove()
	{
		base.OnStartMove();
		if (Game1.currentLocation.NameOrUniqueName == parentLocationName.Value)
			AmbientLocationSounds.removeSound(new(tileX.Value + tilesWide.Value / 2, tileY.Value + tilesHigh.Value));
	}

	public override void OnEndMove()
	{
		base.OnEndMove();
		if (Game1.currentLocation.NameOrUniqueName == parentLocationName.Value && StrikeCount != 0)
			AmbientLocationSounds.addSound(new(tileX.Value + tilesWide.Value / 2, tileY.Value + tilesHigh.Value), AmbientLocationSounds.sound_engine);
	}

	public override void draw(SpriteBatch b)
	{
		base.draw(b);
		if (!isUnderConstruction())
			DrawFuseBox(b, Game1.GlobalToLocal(new(tileX.Value * 64f, tileY.Value * 64f)), StrikeCount != 0);
	}

	public override void drawInMenu(SpriteBatch b, int x, int y)
	{
		base.drawInMenu(b, x, y);
		if (GetData() is BuildingData data)
			DrawFuseBox(b, new(x, y + data.SourceRect.Height * 4 - tilesHigh.Value * 64), false, data);
	}

	private void DrawFuseBox(SpriteBatch b, Vector2 position, bool shake, BuildingData? data = null)
	{
		data ??= GetData();
		if (data is null)
			return;

		position.X += (tilesWide.Value - 1) * 64f * .5f - 32f;
		position.Y += tilesHigh.Value * 64f;
		float depth = MathF.BitIncrement((tileY.Value * 64f + tilesHigh.Value * 64f) / 10000f);
		position.Y -= 128f;

		if (shake)
		{
			position.X += (float)Game1.random.NextDouble() * 2f - 1f;
			position.Y += (float)Game1.random.NextDouble() * 2f - 1f;
		}

		b.Draw(
			texture.Value, position,
			new Rectangle(data.SourceRect.Right + data.SeasonOffset.X * 3, data.SourceRect.Bottom - 32, 16, 16),
			color * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, depth
		);
	}

	public override bool doAction(Vector2 tileLocation, Farmer who)
	{
		if (base.doAction(tileLocation, who))
			return true;

		if (isUnderConstruction())
			return false;

		if (totalStrikes.Value is 0)
			Game1.drawObjectDialogue(Assets.LoadString("ui.lightning.none"));
		else if (totalStrikes.Value is 1)
			Game1.drawObjectDialogue(Assets.LoadString("ui.lightning.single"));
		else
			Game1.drawObjectDialogue(Game1.content.LoadString(LANG_PATH + ":ui.lightning.multiple", totalStrikes.Value));

		return true;
	}

	public override void dayUpdate(int dayOfMonth)
	{
		base.dayUpdate(dayOfMonth);

		var output = GetBuildingChest("Output");

		if (output is null)
		{
			Print.Warn("Lightning Attractor is mangled; required chests not present.");
		}
		else
		{
			while (StrikeCount > 0)
			{
				var item = ItemRegistry.Create("(O)787", StrikeCount);
				StrikeCount -= item.Stack;
				output.addItem(item);
			}
		}
	}

	public void ApplyStrike(Farm.LightningStrikeEvent? ev)
	{
		totalStrikes.Value++;
		StrikeCount++;

		if (ev is not null)
		{
			ev.createBolt = true;
			ev.boltPosition = this.GetCenter();
			Game1.getFarm().lightningStrikeEvent.Fire(ev);
		}
	}

	[ModEvent]
	internal static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<Utility>(nameof(Utility.performLightningUpdate)).Transpiler(InsertCheck);
	}

	[ModEvent]
	internal static void Warped(object? _, WarpedEventArgs e)
	{
		var b = e.NewLocation.getBuildingByType(MOD_ID + "_LightningRod");
		if (b is LightningRod rod && rod.StrikeCount != 0)
			AmbientLocationSounds.addSound(new(rod.tileX.Value + rod.tilesWide.Value / 2, rod.tileY.Value + rod.tilesHigh.Value), AmbientLocationSounds.sound_engine);
	}

	private static IEnumerable<CodeInstruction> InsertCheck(IEnumerable<CodeInstruction> source, ILGenerator gen)
	{
		var il = new CodeMatcher(source, gen);

		il
			// find and store return
			.End()
			.MatchStartBackwards(
				new CodeMatch(OpCodes.Ret)
			)
			.CreateLabel(out var ret)

			// find getFarm() and inject after
			.Start()
			.MatchStartForward(
				new CodeMatch(OpCodes.Call, typeof(Game1).GetMethod(nameof(Game1.getFarm)))
			)
			.ThrowIfInvalid("Could not find injection point")

			// call TryStrikeAttractor. if true, return
			.CreateLabel(out var jump)
			.InsertAndAdvance(
				new(OpCodes.Ldloc_2),
				new(OpCodes.Call, typeof(LightningRod).GetMethod(nameof(TryStrikeAttractor))),
				new(OpCodes.Brfalse, jump),
				new(OpCodes.Br, ret)
			);

		return il.InstructionEnumeration();
	}

	public static bool TryStrikeAttractor(Farm.LightningStrikeEvent strike)
	{
		var where = Game1.getFarm();
		if (where.getBuildingByType(ID) is not LightningRod attractor || attractor.isUnderConstruction())
			return false;

		attractor.ApplyStrike(strike);
		return true;
	}
}
