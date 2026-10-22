using UnityEngine;

namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        internal FarmActionHint FarmHint(Vector2 point)
        {
            if(!Near(point)) return new(false,"Walk closer first.");
            if(combat.IsRolling) return new(false,"Finish dodging first.");
            var piece=PieceAt(point);
            if(piece!=null) return CanAddItem(piece.definition)?new(true,"Pick up "+piece.definition.label):new(false,"Inventory full; free a slot to pick up.");
            foreach(var building in GetComponentsInChildren<FarmBuilding>())
                if(building.Contains(point)) return new(true,"Harvest "+building.HarvestItem.label);
            return farming.InteractionHint(Vector2Int.RoundToInt(point));
        }
        internal FarmActionHint PrimaryFarmHint(Vector2 point)
        {
            if(Held==null || (Held.effect!=GrowthEffect.Dig && Held.effect!=GrowthEffect.Water && Held.effect!=GrowthEffect.Scythe && Held.effect!=GrowthEffect.House)) return new(false,"");
            if(!Near(point)) return new(false,"Walk closer first.");
            if(combat.IsRolling) return new(false,"Finish dodging first.");
            var cell=Vector2Int.RoundToInt(point);
            if(Held.effect==GrowthEffect.House) return CanPlaceHouse(Held,cell)?new(true,"Install house"):new(false,"Cannot install here; keep the footprint clear.");
            return farming.ToolHint(cell,Held.effect);
        }
        internal bool ShowFarmingHints => IsInitialized && !WorldInputBlocked && !GameSession.Busy && string.IsNullOrEmpty(GameSession.LastError) && !OverUI() && combat!=null && !combat.IsGameOver;
        internal Vector2 FarmingPointer => MousePoint;
    }
}
