using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Stable slot indices. Empty slots are absent; removing an item never shifts its neighbours.
    internal sealed class InventoryState
    {
        private sealed class Stack
        {
            public PlantDefinition Item;
            public int Count;
            public Stack(PlantDefinition item,int count=1) { Item=item;Count=count; }
        }
        private readonly Dictionary<int,Stack> stacks=new();
        private int hotbarSlots=4,storageSlots=1;
        public int Selected { get; private set; }
        public int RemovedSlots { get; private set; }
        public int Count => stacks.Count;
        public int LastOccupiedIndex { get { int last=-1;foreach(var index in stacks.Keys)last=Mathf.Max(last,index);return last; } }
        public PlantDefinition Held => ItemAt(Selected);
        public void Configure(int hotbar,int storage) { hotbarSlots=Mathf.Clamp(hotbar,1,9);storageSlots=Mathf.Max(0,storage); }
        public int Capacity(int storageBonus) => Mathf.Max(1,hotbarSlots+storageSlots+storageBonus-RemovedSlots);
        public int HotbarCapacity(int capacity) => Mathf.Min(hotbarSlots,capacity);
        public PlantDefinition ItemAt(int index) => stacks.TryGetValue(index,out var stack)?stack.Item:null;
        public int QuantityAt(int index) => stacks.TryGetValue(index,out var stack)?stack.Count:0;
        private int EmptySlot(int capacity) { for(int i=0;i<capacity;i++)if(!stacks.ContainsKey(i))return i;return -1; }
        private int MatchingSlot(PlantDefinition item,int capacity)
        { if(item.stackable)foreach(var pair in stacks)if(pair.Key<capacity && pair.Value.Item==item && pair.Value.Count<int.MaxValue)return pair.Key;return -1; }
        public bool CanAdd(PlantDefinition item,int capacity) => item!=null && (MatchingSlot(item,capacity)>=0 || EmptySlot(capacity)>=0);
        public bool TryAdd(PlantDefinition item,int capacity)
        {
            if(item==null)return false;
            int index=MatchingSlot(item,capacity);
            if(index>=0)stacks[index].Count++;
            else { index=EmptySlot(capacity);if(index<0)return false;stacks.Add(index,new Stack(item)); }
            if(index<HotbarCapacity(capacity))Selected=index;
            return true;
        }
        public bool RemoveOne(int index,int capacity) => RemoveAmount(index,1,capacity);
        public bool RemoveAmount(int index,int amount,int capacity)
        {
            if(amount<=0 || !stacks.TryGetValue(index,out var stack) || amount>stack.Count)return false;
            stack.Count-=amount;if(stack.Count==0)stacks.Remove(index);
            ClampSelection(capacity);return true;
        }
        // Full bundles swap; partial bundles only enter an empty or matching stackable slot.
        public bool MoveOrSwap(int from,int to,int capacity,int amount=0)
        {
            if(from==to || from<0 || to<0 || from>=capacity || to>=capacity || !stacks.TryGetValue(from,out var source))return false;
            if(amount==0)amount=source.Count;
            if(amount<1 || amount>source.Count)return false;
            if(stacks.TryGetValue(to,out var target))
            {
                if(source.Item==target.Item && source.Item.stackable)
                {
                    if(target.Count>int.MaxValue-amount)return false;
                    target.Count+=amount;RemoveAmount(from,amount,capacity);return true;
                }
                if(amount!=source.Count)return false;
                stacks[from]=target;stacks[to]=source;return true;
            }
            stacks[to]=new Stack(source.Item,amount);RemoveAmount(from,amount,capacity);return true;
        }
        public bool TryDetachSlot(PlantDefinition slotItem,int capacity)
        {
            if(slotItem==null || capacity<=1 || Count+1>capacity-1)return false;
            int reduced=capacity-1;
            if(stacks.TryGetValue(reduced,out var displaced))
            {
                int empty=EmptySlot(reduced);if(empty<0)return false;
                stacks.Remove(reduced);stacks[empty]=displaced;
                if(Selected==reduced)Selected=empty;
            }
            RemovedSlots++;
            TryAdd(slotItem,reduced);ClampSelection(reduced);return true;
        }
        public bool TryRestoreSlot(PlantDefinition slotItem,int capacity)
        {
            if(RemovedSlots<=0 || slotItem==null || Held!=slotItem)return false;
            RemoveOne(Selected,capacity);RemovedSlots--;return true;
        }
        public void Select(int index,int capacity) => Selected=Mathf.Clamp(index,0,HotbarCapacity(capacity)-1);
        public void ClampSelection(int capacity) => Select(Selected,capacity);
        public void Capture(PlayerSave state,SaveItemCatalog catalog)
        {
            state.selected=Selected;state.removedSlots=RemovedSlots;state.inventoryUsesSlots=true;
            var indices=new List<int>(stacks.Keys);indices.Sort();
            foreach(int index in indices)state.inventory.Add(new ItemStackSave {slot=index,item=catalog.Id(stacks[index].Item),count=stacks[index].Count});
        }
        public void Restore(PlayerSave state,SaveItemCatalog catalog)
        {
            var restored=new Dictionary<int,Stack>();int legacyIndex=0;
            foreach(var stack in state.inventory)
            {
                int index=state.inventoryUsesSlots?stack.slot:legacyIndex++;
                if(index<0 || restored.ContainsKey(index))throw new InvalidOperationException("Invalid or duplicate inventory slot: "+index);
                restored.Add(index,new Stack(catalog.Resolve(stack.item),stack.count));
            }
            stacks.Clear();foreach(var pair in restored)stacks.Add(pair.Key,pair.Value);
            Selected=state.selected;RemovedSlots=state.removedSlots;
        }
    }
}
