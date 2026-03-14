using FunkyBuildings.Framework;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StarModGen.Lib;

namespace FunkyBuildings.Features;

internal class BuildingBirds
{
	private static readonly PerScreen<PerchingBirds?> birds = new();
	private static readonly AccessTools.FieldRef<PerchingBirds, Point[]> birdPerches
		= AccessTools.FieldRefAccess<PerchingBirds, Point[]>("_birdLocations");
	private static readonly AccessTools.FieldRef<PerchingBirds, Point[]> birdRoosts
		= AccessTools.FieldRefAccess<PerchingBirds, Point[]>("_birdRoostLocations");

	[ModEvent]
	public static void ChangeLocation(object? _, WarpedEventArgs ev)
	{
		if (ev.NewLocation == ev.OldLocation)
			return;

		birds.Value = RebuildPerches(ev.NewLocation);
	}

	[ModEvent]
	public static void Init(object? _, SetupEventArgs ev)
	{
		ev.Harmony
			.With<Building>(nameof(Building.OnEndMove)).Postfix(DoBuildingMove);
	}

	private static void DoBuildingMove(Building __instance)
	{
		var where = __instance.GetParentLocation();
		foreach (var screen in GameRunner.instance.gameInstances.Where(g => g.instanceGameLocation == where))
			birds.SetValueForScreen(screen.instanceId, UpdatePerchesHere(where, birds.GetValueForScreen(screen.instanceId)));
	}

	private static PerchingBirds? RebuildPerches(GameLocation where)
	{
		var perches = GetPerchesHere(where).ToArray();
		if (perches.Length is 0)
		{
			return null;
		}

		PerchingBirds _birds;

		bool isIsland = false;
		if (where.GetLocationContextId() == "Island")
		{
			_birds = new(
				Game1.temporaryContent.Load<Texture2D>("LooseSprites\\parrots"),
				3, 24, 24, new(12, 19), perches, perches
			);
			isIsland = true;
		}
		else
		{
			_birds = new(
				Game1.birdsSpriteSheet,
				2, 16, 16, new(8, 14), perches, perches
			);
		}

		int count = Math.Max(perches.Length / 2, 1);
		for (int i = 0; i < count; i++)
		{
			int bird_type = Game1.random.Next(0, 4);
			if (!isIsland && where.IsFallHere())
				bird_type = 10;

			_birds.AddBird(bird_type);
		}

		if (isIsland)
			_birds.peckDuration = 0;

		if (Game1.isDarkOut(where))
			_birds.roosting = true;

		return _birds;
	}

	[ModEvent]
	internal static void BuildingsChanged(object? _, BuildingListChangedEventArgs ev)
	{
		var where = ev.Location;
		foreach (var screen in GameRunner.instance.gameInstances.Where(g => g.instanceGameLocation == where))
			birds.SetValueForScreen(screen.instanceId, UpdatePerchesHere(where, birds.GetValueForScreen(screen.instanceId)));
	}

	[ModEvent]
	internal static void Draw(object? _, RenderedWorldEventArgs ev)
	{
		if (birds.Value is PerchingBirds b)
		{
			b.Update(Game1.currentGameTime);
			b.Draw(ev.SpriteBatch);
		}
	}

	[ModEvent]
	internal static void Cleanup(object? _, ReturnedToTitleEventArgs ev)
	{
		birds.GetHashCode();
	}

	public static PerchingBirds? UpdatePerchesHere(GameLocation where, PerchingBirds? _birds)
	{
		var perches = GetPerchesHere(where).ToArray();
		if (perches.Length is 0 or 1)
		{
			return null;
		}

		if (_birds is PerchingBirds b)
		{
			birdPerches(b) = perches;
			birdRoosts(b) = perches;
			if (perches.Length <= b._birds.Count)
				b._birds.RemoveRange(perches.Length - 2, b._birds.Count - perches.Length + 1);

			List<KeyValuePair<Point, Bird?>> displaced = [];

			foreach (var pair in b._birdPointOccupancy)
				if (!perches.Contains(pair.Key))
					displaced.Add(pair); //can't mutate, defer

			foreach (var pair in displaced)
				b._birdPointOccupancy.Remove(pair.Key);

			foreach (var pnt in perches)
				if (!b._birdPointOccupancy.ContainsKey(pnt))
					b._birdPointOccupancy[pnt] = null;

			foreach (var pair in displaced)
				if (pair.Value is Bird bird)
					bird.FlyToNewPoint();
		}

		return _birds;
	}

	private static List<Point> GetPerchesHere(GameLocation where)
	{
		List<Point> perches = [];
		foreach (var b in where.buildings)
			if (b.TryGetCustomField(MOD_ID + "_BirdSpots", out var s))
				AddPerches(perches, s, b.tileX.Value, b.tileY.Value);

		return perches;
	}

	private static void AddPerches(List<Point> points, string s, int x, int y)
	{
		var split = s.Split(' ');
		for (int i = 0; i < split.Length - 1 && ArgUtility.TryGetPoint(split, i, out var p, out _); i += 2)
			points.Add(new(p.X + x, p.Y + y));
	}
}
