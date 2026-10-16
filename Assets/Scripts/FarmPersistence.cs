using System.Collections.Generic;
using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private readonly HashSet<string> removedMapObjects=new();
        private int carriedGold;
        private readonly Dictionary<FarmBuilding,string> authoredBuildingKeys=new();
        private string AuthoredKey(FarmBuilding building) => authoredBuildingKeys.TryGetValue(building,out var key)?key:MapObjectId.Key(building);
        public PlayerSave CapturePlayer(SaveItemCatalog catalog)
        {
            var state=new PlayerSave {gold=Gold,selected=selected,removedSlots=removedSlots,position=PlayerPosition,health=combat.Health,removedHearts=combat.RemovedHearts,kills=combat.Kills,gameOver=combat.IsGameOver,cycleElapsed=Cycle.Elapsed};
            foreach(var stack in inventory) state.inventory.Add(new ItemStackSave {item=catalog.Id(stack.item),count=stack.count});
            return state;
        }
        public void RestorePlayer(PlayerSave state,SaveItemCatalog catalog)
        {
            inventory.Clear();
            foreach(var stack in state.inventory) inventory.Add(new InventoryStack(catalog.Resolve(stack.item)) {count=stack.count});
            removedSlots=state.removedSlots; selected=state.selected; carriedGold=state.gold; shop?.RestoreGold(state.gold);
            combat.RestorePlayerState(state.health,state.removedHearts,state.kills,state.gameOver);
            shop?.Close(); showInventory=false;
        }
        public void ReconcileRestoredInventory() { DropOverflow(); selected=Mathf.Clamp(selected,0,Capacity-1); }
        public MapSave CaptureMap(SaveItemCatalog catalog,double worldSeconds)
        {
            var state=new MapSave {scene=gameObject.scene.path,updatedAt=worldSeconds,removedObjects=new List<string>(removedMapObjects)};
            foreach(var plot in plots.Values) state.plots.Add(new PlotSave {cell=plot.cell,item=catalog.Id(plot.plant),wet=plot.wet,mature=plot.mature,ready=plot.ready,wilted=plot.wilted,regrowing=plot.regrowing,progress=plot.progress,production=plot.production});
            foreach(var piece in pieces) if(piece.model!=null)
                state.pieces.Add(new PieceSave {item=catalog.Id(piece.definition),position=piece.model.transform.position,size=(Vector2)piece.model.transform.lossyScale*Mathf.Max(piece.model.GetComponent<SpriteRenderer>().sprite.bounds.size.x,piece.model.GetComponent<SpriteRenderer>().sprite.bounds.size.y)});
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) if(building.runtimePlaced)
                state.buildings.Add(new BuildingSave {item=catalog.Id(building.HarvestItem),position=building.placementAnchor});
            return state;
        }
        public void RestoreMap(MapSave state,SaveItemCatalog catalog)
        {
            foreach(var plot in plots.Values) { if(plot.model!=null) Remove(plot.model.gameObject); Remove(plot.soil.gameObject); }
            plots.Clear();
            foreach(var piece in pieces) if(piece.model!=null) Remove(piece.model); pieces.Clear();
            removedMapObjects.Clear(); foreach(var id in state.removedObjects) removedMapObjects.Add(id);
            foreach(var building in GetComponentsInChildren<FarmBuilding>())
                if(building.runtimePlaced || removedMapObjects.Contains(AuthoredKey(building)) || removedMapObjects.Contains(MapObjectId.FallbackKey(building)))
                {
                    if(!building.runtimePlaced) removedMapObjects.Add(AuthoredKey(building));
                    Remove(building.gameObject);
                }
            foreach(var saved in state.plots)
            {
                var plot=new Plot {cell=saved.cell,plant=catalog.Resolve(saved.item),wet=saved.wet,mature=saved.mature,ready=saved.ready,wilted=saved.wilted,regrowing=saved.regrowing,progress=Mathf.Clamp01(saved.progress),production=Mathf.Max(0,saved.production)};
                plot.soil=ArtDraw("Tilled soil",plot.cell,Vector2.one,plot.wet?11:10,new Color(.37f,.23f,.14f),-90); plots[plot.cell]=plot;
                if(plot.plant!=null) { plot.model=Draw("Growing "+plot.plant.label,plot.cell,Vector2.one,Color.white,4); UpdatePlantVisual(plot); }
            }
            foreach(var saved in state.pieces) Spawn(catalog.Resolve(saved.item),saved.position,saved.size);
            foreach(var saved in state.buildings) CreateInstalledHouse(catalog.Resolve(saved.item),saved.position);
        }
    }
}
