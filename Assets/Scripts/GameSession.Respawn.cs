using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EverythingGrows
{
    public sealed partial class GameSession
    {
        static bool pendingRespawn;
        public static bool SetRespawn(FarmGame farm,Vector2 position,string anchor=null)
        {
            if(farm==null || !farm.IsInitialized || Busy || blocked || !Enabled)return false;
            try
            {
                Load();if(!new MapCollision(farm).CanStand(position,farm.PlayerRadius)){farm.Notify("부활할 공간이 없습니다.");return false;}
                var previous=data.respawn;
                data.respawn=new RespawnSave {scene=farm.gameObject.scene.path,position=position,anchor=anchor};
                if(!Save(farm)){data.respawn=previous;return false;}
                farm.Notify("부활 위치를 지정했습니다.");return true;
            }
            catch(Exception problem){error="부활 위치 지정 실패: "+problem.Message;Debug.LogError(error);return false;}
        }
        public static bool RespawnAfterDeath(FarmGame farm)
        {
            if(farm==null || Busy || blocked || !Enabled)return false;
            try
            {
                Load();EnsureDefaultRespawn(farm);
                var destination=data.respawn!=null && data.respawn.IsSet?data.respawn:data.defaultRespawn;
                if(SceneUtility.GetBuildIndexByScenePath(destination.scene)<0)destination=data.defaultRespawn;
                if(destination==null || SceneUtility.GetBuildIndexByScenePath(destination.scene)<0)
                { error="부활할 기본 씬을 빌드 설정에서 찾을 수 없습니다.";return false; }
                farm.Combat.ResetBossEncounters();
                var before=farm.CapturePlayer(catalog);var cache=farm.LeaveDeathCache(catalog);
                if(!Save(farm)){farm.RemoveDeathCache(cache);farm.RestorePlayer(before,catalog);return false;}
                farm.Shop?.Close();farm.SetInventoryOpen(false);farm.SetMapOpen(false);
                if(destination.scene==farm.gameObject.scene.path)
                {
                    farm.TeleportPlayer(ResolveRespawn(farm,destination));
                    Save(farm);return true;
                }
                pendingRespawn=true;
                if(Travel(farm,destination.scene,null,destination.position))return true;
                pendingRespawn=false;farm.Notify("부활 이동 실패. 사망 지점의 물건은 보존되어 있습니다. "+LastError);return false;
            }
            catch(Exception problem){error="부활 처리 실패: "+problem.Message;Debug.LogError(error);return false;}
        }
        static void EnsureDefaultRespawn(FarmGame farm)
        {
            if(data.defaultRespawn==null || !data.defaultRespawn.IsSet)data.defaultRespawn=new RespawnSave {scene=farm.gameObject.scene.path,position=farm.SpawnPosition};
        }
        static Vector2 ResolveRespawn(FarmGame farm,RespawnSave destination)
        {
            var collision=new MapCollision(farm);
            Vector2 position=destination.position;
            if(!string.IsNullOrEmpty(destination.anchor))
            {
                var anchor=farm.GetComponentsInChildren<RespawnAnchor>().FirstOrDefault(a=>a.isActiveAndEnabled && a.Key==destination.anchor);
                if(anchor==null){data.respawn=null;position=farm.SpawnPosition;farm.Notify("지정한 침대가 없어 이 맵의 기본 위치에서 부활했습니다.");}
                else position=anchor.SpawnPosition;
            }
            if(collision.CanStand(position,farm.PlayerRadius))return position;
            // Terrain edits can block a saved position. Search locally before falling back to the map spawn.
            Vector2 origin=farm.SpawnPosition;
            for(float radius=0;radius<=8;radius+=.5f)for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI/8;var point=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                if(collision.CanStand(point,farm.PlayerRadius))return point;
            }
            throw new InvalidOperationException("기본 부활 위치 주변에 빈 공간이 없습니다. 씬의 Player Spawn 배치를 확인하세요.");
        }
        static void ValidateRecovery(GameSaveData candidate)
        {
            bool Finite(Vector2 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y);
            foreach(var spawn in new[]{candidate.respawn,candidate.defaultRespawn})
                if(spawn!=null && spawn.IsSet && (string.IsNullOrWhiteSpace(spawn.scene)||!Finite(spawn.position)))throw new InvalidDataException("Invalid respawn position.");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var map in candidate.maps)
            {
                if(map==null)continue;
                map.deathCaches??=new();map.respawnAnchors??=new();
                foreach(var cache in map.deathCaches)
                {
                    if(cache==null || string.IsNullOrWhiteSpace(cache.id) || !ids.Add(cache.id) || !Finite(cache.position) || cache.items==null || cache.items.Count==0)throw new InvalidDataException("Invalid death cache.");
                    foreach(var stack in cache.items)
                    {
                        if(stack==null || string.IsNullOrWhiteSpace(stack.item) || stack.count<=0)throw new InvalidDataException("Invalid death cache item.");
                        catalog.Resolve(stack.item);
                    }
                }
                var anchorIds=new System.Collections.Generic.HashSet<string>();
                foreach(var anchor in map.respawnAnchors)
                    if(anchor==null || string.IsNullOrWhiteSpace(anchor.id) || !anchorIds.Add(anchor.id) || !Finite(anchor.position))throw new InvalidDataException("Invalid respawn anchor.");
            }
        }
    }
}
