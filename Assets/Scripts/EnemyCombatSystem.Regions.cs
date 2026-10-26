using System;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    internal sealed partial class EnemyCombatSystem
    {
        public void RetireDefeated()
        {
            for(int i=enemies.Count-1;i>=0;i--)if(!enemies[i].trainingDummy && enemies[i].health<=0 && !enemies[i].monsterType.isBoss){RemoveEnemy(enemies[i]);enemies.RemoveAt(i);}
        }
        public int RegionCount(string id)=>enemies.Count(e=>e.regionId==id && !e.trainingDummy && !e.monsterType.isBoss && e.health>0);
        public void ClearRegion(string id)
        {
            for(int i=enemies.Count-1;i>=0;i--)
            {
                var enemy=enemies[i];
                if(enemy.trainingDummy || enemy.monsterType.isBoss || enemy.regionId!=id)continue;
                RemoveEnemy(enemy);enemies.RemoveAt(i);
            }
        }
        void RemoveEnemy(Enemy enemy)
        { FarmWorldView.Remove(enemy.model.gameObject);FarmWorldView.Remove(enemy.warning.gameObject); }
        public void SetRegionalSleep(bool daytime,bool reset)
        {
            foreach(var enemy in enemies)
            {
                if(enemy.trainingDummy)continue;
                if(reset || daytime)enemy.provoked=false;
                enemy.sleeping=!daytime && !enemy.provoked;
                if(enemy.sleeping){enemy.windup=0;enemy.warning.enabled=false;enemy.path.Clear();}
                enemy.model.enabled=enemy.health>0;
            }
        }
        void CaptureRegionalState(MapSave state)
        {
            state.regionalCombatSaved=true;state.combatCleared=false;
            foreach(var enemy in enemies)
            {
                if(enemy.trainingDummy || (enemy.health<=0 && !enemy.monsterType.isBoss))continue;
                state.regionalEnemies.Add(new RegionalEnemySave {
                    id=enemy.id,type=enemy.monsterType.SaveKey,region=enemy.regionId,placementId=enemy.placementId,
                    position=enemy.model.transform.position,home=enemy.home,size=enemy.displaySize,
                    health=enemy.health,provoked=enemy.provoked });
            }
        }
        void RestoreRegionalState(MapSave state)
        {
            FloorCleared=false;defeatedPlacements.Clear();
            if(!state.regionalCombatSaved)return; // Older floor saves become a new regional population once.
            for(int i=enemies.Count-1;i>=0;i--)if(!enemies[i].trainingDummy){RemoveEnemy(enemies[i]);enemies.RemoveAt(i);}
            var types=Resources.LoadAll<MonsterType>("");
            foreach(var saved in state.regionalEnemies)
            {
                var type=types.FirstOrDefault(t=>t.SaveKey==saved.type);
                if(type==null)throw new InvalidOperationException("저장된 몬스터 종류가 없습니다: "+saved.type);
                Spawn(saved.position,type.IsRanged(false),saved.size,false,saved.placementId,type,saved.region);
                var enemy=enemies[enemies.Count-1];enemy.id=saved.id;enemy.home=saved.home;
                enemy.health=Mathf.Min(saved.health,enemy.MaxHealth);enemy.provoked=saved.provoked;
                enemy.sleeping=farm.Cycle!=null && !farm.Cycle.IsDaytime && !enemy.provoked;
                enemy.model.enabled=enemy.health>0;
            }
        }
    }
}
