using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Owns inventory rules only. No input, scene objects, UI or world drops.
    internal sealed class InventoryState
    {
        private sealed class Stack
        {
            public PlantDefinition Item;
            public int Count;
            public Stack(PlantDefinition item, int count = 1) { Item = item; Count = count; }
        }

        private readonly List<Stack> stacks = new();
        public int Selected { get; set; }
        public int RemovedSlots { get; private set; }
        public int Count => stacks.Count;
        public PlantDefinition Held => ItemAt(Selected);
        public int Capacity(int storageBonus) => Mathf.Max(1, 5 + storageBonus - RemovedSlots);
        public PlantDefinition ItemAt(int index) => Valid(index) ? stacks[index].Item : null;
        public int QuantityAt(int index) => Valid(index) ? stacks[index].Count : 0;
        private bool Valid(int index) => index >= 0 && index < stacks.Count;

        public bool CanAdd(PlantDefinition item, int capacity)
        {
            return item != null && (Count < capacity || (item.stackable && stacks.Exists(stack => stack.Item == item)));
        }

        public bool TryAdd(PlantDefinition item, int capacity)
        {
            if (!CanAdd(item, capacity)) return false;
            int existing = item.stackable ? stacks.FindIndex(stack => stack.Item == item) : -1;
            if (existing >= 0) stacks[existing].Count++;
            else { stacks.Add(new Stack(item)); existing = Count - 1; }
            Selected = existing;
            return true;
        }

        public bool RemoveOne(int index, int capacity)
        {
            if (!Valid(index)) return false;
            if (--stacks[index].Count <= 0) stacks.RemoveAt(index);
            ClampSelection(capacity);
            return true;
        }

        public bool TryDetachSlot(PlantDefinition slotItem, int capacity)
        {
            if (slotItem == null || capacity <= 1 || Count + 1 > capacity - 1) return false;
            RemovedSlots++;
            stacks.Add(new Stack(slotItem));
            Selected = Count - 1;
            return true;
        }

        public bool TryRestoreSlot(PlantDefinition slotItem, int capacity)
        {
            if (RemovedSlots <= 0 || slotItem == null || Held != slotItem) return false;
            RemoveOne(Selected, capacity);
            RemovedSlots--;
            return true;
        }

        public void ClampSelection(int capacity) => Selected = Mathf.Clamp(Selected, 0, capacity - 1);

        public void Capture(PlayerSave state, SaveItemCatalog catalog)
        {
            state.selected = Selected;
            state.removedSlots = RemovedSlots;
            foreach (var stack in stacks)
                state.inventory.Add(new ItemStackSave { item = catalog.Id(stack.Item), count = stack.Count });
        }

        public void Restore(PlayerSave state, SaveItemCatalog catalog)
        {
            // Resolve everything before replacing the live inventory.
            var restored = new List<Stack>();
            foreach (var stack in state.inventory)
                restored.Add(new Stack(catalog.Resolve(stack.item), stack.count));
            stacks.Clear();
            stacks.AddRange(restored);
            Selected = state.selected;
            RemovedSlots = state.removedSlots;
        }
    }
}
