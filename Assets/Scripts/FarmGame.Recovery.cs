using System;
using System.Collections.Generic;
using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private readonly List<DeathCacheSave> deathCaches=new();
        private readonly Dictionary<string,GameObject> deathCacheModels=new();
        internal DeathCacheSave LeaveDeathCache(SaveItemCatalog catalog)
        {
            var playerState=CapturePlayer(catalog);
            if(playerState.inventory.Count==0)return null;
            var cache=new DeathCacheSave {id=Guid.NewGuid().ToString("N"),position=PlayerPosition,items=playerState.inventory};
            var empty=new PlayerSave {selected=playerState.selected,removedSlots=playerState.removedSlots,inventoryUsesSlots=true};
            inventory.Restore(empty,catalog);deathCaches.Add(cache);DrawDeathCache(cache);return cache;
        }
        internal void RemoveDeathCache(DeathCacheSave cache)
        {
            if(cache==null)return;
            if(deathCacheModels.Remove(cache.id,out var model) && model!=null)Remove(model);
            deathCaches.Remove(cache);
        }
        private void DrawDeathCache(DeathCacheSave cache)
        {
            var marker=Draw("Dropped belongings",cache.position,Vector2.one*.65f,new Color(.85f,.57f,.18f),15);
            var strap=Draw("Bag strap",cache.position+Vector2.up*.18f,new Vector2(.14f,.55f),new Color(.35f,.22f,.12f),16);
            strap.transform.SetParent(marker.transform,true);deathCacheModels[cache.id]=marker.gameObject;
        }
        private bool RecoverDeathCache(Vector2 point)
        {
            DeathCacheSave cache=null;float nearest=.8f;
            foreach(var candidate in deathCaches) { float distance=Vector2.Distance(candidate.position,point);if(distance<nearest){cache=candidate;nearest=distance;} }
            if(cache==null)return false;
            if(!WithinReach(cache.position)){Notify("물건에 가까이 다가가세요.");return true;}
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");var before=CapturePlayer(catalog);
            var saved=JsonUtility.FromJson<DeathCacheSave>(JsonUtility.ToJson(cache));int recovered=0;
            foreach(var stack in cache.items)
            {
                var item=catalog.Resolve(stack.item);
                while(stack.count>0 && inventory.TryAdd(item,Capacity)){stack.count--;recovered++;}
            }
            cache.items.RemoveAll(stack=>stack.count==0);
            if(recovered==0){Notify("가방에 빈 공간이 없습니다.");return true;}
            if(cache.items.Count==0)RemoveDeathCache(cache);
            if(!GameSession.Save(this))
            {
                RestorePlayer(before,catalog);RemoveDeathCache(cache);deathCaches.Add(saved);DrawDeathCache(saved);
                Notify("저장 실패로 회수를 취소했습니다. 저장 재시도 후 다시 회수하세요.");return true;
            }
            Notify(cache.items.Count==0?"사망 지점의 물건을 모두 회수했습니다.":"가방에 들어가지 못한 물건은 여기에 남아 있습니다.");return true;
        }
        internal RespawnAnchor CreateRespawnAnchor(Vector2 point,string id=null)
        {
            var bed=Draw("Camping bed",point,new Vector2(.9f,1.3f),new Color(.25f,.52f,.7f),5);
            if(mapLayout!=null)bed.transform.SetParent(mapLayout.transform,true);
            var pillow=Draw("Pillow",point+Vector2.up*.4f,new Vector2(.7f,.3f),Color.white,6);pillow.transform.SetParent(bed.transform,true);
            if(Presentation3D!=null){Presentation3D.Show(bed,ModelKind3D.Bed,true);pillow.enabled=false;}
            var anchor=bed.gameObject.AddComponent<RespawnAnchor>();anchor.runtimePlaced=true;anchor.runtimeId=id??Guid.NewGuid().ToString("N");return anchor;
        }
        public bool UseRespawnItem(PlantDefinition item,Vector2 target,bool placeAnchor)
        {
            if(item==null || Held!=item || !GameSession.Enabled)return false;
            var point=placeAnchor?target:PlayerPosition;
            var collision=new MapCollision(this);
            if(!WithinReach(point) || !collision.CanStand(point,placeAnchor ? .8f : PlayerRadius) || (placeAnchor && !collision.CanStand(point+Vector2.down,PlayerRadius)))
            { Notify("가까운 빈 땅에 부활 위치를 지정하세요.");return false; }
            if(placeAnchor)
            {
                foreach(var anchor in GetComponentsInChildren<RespawnAnchor>())if(Vector2.Distance(anchor.transform.position,point)<1.5f){Notify("기존 침대와 겹칩니다.");return false;}
                foreach(var plot in farming.Plots)if(plot.plant!=null && Vector2.Distance(plot.cell,point)<1.2f){Notify("심어진 물건과 겹칩니다.");return false;}
            }
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");var before=CapturePlayer(catalog);
            var bed=placeAnchor?CreateRespawnAnchor(point):null;
            RemoveInventoryItem(selected);
            if(GameSession.SetRespawn(this,bed!=null?bed.SpawnPosition:point,bed!=null?bed.Key:null))return true;
            if(bed!=null)Remove(bed.gameObject);RestorePlayer(before,catalog);return false;
        }
        private bool InteractRecovery(Vector2 point)
        {
            if(RecoverDeathCache(point))return true;
            foreach(var anchor in GetComponentsInChildren<RespawnAnchor>())
            {
                if(!anchor.isActiveAndEnabled || !anchor.Contains(point))continue;
                if(!WithinReach(anchor.transform.position))Notify("침대에 가까이 다가가세요.");
                else GameSession.SetRespawn(this,anchor.SpawnPosition,anchor.Key);
                return true;
            }
            return false;
        }
        internal void CaptureRecovery(MapSave state)
        {
            state.deathCaches=JsonUtility.FromJson<MapSave>(JsonUtility.ToJson(new MapSave {deathCaches=deathCaches})).deathCaches;
            foreach(var anchor in GetComponentsInChildren<RespawnAnchor>())if(anchor.runtimePlaced && anchor.isActiveAndEnabled)
                state.respawnAnchors.Add(new RespawnAnchorSave {id=anchor.Key,position=anchor.transform.position});
        }
        internal void RestoreRecovery(MapSave state)
        {
            foreach(var cache in deathCaches.ToArray())RemoveDeathCache(cache);
            foreach(var anchor in GetComponentsInChildren<RespawnAnchor>())if(anchor.runtimePlaced)Remove(anchor.gameObject);
            var copy=JsonUtility.FromJson<MapSave>(JsonUtility.ToJson(state));
            foreach(var cache in copy.deathCaches){deathCaches.Add(cache);DrawDeathCache(cache);}
            foreach(var anchor in copy.respawnAnchors)CreateRespawnAnchor(anchor.position,anchor.id);
        }
    }
}
