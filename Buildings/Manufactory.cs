using FunkyBuildings.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Inventories;
using StardewValley.Objects;
using System.Reflection;
using System.Xml.Serialization;
using SObject = StardewValley.Object;

namespace FunkyBuildings.Buildings
{
	[XmlType("Mods_" + MOD_ID + "_Manufactory")]
	public class Manufactory : Building
	{
		private static readonly Action<SObject, Farmer, bool> CheckMachine =
			typeof(SObject)
			.GetMethod("CheckForActionOnMachine", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
			.CreateDelegate<Action<SObject, Farmer, bool>>();


		public override void performTenMinuteAction(int timeElapsed)
		{
			base.performTenMinuteAction(timeElapsed);

			if (!Context.IsMainPlayer)
				return;

			var input = GetBuildingChest("Input")?.GetItemsForPlayer();
			var output = GetBuildingChest("Output")?.GetItemsForPlayer();

			if (input is null || output is null)
				return;

			if (indoors.Value is not GameLocation interior)
				return;

			foreach (var obj in interior.Objects.Values)
			{
				if (obj.HasContextTag("is_machine"))
					ProcessMachine(obj, input, output, Game1.player);
			}
		}

		private static void ProcessMachine(SObject machine, IInventory input, IInventory output, Farmer who)
		{
			if (machine.IsConsideredReadyMachineForComputer())
			{
				using var proxy = new InventoryProxy(output);
				CheckMachine(machine, who, false);
			}

			if (machine.HasContextTag("machine_input") && (machine.heldObject.Value is null || (machine.heldObject.Value is Chest c && c.isEmpty())))
			{
				SObject.autoLoadFrom = input;
				try
				{
					foreach (var slot in input)
					{
						if (slot is null || slot.Stack is 0)
							continue;

						int oldStack = slot.Stack;
						if (machine.performObjectDropInAction(slot, false, who))
						{
							if (slot.Stack == oldStack)
								slot.Stack--;

							break;
						}
					}
				}
				finally
				{
					SObject.autoLoadFrom = null;
				}
			}
		}
	}
}
