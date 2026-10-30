using System;
using System.Collections.Generic;

namespace ITKamianets.Engine.Inventory
{
	/// <summary>How many of one kind of item. The ids are the game's own strings, such as <c>wood</c>.</summary>
	[Serializable]
	public sealed class ItemEntry
	{
		public string itemId;
		public int quantity;

		public ItemEntry() { }

		public ItemEntry(string itemId, int quantity)
		{
			this.itemId = itemId;
			this.quantity = quantity;
		}
	}

	/// <summary>
	/// The portable form of a container, field for field the same as 3d-inventory-schema (camelCase).
	/// Items are listed by total quantity; how they are spread over slots is the container's business.
	/// </summary>
	[Serializable]
	public sealed class InventoryData
	{
		public int schemaVersion = 1;
		public int slots;
		public int stackLimit;
		public List<ItemEntry> items = new List<ItemEntry>();
	}
}
