using StardewValley;
using StardewValley.Inventories;
using StarModGen.Lib;

namespace FunkyBuildings.Framework
{
	public class InventoryProxy : IDisposable
	{
		private static IInventory? proxied;
		private static int proxyLimit = 0;
		private bool disposedValue;

		[ModEvent]
		internal static void Init(object? _, SetupEventArgs e)
		{
			e.Harmony
				.With<Farmer>(nameof(Farmer.addItemToInventoryBool)).Prefix(ProxyToInventory)
				.With(nameof(Farmer.isMoving)).Postfix(HideMovementIfProxied);
		}

		public InventoryProxy(IInventory store, int limit = 0)
		{
			if (proxied is not null)
				throw new InvalidOperationException("Cannot proxy multiple inventories at once!");

			proxied = store;
			proxyLimit = limit;
		}

		private void Dispose(bool disposing)
		{
			if (disposedValue)
				return;

			proxied = null;
			proxyLimit = 0;
			disposedValue = true;
		}

		~InventoryProxy()
		{
		    Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private static bool ProxyToInventory(ref bool __result, Item item)
		{
			if (proxied is not IInventory inv)
				return true;

			if (!TryStackToExisting(item, inv))
			{
				if (proxyLimit > 0 && inv.Count >= proxyLimit)
				{
					__result = false;
				}
				else
				{
					inv.Add(item);
					__result = true;
				}
			}

			return false;
		}

		private static bool TryStackToExisting(Item item, IInventory inv)
		{
			if (item.Stack is 0)
				return true;

			for (int i = inv.Count - 1; item.Stack != 0; i--)
			{
				var slot = inv[i];

				if (slot is null || slot.Stack is 0)
				{
					inv[i] = item;
					return true;
				}

				if (slot.canStackWith(item))
				{
					int transfer = Math.Min(Math.Max(slot.maximumStackSize() - slot.Stack, 0), item.Stack);
					item.Stack -= transfer;
					slot.Stack += transfer;

					if (item.Stack is 0)
						return true;
				}
			}

			return false;
		}

		private static bool HideMovementIfProxied(bool moving)
			=> moving && proxied is null;
	}
}
