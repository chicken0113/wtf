using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    // Owns authored gathering nodes only. Loose drops remain ordinary FarmGame pieces.
    internal sealed class ForageSystem
    {
        sealed class Node
        {
            public FarmItemPlacement marker;
            public ForageNodeSave state;
            public PlantDefinition item;
            public FarmGame.Piece piece;
        }
        readonly FarmGame farm;
        readonly Dictionary<string,Node> nodes=new();
        public ForageSystem(FarmGame farm,IEnumerable<FarmItemPlacement> markers)
        {
            this.farm=farm;
            foreach(var marker in markers.Where(m=>m.forage!=null && m.enabled && m.gameObject.activeInHierarchy))
            {
                var node=new Node {marker=marker,state=new ForageNodeSave {id=MapObjectId.Key(marker)}};
                nodes.Add(node.state.id,node);Refill(node);
            }
        }
        void Refill(Node node)
        {
            node.item=node.marker.forage.item;node.state.remaining=node.marker.forage.quantity;node.state.readyAt=0;Render(node);
        }
        void Render(Node node)
        {
            farm.RemoveForagePiece(node.piece);node.piece=null;
            if(node.state.remaining<=0)return;
            var scale=node.marker.transform.lossyScale;
            node.piece=farm.SpawnForagePiece(node.item,node.marker.transform.position,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)),node.state.id);
        }
        public void Tick()
        {
            foreach(var node in nodes.Values)
                if(node.marker!=null && node.marker.isActiveAndEnabled && node.state.remaining==0 && node.marker.forage.respawns && node.state.readyAt>0 && GameSession.WorldSeconds>=node.state.readyAt)Refill(node);
        }
        public bool Collect(string id)
        {
            if(!nodes.TryGetValue(id,out var node) || node.state.remaining<=0 || !farm.WithinReach(node.marker.transform.position))return false;
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");var before=farm.CapturePlayer(catalog);
            int remaining=node.state.remaining;bool harvested=node.state.harvested;double readyAt=node.state.readyAt;int received=0;
            while(node.state.remaining>0 && farm.AddItem(node.item)){node.state.remaining--;received++;}
            if(received==0)return false;
            if(node.state.remaining==0)
            {
                node.state.harvested=true;node.state.readyAt=node.marker.forage.respawns?GameSession.WorldSeconds+node.marker.forage.respawnSeconds:0;
                Render(node);
            }
            if(!GameSession.Save(farm))
            {
                farm.RestorePlayer(before,catalog);node.state.remaining=remaining;node.state.harvested=harvested;node.state.readyAt=readyAt;Render(node);
                farm.Notify("저장 실패로 채집을 취소했습니다. 저장 재시도 후 다시 주워주세요.");return false;
            }
            farm.Notify(node.state.remaining>0?received+"개를 주웠습니다. 남은 물건은 여기에 유지됩니다.":received+"개를 주웠습니다.");return true;
        }
        public void Capture(MapSave map,SaveItemCatalog catalog)
        {
            foreach(var node in nodes.Values)map.forageNodes.Add(new ForageNodeSave {id=node.state.id,item=catalog.Id(node.item),remaining=node.state.remaining,harvested=node.state.harvested,readyAt=node.state.readyAt});
        }
        public void Restore(MapSave map,SaveItemCatalog catalog)
        {
            foreach(var node in nodes.Values)
            {
                var saved=map.forageNodes?.FirstOrDefault(s=>s.id==node.state.id);
                if(saved==null){node.state.remaining=node.marker.forage.quantity;node.state.harvested=false;node.state.readyAt=0;node.item=node.marker.forage.item;}
                else{node.state=JsonUtility.FromJson<ForageNodeSave>(JsonUtility.ToJson(saved));node.item=catalog.Resolve(saved.item);}
                if(node.state.remaining==0 && node.state.harvested && node.marker.forage.respawns && node.state.readyAt<=0)node.state.readyAt=GameSession.WorldSeconds+node.marker.forage.respawnSeconds;
                Render(node);
            }
            Tick();
        }
    }
}
