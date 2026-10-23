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
            var state=new PlayerSave {gold=Gold,position=PlayerPosition,health=combat.Health,removedHearts=combat.RemovedHearts,kills=combat.Kills,gameOver=combat.IsGameOver,cycleElapsed=Cycle.Elapsed};
            state.farmingExperience=progression.TotalExperience;
            inventory.Capture(state,catalog);
            return state;
        }
        public void RestorePlayer(PlayerSave state,SaveItemCatalog catalog)
        {
            if(state.farmingExperience<0)throw new System.InvalidOperationException("Invalid farming experience in player save.");
            inventory.Restore(state,catalog);
            progression.Restore(state.farmingExperience);
            carriedGold=state.gold; shop?.RestoreGold(state.gold);
            combat.RestorePlayerState(state.health,state.removedHearts,state.kills,state.gameOver);
            shop?.Close(); showInventory=false;
        }
        public void ReconcileRestoredInventory() { DropOverflow(); inventory.ClampSelection(Capacity); }
        public MapSave CaptureMap(SaveItemCatalog catalog,double worldSeconds)
        {
            var state=new MapSave {scene=gameObject.scene.path,updatedAt=worldSeconds,removedObjects=new List<string>(removedMapObjects)};
            state.plots=farming.CapturePlots(catalog);
            combat.CaptureMapState(state);
            foreach(var piece in pieces) if(piece.model!=null)
                state.pieces.Add(new PieceSave {item=catalog.Id(piece.definition),position=piece.model.transform.position,size=(Vector2)piece.model.transform.lossyScale*Mathf.Max(piece.model.GetComponent<SpriteRenderer>().sprite.bounds.size.x,piece.model.GetComponent<SpriteRenderer>().sprite.bounds.size.y)});
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) if(building.runtimePlaced)
                state.buildings.Add(new BuildingSave {item=catalog.Id(building.HarvestItem),position=building.placementAnchor});
            return state;
        }
        public void RestoreMap(MapSave state,SaveItemCatalog catalog)
        {
            combat.RestoreMapState(state);
            foreach(var piece in pieces) if(piece.model!=null) Remove(piece.model); pieces.Clear();
            removedMapObjects.Clear(); foreach(var id in state.removedObjects) removedMapObjects.Add(id);
            foreach(var building in GetComponentsInChildren<FarmBuilding>())
                if(building.runtimePlaced || removedMapObjects.Contains(AuthoredKey(building)) || removedMapObjects.Contains(MapObjectId.FallbackKey(building)))
                {
                    if(!building.runtimePlaced) removedMapObjects.Add(AuthoredKey(building));
                    Remove(building.gameObject);
                }
            farming.RestorePlots(state.plots,catalog);
            foreach(var saved in state.pieces) Spawn(catalog.Resolve(saved.item),saved.position,saved.size);
            foreach(var saved in state.buildings) CreateInstalledHouse(catalog.Resolve(saved.item),saved.position);
        }
    }
}
