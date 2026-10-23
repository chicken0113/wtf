using UnityEngine;

namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private readonly InventoryState inventory = new();
        private int selected { get => inventory.Selected; set => inventory.Select(value,Capacity); }
        internal int SelectedIndex => selected;
        internal void SelectSlot(int index) { if(index>=0 && index<HotbarCapacity)selected=index; }
        public int HotbarCapacity => inventory.HotbarCapacity(Capacity);
        public int StorageCapacity => Capacity-HotbarCapacity;
        public bool MoveInventoryItem(int from,int to,int amount=0) => inventory.MoveOrSwap(from,to,Capacity,amount);
        public bool DropInventoryItems(int index,int amount)
        {
            var item=inventory.ItemAt(index);
            if(item==null || combat==null || combat.IsGameOver || !inventory.RemoveAmount(index,amount,Capacity))return false;
            for(int i=0;i<amount;i++)Spawn(item,PlayerPosition+Vector2.down*.65f+Vector2.right*((i%3)-1)*.12f);
            return true;
        }
        public int Capacity => inventory.Capacity(StorageBonus);
        public int InventoryCount => inventory.Count;
        public int RemovedSlots => inventory.RemovedSlots;
        private PlantDefinition Held => inventory.Held;
        public PlantDefinition InventoryItem(int index) => inventory.ItemAt(index);
        public int InventoryQuantity(int index) => inventory.QuantityAt(index);
        public bool CanAddItem(PlantDefinition item) => inventory.CanAdd(item, Capacity);
        public bool RemoveInventoryItem(int index) => inventory.RemoveOne(index, Capacity);
        public bool AddItem(PlantDefinition item)
        {
            if (inventory.TryAdd(item, Capacity)) return true;
            message = "No space. Plant or drop an item first."; return false;
        }
        internal void ReconcileInventoryCapacity() => DropOverflow();
        internal void AddRewardGold(int amount)
        {
            if(amount<0)throw new System.ArgumentOutOfRangeException(nameof(amount));
            carriedGold=(int)System.Math.Min(int.MaxValue,(long)Gold+amount);
            shop?.RestoreGold(carriedGold);
        }
        public bool ReceiveHarvest(PlantDefinition item) => ReceiveHarvest(item,1);
        public bool ReceiveHarvest(PlantDefinition item,int quantity)
        {
            if(item==null || quantity<1) return false;
            bool dropped=false;
            for(int i=0;i<quantity;i++)
                if(!inventory.TryAdd(item,Capacity)) { Spawn(item,PlayerPosition+Vector2.down*.65f);dropped=true; }
            message=dropped?"Inventory full. Harvest dropped at your feet.":"Harvested "+item.label+" x"+quantity+".";
            return true;
        }
        private void DropOverflow()
        {
            int dropped=0;
            while(inventory.LastOccupiedIndex>=Capacity)
            {
                int index=inventory.LastOccupiedIndex; var item=inventory.ItemAt(index); int count=inventory.QuantityAt(index);
                for(int i=0;i<count;i++) { RemoveInventoryItem(index); Spawn(item,PlayerPosition+Vector2.down*.65f+Vector2.right*(dropped++%3)*.12f); }
            }
        }
        public bool DetachSlot()
        {
            var slot = definitions != null && definitions.Length >= 5 ? definitions[4] : null;
            if (!inventory.TryDetachSlot(slot, Capacity))
            { message="Keep the final slot and enough room to carry the detached slot."; return false; }
            message="A slot was detached. Your storage capacity decreased."; return true;
        }
        public bool RestoreSlot()
        {
            var slot = definitions != null && definitions.Length >= 5 ? definitions[4] : null;
            if (!inventory.TryRestoreSlot(slot, Capacity)) return false;
            message="The detached slot was reattached to the inventory."; return true;
        }
    }
}
