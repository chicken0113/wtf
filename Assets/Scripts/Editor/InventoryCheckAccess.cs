using EverythingGrows;

// Shared fixture operations for stable slots; occupied count is not a slot index.
public static class InventoryCheckAccess
{
    public static int LastOccupied(FarmGame farm)
    { for(int i=farm.Capacity-1;i>=0;i--)if(farm.InventoryItem(i)!=null)return i;return -1; }
    public static void Clear(FarmGame farm)
    { for(int i=farm.Capacity-1;i>=0;i--)while(farm.InventoryItem(i)!=null)farm.RemoveInventoryItem(i); }
}
