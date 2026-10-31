# 3d-unity-inventory

Item containers for Unity games on the IT Kamianets 3D Engine: stackable items by id, slot and stack limits, and moving items between containers.

### Logic

An `ItemContainer` has a fixed number of slots; each slot holds one kind of item up to the stack limit. It does not know what an item is: ids are the game's own strings (`wood`, `iron-sword`), so one container type serves a backpack, a warehouse and a loot chest.

- `TryAdd` and `TryRemove` are all or nothing and answer with an `InventoryStatus` (`Ok`, `InvalidQuantity`, `NotEnoughSpace`, `NotEnoughItems`).
- `AddUpTo` adds what fits and says how many.
- `ItemContainer.Transfer` and `TransferAll` move items from one container to another, as many as fit.
- `Count`, `SpaceFor`, `UsedSlots`, `TotalItems` and `Items()` read the contents; `Changed` tells a UI when to redraw.
- `ToData()` and `ItemContainer.FromData()` convert to the portable `InventoryData`, which has the same fields as [`3d-inventory-schema`](../3d-inventory-schema). Loading is forgiving: invalid entries are skipped and what does not fit is dropped, so a damaged save cannot break the container.

No item definitions, recipes, prices or weights, no UI, no networking, no dependencies (it does not even use Unity's API). A game that stores inventories uses `3d-unity-save` for the file.

### Status

v0.1: `ItemContainer`, `ItemEntry`, `InventoryData`, `InventoryStatus`, with edit-mode tests. Used by Temnorid Craft (the backpack and the stores), Dungeons (the stash), Legends (hero items) and Conquest.

### Roadmap

- Per-item stack limits.
- Item metadata (name, icon id) kept by the game next to the ids.
- Move between two specific slots, for an inventory screen.
