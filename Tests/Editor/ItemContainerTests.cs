using NUnit.Framework;

namespace ITKamianets.Engine.Inventory.Tests
{
	public class ItemContainerTests
	{
		[Test]
		public void Items_stack_up_to_the_limit_and_then_open_new_slots()
		{
			var bag = new ItemContainer(3, 8);
			Assert.AreEqual(InventoryStatus.Ok, bag.TryAdd("wood", 20));
			Assert.AreEqual(20, bag.Count("wood"));
			Assert.AreEqual(3, bag.UsedSlots);
			Assert.AreEqual(4, bag.SpaceFor("wood"));
			Assert.AreEqual(0, bag.SpaceFor("stone"), "no free slot is left for another kind");
		}

		[Test]
		public void A_full_container_refuses_an_add_without_changing_anything()
		{
			var bag = new ItemContainer(1, 5);
			bag.TryAdd("iron", 4);
			Assert.AreEqual(InventoryStatus.NotEnoughSpace, bag.TryAdd("iron", 2));
			Assert.AreEqual(4, bag.Count("iron"));
			Assert.AreEqual(InventoryStatus.NotEnoughSpace, bag.TryAdd("wood", 1));
		}

		[Test]
		public void AddUpTo_adds_what_fits_and_says_how_many()
		{
			var bag = new ItemContainer(2, 5);
			Assert.AreEqual(10, bag.AddUpTo("wood", 99));
			Assert.AreEqual(0, bag.AddUpTo("wood", 1));
			Assert.AreEqual(0, bag.AddUpTo("wood", 0));
		}

		[Test]
		public void Removing_more_than_there_is_changes_nothing()
		{
			var bag = new ItemContainer(2, 10);
			bag.TryAdd("stone", 7);
			Assert.AreEqual(InventoryStatus.NotEnoughItems, bag.TryRemove("stone", 8));
			Assert.AreEqual(7, bag.Count("stone"));
			Assert.AreEqual(InventoryStatus.Ok, bag.TryRemove("stone", 7));
			Assert.IsTrue(bag.IsEmpty);
			Assert.AreEqual(InventoryStatus.InvalidQuantity, bag.TryRemove("stone", 0));
			Assert.AreEqual(InventoryStatus.InvalidQuantity, bag.TryAdd("", 3));
		}

		[Test]
		public void Transfer_moves_only_what_the_target_has_room_for()
		{
			var bag = new ItemContainer(2, 10);
			var store = new ItemContainer(1, 6);
			bag.TryAdd("wood", 9);

			Assert.AreEqual(6, ItemContainer.Transfer(bag, store, "wood", 99));
			Assert.AreEqual(3, bag.Count("wood"));
			Assert.AreEqual(6, store.Count("wood"));
			Assert.AreEqual(0, ItemContainer.Transfer(bag, store, "wood", 1), "the store is full");
		}

		[Test]
		public void TransferAll_moves_every_kind()
		{
			var bag = new ItemContainer(3, 10);
			var store = new ItemContainer(6, 99);
			bag.TryAdd("wood", 4);
			bag.TryAdd("stone", 2);
			bag.TryAdd("iron", 1);

			Assert.AreEqual(7, ItemContainer.TransferAll(bag, store));
			Assert.IsTrue(bag.IsEmpty);
			Assert.AreEqual(4, store.Count("wood"));
			Assert.AreEqual(1, store.Count("iron"));
		}

		[Test]
		public void Data_round_trips_and_damaged_data_cannot_break_the_container()
		{
			var bag = new ItemContainer(3, 8);
			bag.TryAdd("wood", 10);
			bag.TryAdd("iron", 2);

			var copy = ItemContainer.FromData(bag.ToData());
			Assert.AreEqual(3, copy.SlotCount);
			Assert.AreEqual(10, copy.Count("wood"));
			Assert.AreEqual(2, copy.Count("iron"));

			var damaged = new InventoryData { slots = 1, stackLimit = 5 };
			damaged.items.Add(new ItemEntry("wood", 50));
			damaged.items.Add(new ItemEntry("", 3));
			damaged.items.Add(new ItemEntry("stone", -4));
			damaged.items.Add(null);
			var loaded = ItemContainer.FromData(damaged);
			Assert.AreEqual(5, loaded.Count("wood"), "capped to what fits");
			Assert.AreEqual(5, loaded.TotalItems);
		}

		[Test]
		public void Changed_is_raised_after_a_change()
		{
			var bag = new ItemContainer(1, 5);
			var raised = 0;
			bag.Changed += () => raised++;
			bag.TryAdd("wood", 1);
			bag.TryRemove("wood", 1);
			bag.TryRemove("wood", 1); // fails: no event
			Assert.AreEqual(2, raised);
		}
	}
}
