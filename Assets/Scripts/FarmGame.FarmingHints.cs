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
        private void UpdatePlantingPreview() => UpdateFarmingIcon(MousePoint,OverUI());

        private SpriteRenderer farmingBorder;
        private void HideFarmingPreview()
        {
            if(plantingPreview!=null) plantingPreview.enabled=false;
            if(farmingBorder!=null) farmingBorder.enabled=false;
        }

        // One action icon, anchored to the cell rather than following the pointer.
        private void UpdateFarmingIcon(Vector2 point,bool pointerOverUI)
        {
            HideFarmingPreview();
            var cell=Vector2Int.RoundToInt(point);
            if(!IsInitialized || Held==null || pointerOverUI || WorldInputBlocked || GameSession.Busy ||
                !string.IsNullOrEmpty(GameSession.LastError) || combat==null || combat.IsGameOver ||
                !WorldBounds.Contains(point) || !Near(point) || !WithinReach(cell)) return;
            bool tool=Held.effect==GrowthEffect.Dig || Held.effect==GrowthEffect.Water || Held.effect==GrowthEffect.Scythe;
            var hint=tool?PrimaryFarmHint(point):farming.PlantHint(cell);
            if(!hint.Allowed || combat.IsRolling) return;
            var source=tool?art.ItemSprite(Held):art.Get(FarmArt.PlantingIcon);
            plantingPreview.sprite=source;
            plantingPreview.transform.position=new Vector3(cell.x,cell.y,0);
            plantingPreview.transform.localScale=Vector3.one*(.55f/Mathf.Max(.01f,Mathf.Max(source.bounds.size.x,source.bounds.size.y)));
            plantingPreview.sortingOrder=30000;
            plantingPreview.color=tool?Color.white:new Color(.5f,1,.55f);
            if(farmingBorder==null)
            {
                farmingBorder=Draw("Available farming cell border",cell,Vector2.one,new Color(.25f,1,.3f),29999);
                farmingBorder.sprite=art.Get(FarmArt.FarmingBorderIcon);
            }
            farmingBorder.transform.position=new Vector3(cell.x,cell.y,0);
            farmingBorder.enabled=true;
            plantingPreview.enabled=true;
        }
    }
}
