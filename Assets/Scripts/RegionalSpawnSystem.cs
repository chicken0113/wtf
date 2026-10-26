using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    // Region population lifecycle. EnemyCombatSystem continues to own individual combat behavior.
    internal sealed class RegionalSpawnSystem
    {
        readonly FarmGame farm;
        readonly SpawnRegion[] regions;
        readonly MonsterType[] types;
        readonly Dictionary<string,float> timers=new();
        public bool Enabled => regions.Length>0;
        public bool Daytime { get; private set; }=true;
        public RegionalSpawnSystem(FarmGame farm)
        {
            this.farm=farm;regions=SpawnRegion.ForScene(farm.gameObject.scene.path);types=Resources.LoadAll<MonsterType>("");
            foreach(var region in regions)timers[region.id]=0;
        }
        public string RegionAt(Vector2 point)=>regions.FirstOrDefault(r=>r.Bounds(farm).Contains(point))?.id;
        public void SetDaytime(bool daytime,EnemyCombatSystem enemies,bool restoring)
        {
            bool transition=Daytime!=daytime;Daytime=daytime;
            enemies.SetRegionalSleep(daytime,transition && !restoring);
        }
        public void Tick(float seconds,EnemyCombatSystem enemies)
        {
            if(!Enabled)return;
            enemies.RetireDefeated();
            if(!Daytime)return;
            var collision=new MapCollision(farm);
            foreach(var region in regions)
            {
                var bounds=region.Bounds(farm);bool inside=bounds.Contains(farm.PlayerPosition);
                if(!inside)
                {
                    if(!bounds.Overlaps(SpawnRegion.CameraBounds(Camera.main,region.viewPadding)))
                    { enemies.ClearRegion(region.id);timers[region.id]=0; }
                    continue;
                }
                timers[region.id]-=seconds;if(timers[region.id]>0)continue;
                timers[region.id]=region.spawnInterval;
                if(enemies.RegionCount(region.id)>=region.maxAlive)continue;
                var eligible=types.Where(t=>!t.isBoss && t.spawnRegions!=null && t.spawnRegions.Contains(region)).ToArray();
                if(eligible.Length==0)continue;
                for(int attempt=0;attempt<32;attempt++)
                {
                    Vector2 point=new(UnityEngine.Random.Range(bounds.xMin,bounds.xMax),UnityEngine.Random.Range(bounds.yMin,bounds.yMax));
                    if(!collision.CanStand(point,enemies.Radius) || Vector2.Distance(point,farm.PlayerPosition)<region.playerExclusionRadius)continue;
                    if(enemies.Items.Any(e=>e.health>0 && Vector2.Distance(e.model.transform.position,point)<enemies.Radius*2))continue;
                    var type=eligible[UnityEngine.Random.Range(0,eligible.Length)];
                    enemies.Spawn(point,type.IsRanged(false),monsterType:type,regionId:region.id);break;
                }
            }
        }
    }
}
