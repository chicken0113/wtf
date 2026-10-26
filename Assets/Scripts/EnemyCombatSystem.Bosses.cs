using UnityEngine;
namespace EverythingGrows
{
    internal sealed partial class EnemyCombatSystem
    {
        bool TickBossLifecycle(Enemy enemy,float seconds,MapCollision collision,ProjectileSystem projectiles)
        {
            if(!settings.UsesRegions || enemy.trainingDummy || !enemy.monsterType.isBoss)return false;
            if(enemy.health<=0)
            {
                // The deadline advances in other maps, but the existing daytime-only spawn rule still applies.
                if(farm.Cycle!=null && farm.Cycle.IsDaytime && enemy.bossRespawnAt>0 && GameSession.WorldSeconds>=enemy.bossRespawnAt)
                { ResetBoss(enemy,projectiles,true);enemy.bossRespawnAt=0;GameSession.Save(farm); }
                return true;
            }
            if(enemy.bossReturning)
            {
                if(enemy.sleeping){enemy.model.transform.position=enemy.home;enemy.bossReturning=false;enemy.path.Clear();return true;}
                MoveEnemy(enemy,enemy.home,seconds*enemy.monsterType.returnSpeed,seconds,collision);
                if(Vector2.Distance(enemy.model.transform.position,enemy.home)<.15f)
                { enemy.model.transform.position=enemy.home;enemy.bossReturning=false;enemy.path.Clear(); }
                return true;
            }
            float leash=enemy.monsterType.bossLeashRange;
            if((enemy.bossEngaged || enemy.provoked) && (Vector2.Distance(farm.PlayerPosition,enemy.home)>leash || Vector2.Distance(enemy.model.transform.position,enemy.home)>leash))
            { ResetBoss(enemy,projectiles,false);return true; }
            return false;
        }
        void ResetBoss(Enemy enemy,ProjectileSystem projectiles,bool immediate)
        {
            enemy.health=enemy.MaxHealth;enemy.bossEngaged=enemy.provoked=false;
            enemy.windup=enemy.stagger=enemy.hitTime=enemy.repath=enemy.respawn=0;enemy.cooldown=1;
            enemy.path.Clear();enemy.warning.enabled=false;enemy.model.color=enemy.tint;
            projectiles.ClearOwner(enemy.id);
            enemy.bossReturning=!immediate && Vector2.Distance(enemy.model.transform.position,enemy.home)>.15f;
            if(immediate)enemy.model.transform.position=enemy.home;
            enemy.sleeping=farm.Cycle!=null && !farm.Cycle.IsDaytime;
            enemy.model.enabled=true;
        }
        public void ResetBossEncounters(ProjectileSystem projectiles)
        {
            foreach(var enemy in enemies)if(!enemy.trainingDummy && enemy.monsterType.isBoss && enemy.health>0)
                ResetBoss(enemy,projectiles,true);
        }
    }
}
