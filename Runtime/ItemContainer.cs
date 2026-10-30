using System;
using System.Collections.Generic;

namespace ITKamianets.Engine.Inventory
{
	/// <summary>What happened to an add or remove that had to be all or nothing.</summary>
	public enum InventoryStatus
	{
		Ok,
		/// <summary>The quantity was zero or negative, or the item id was empty.</summary>
		InvalidQuantity,
		/// <summary>The items would not fit in the free slots.</summary>
		NotEnoughSpace,
		/// <summary>The container holds less of the item than was asked for.</summary>
		NotEnoughItems,
	}

	/// <summary>
	/// A bag, a warehouse, a chest: a fixed number of slots, each holding one kind of item up to the
	/// stack limit. It knows nothing about what the items are, so it works for wood and for swords alike.
	/// </summary>
	public sealed class ItemContainer
	{
		readonly string[] _ids;
		readonly int[] _quantities;

		public int SlotCount { get; }
		public int StackLimit { get; }

		/// <summary>Raised after any change to the contents.</summary>
		public event Action Changed;

		public ItemContainer(int slotCount, int stackLimit = 99)
		{
			if (slotCount < 1) throw new ArgumentOutOfRangeException(nameof(slotCount));
			if (stackLimit < 1) throw new ArgumentOutOfRangeException(nameof(stackLimit));
			SlotCount = slotCount;
			StackLimit = stackLimit;
			_ids = new string[slotCount];
			_quantities = new int[slotCount];
		}

		/// <summary>How many of the item are in the container, in all slots.</summary>
		public int Count(string itemId)
		{
			var total = 0;
			for (var i = 0; i < SlotCount; i++) if (_quantities[i] > 0 && _ids[i] == itemId) total += _quantities[i];
			return total;
		}

		public int TotalItems
		{
			get
			{
				var total = 0;
				for (var i = 0; i < SlotCount; i++) total += _quantities[i];
				return total;
			}
		}

		public int UsedSlots
		{
			get
			{
				var used = 0;
				for (var i = 0; i < SlotCount; i++) if (_quantities[i] > 0) used++;
				return used;
			}
		}

		public bool IsEmpty => TotalItems == 0;

		/// <summary>How many more of this item would fit right now.</summary>
		public int SpaceFor(string itemId)
		{
			var space = 0;
			for (var i = 0; i < SlotCount; i++)
			{
				if (_quantities[i] == 0) space += StackLimit;
				else if (_ids[i] == itemId) space += StackLimit - _quantities[i];
			}
			return space;
		}

		/// <summary>Adds all of it, or nothing at all when it does not fit.</summary>
		public InventoryStatus TryAdd(string itemId, int quantity)
		{
			if (string.IsNullOrEmpty(itemId) || quantity < 1) return InventoryStatus.InvalidQuantity;
			if (SpaceFor(itemId) < quantity) return InventoryStatus.NotEnoughSpace;
			Put(itemId, quantity);
			Changed?.Invoke();
			return InventoryStatus.Ok;
		}

		/// <summary>Adds as much as fits and returns how many were added (0 when full).</summary>
		public int AddUpTo(string itemId, int quantity)
		{
			if (string.IsNullOrEmpty(itemId) || quantity < 1) return 0;
			var amount = Math.Min(quantity, SpaceFor(itemId));
			if (amount < 1) return 0;
			Put(itemId, amount);
			Changed?.Invoke();
			return amount;
		}

		/// <summary>Removes all of it, or nothing at all when there is not enough.</summary>
		public InventoryStatus TryRemove(string itemId, int quantity)
		{
			if (string.IsNullOrEmpty(itemId) || quantity < 1) return InventoryStatus.InvalidQuantity;
			if (Count(itemId) < quantity) return InventoryStatus.NotEnoughItems;
			Take(itemId, quantity);
			Changed?.Invoke();
			return InventoryStatus.Ok;
		}

		/// <summary>Moves up to <paramref name="quantity"/> of an item from one container to another, as many as fit. Returns how many moved.</summary>
		public static int Transfer(ItemContainer from, ItemContainer to, string itemId, int quantity)
		{
			if (from == to || quantity < 1) return 0;
			var amount = Math.Min(Math.Min(quantity, from.Count(itemId)), to.SpaceFor(itemId));
			if (amount < 1) return 0;
			from.Take(itemId, amount);
			to.Put(itemId, amount);
			from.Changed?.Invoke();
			to.Changed?.Invoke();
			return amount;
		}

		/// <summary>Moves everything that fits from one container to another. Returns how many items moved.</summary>
		public static int TransferAll(ItemContainer from, ItemContainer to)
		{
			var moved = 0;
			foreach (var entry in from.Items()) moved += Transfer(from, to, entry.itemId, entry.quantity);
			return moved;
		}

		/// <summary>The contents as totals per item, in the order the items first appear.</summary>
		public List<ItemEntry> Items()
		{
			var totals = new List<ItemEntry>();
			for (var i = 0; i < SlotCount; i++)
			{
				if (_quantities[i] < 1) continue;
				var entry = totals.Find(e => e.itemId == _ids[i]);
				if (entry == null) totals.Add(new ItemEntry(_ids[i], _quantities[i]));
				else entry.quantity += _quantities[i];
			}
			return totals;
		}

		public void Clear()
		{
			for (var i = 0; i < SlotCount; i++)
			{
				_ids[i] = null;
				_quantities[i] = 0;
			}
			Changed?.Invoke();
		}

		public InventoryData ToData()
		{
			return new InventoryData { slots = SlotCount, stackLimit = StackLimit, items = Items() };
		}

		/// <summary>
		/// Replaces the contents with saved ones. Entries that are invalid are skipped, and what does not
		/// fit in this container's slots is dropped, so damaged data can never break the container.
		/// </summary>
		public void Load(InventoryData data)
		{
			for (var i = 0; i < SlotCount; i++)
			{
				_ids[i] = null;
				_quantities[i] = 0;
			}
			if (data?.items != null)
			{
				foreach (var entry in data.items)
				{
					if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.quantity < 1) continue;
					var amount = Math.Min(entry.quantity, SpaceFor(entry.itemId));
					if (amount > 0) Put(entry.itemId, amount);
				}
			}
			Changed?.Invoke();
		}

		/// <summary>A container of the size the data says, filled from it.</summary>
		public static ItemContainer FromData(InventoryData data)
		{
			var container = new ItemContainer(Math.Max(1, data.slots), Math.Max(1, data.stackLimit));
			container.Load(data);
			return container;
		}

		// First top up stacks of the same item, then open empty slots.
		void Put(string itemId, int quantity)
		{
			for (var i = 0; i < SlotCount && quantity > 0; i++)
			{
				if (_quantities[i] == 0 || _ids[i] != itemId) continue;
				var add = Math.Min(StackLimit - _quantities[i], quantity);
				_quantities[i] += add;
				quantity -= add;
			}
			for (var i = 0; i < SlotCount && quantity > 0; i++)
			{
				if (_quantities[i] != 0) continue;
				var add = Math.Min(StackLimit, quantity);
				_ids[i] = itemId;
				_quantities[i] = add;
				quantity -= add;
			}
		}

		// Take from the last stacks first, so earlier slots stay full.
		void Take(string itemId, int quantity)
		{
			for (var i = SlotCount - 1; i >= 0 && quantity > 0; i--)
			{
				if (_quantities[i] == 0 || _ids[i] != itemId) continue;
				var take = Math.Min(_quantities[i], quantity);
				_quantities[i] -= take;
				quantity -= take;
				if (_quantities[i] == 0) _ids[i] = null;
			}
		}
	}
}
