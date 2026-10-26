using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        internal sealed class Piece { public PlantDefinition definition; public GameObject model; }
        private readonly List<Piece> pieces = new();
        internal IEnumerable<Piece> WorldPieces => pieces;
        public int WorldPieceCount => pieces.Count;
        public void DropItem(PlantDefinition definition,Vector2 point) => Spawn(definition,point);
        private void Spawn(PlantDefinition definition,Vector2 point,Vector2? size=null)
        {
            var renderer=ArtDraw(definition.label,point,size??Vector2.one*.8f,art.ItemIndex(definition),definition.color,3);
            renderer.sprite=art.ItemSprite(definition); renderer.color=Color.white;
            float longest=Mathf.Max(renderer.sprite.bounds.size.x,renderer.sprite.bounds.size.y);
            renderer.transform.localScale=new Vector3((size??Vector2.one*.8f).x/longest,(size??Vector2.one*.8f).y/longest,1);
            pieces.Add(new Piece { definition=definition,model=renderer.gameObject });
        }
        internal bool CanFarmAt(Vector2Int cell)
        {
            if(!new MapCollision(this).CanStand(cell,.45f))return false;
            foreach(var anchor in GetComponentsInChildren<RespawnAnchor>())if(anchor.isActiveAndEnabled && anchor.Contains(cell))return false;
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) if(building.Blocks(cell)) return false;
            return mapLayout!=null?mapLayout.CanFarm(cell):cell.x>=-23 && cell.x<=23 && cell.y>=-15 && cell.y<=15;
        }
        public bool DetachAt(Vector2 point)
        {
            var nearest=PieceAt(point);
            if(nearest==null || !AddItem(nearest.definition)) return false;
            Remove(nearest.model); pieces.Remove(nearest); message="Detached "+nearest.definition.label+". Its old place is empty."; return true;
        }
        private Piece PieceAt(Vector2 point)
        {
            Piece nearest=null; float distance=.65f;
            foreach(var piece in pieces) if(piece.model!=null) { float d=Vector2.Distance(piece.model.transform.position,point); if(d<distance) { distance=d; nearest=piece; } }
            return nearest;
        }
        private Rect HouseFootprint(PlantDefinition item,Vector2 point)
        {
            var source=art.ItemSprite(item); float height=item.matureDisplaySize*source.bounds.size.y/Mathf.Max(source.bounds.size.x,source.bounds.size.y);
            return new Rect(point+new Vector2(-item.matureDisplaySize*.425f,-.18f),new Vector2(item.matureDisplaySize*.85f,height*.32f));
        }
        public bool CanPlaceHouse(PlantDefinition item,Vector2 point)
        {
            if(item==null || item.effect!=GrowthEffect.House || combat.IsGameOver || !Near(point)) return false;
            var footprint=HouseFootprint(item,point);
            if(!new MapCollision(this).CanStand(new Vector2(footprint.center.x,footprint.yMin-.7f),playerRadius)) return false;
            var source=art.ItemSprite(item); float height=item.matureDisplaySize*source.bounds.size.y/Mathf.Max(source.bounds.size.x,source.bounds.size.y);
            var visual=new Rect(point+new Vector2(-item.matureDisplaySize*.5f,-.18f),new Vector2(item.matureDisplaySize,height));
            if(!WorldBounds.Contains(visual.min) || !WorldBounds.Contains(visual.max) || footprint.Overlaps(new Rect(PlayerPosition-Vector2.one*playerRadius,Vector2.one*playerRadius*2))) return false;
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) if(footprint.Overlaps(building.WorldFootprint)) return false;
            foreach(var plot in farming.Plots) if(plot.plant!=null && footprint.Overlaps(new Rect((Vector2)plot.cell-Vector2.one*.4f,Vector2.one*.8f))) return false;
            foreach(var npc in GetComponentsInChildren<ShopNpc>()) if(npc.IsVisiting && footprint.Contains(npc.transform.position)) return false;
            return true;
        }
        public bool PlaceHouse(Vector2 point)
        {
            var item=Held;
            if(!CanPlaceHouse(item,point)) { message="Cannot install here. Keep the footprint clear and stand outside it."; return false; }
            CreateInstalledHouse(item,point);
            RemoveInventoryItem(selected); message="House installed."; return true;
        }
        private FarmBuilding CreateInstalledHouse(PlantDefinition item,Vector2 point)
        {
            var source=art.ItemSprite(item); float scale=item.matureDisplaySize/Mathf.Max(source.bounds.size.x,source.bounds.size.y);
            float height=source.bounds.size.y*scale;
            var renderer=Draw("Placed "+item.label,point+Vector2.up*(height*.5f-.18f),Vector2.one*scale,Color.white,10); renderer.sprite=source;
            if(mapLayout!=null) renderer.transform.SetParent(mapLayout.transform,true);
            var building=renderer.gameObject.AddComponent<FarmBuilding>(); building.item=item; building.runtimePlaced=true; building.placementAnchor=point;
            building.footprint=new Vector2(item.matureDisplaySize*.85f,height*.32f)/scale;
            building.offset=new Vector2(0,-height*.34f)/scale; HouseInteriorLink.EnsureDoor(building); return building;
        }
        public bool HarvestBuildingAt(Vector2 point)
        {
            if(combat.IsGameOver || !Near(point)) return false;
            foreach(var building in GetComponentsInChildren<FarmBuilding>())
            {
                if(!building.Contains(point)) continue;
                foreach(var plot in farming.Plots) if(worldView.PlantModel(plot.cell)!=null && worldView.PlantModel(plot.cell).gameObject==building.gameObject) return Uproot(plot.cell);
                var item=building.HarvestItem;
                if(!building.runtimePlaced) removedMapObjects.Add(AuthoredKey(building));
                Remove(building.gameObject); DropOverflow(); return ReceiveHarvest(item);
            }
            return false;
        }
    }
}
