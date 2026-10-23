using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private float customUseCooldown;
        public bool UseSelectedItem(Vector2 point,bool freshPress)
        {
            if(combat==null || combat.IsGameOver || combat.IsRolling || WorldInputBlocked || GameSession.Busy)return false;
            if(Held==null) { if(freshPress)Notify("Select a hoe to till soil.");return false; }
            var item=Held;int slot=selected,count=InventoryQuantity(slot);bool used;
            if(item.useEffect!=null)
            {
                if(customUseCooldown>0 || (!freshPress && !item.useEffect.repeatWhileHeld))return false;
                used=item.useEffect.TryUse(this,item,point);
                if(used)customUseCooldown=item.attackInterval;
            }
            else if(item.effect==GrowthEffect.Weapon)used=combat.Attack(item,PlayerPosition+AimDirection);
            else
            {
                if(!Near(point)) { if(freshPress)Notify("Walk closer first.");return false; }
                bool tool=item.effect==GrowthEffect.Dig || item.effect==GrowthEffect.Water || item.effect==GrowthEffect.Scythe || item.effect==GrowthEffect.Heart || item.effect==GrowthEffect.House;
                if(!tool) { if(freshPress)Notify("This item has no direct use. Right click to plant it.");return false; }
                used=UsePrimaryTool(Vector2Int.RoundToInt(point),freshPress);
            }
            if(!used)return false;
            // House/heart actions already consume themselves. Never charge twice.
            if(item.consumeOnUse && InventoryItem(slot)==item && InventoryQuantity(slot)==count)RemoveInventoryItem(slot);
            actionTime=.25f;return true;
        }
    }
}
