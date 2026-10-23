using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Owns enemies, targeting, navigation, hit reactions and respawn timers.
    internal sealed class EnemyCombatSystem
    {
        private readonly FarmGame farm;
        private readonly OpenFieldCombat settings;
        private readonly CombatEffects effects;
        private readonly Action<Enemy> defeated;
        private readonly List<Enemy> enemies = new();
        private bool nightActive,hasPlacedHostiles;
        private readonly HashSet<string> defeatedPlacements=new();
        public bool FloorCleared { get; private set; }
        public IReadOnlyList<Enemy> Items => enemies;
        public float Radius => settings.EnemyRadius;
        public int LivingHostileCount { get { int count=0;foreach(var enemy in enemies)if(!enemy.trainingDummy && enemy.health>0)count++;return count; } }
        public int EnemyCount { get { int count=0;foreach(var enemy in enemies)if(nightActive&&enemy.health>0)count++;return count; } }
        public EnemyCombatSystem(FarmGame farm,OpenFieldCombat settings,CombatEffects effects,Action<Enemy> defeated)
        { this.farm=farm;this.settings=settings;this.effects=effects;this.defeated=defeated; }
        internal sealed class Enemy
        {
            public Vector2 home, aim, pathGoal;
            public string placementId;
            public MonsterType monsterType;
            public float MaxHealth => monsterType!=null?monsterType.maxHealth:55;
            public List<Vector2> path=new();
            public float repath;
            public SpriteRenderer model, warning;
            public bool ranged, trainingDummy;
            public float health=55,cooldown=1,windup,respawn,hitTime,stagger;
            public Color tint;
        }
        public bool Active(Enemy enemy) => nightActive || enemy.trainingDummy;
        public void Spawn(Vector2 point,bool ranged,Vector2? size=null,bool trainingDummy=false,string placementId=null,MonsterType monsterType=null)
        {
            if((farm.IsSafe(point) || FloorCleared) && !trainingDummy) return;
            if(!trainingDummy && !string.IsNullOrEmpty(placementId))hasPlacedHostiles=true;
            var type=trainingDummy?null:monsterType!=null?monsterType:MonsterType.Default(ranged);
            if(type!=null)ranged=type.IsRanged(ranged);
            var enemy=new Enemy { monsterType=type, placementId=placementId, home=point,ranged=ranged,trainingDummy=trainingDummy,model=effects.Draw(trainingDummy?"Training dummy":ranged?"Spitter":"Chaser",point,size??Vector2.one*1.2f,trainingDummy?new Color(.85f,.65f,.32f):Color.white,trainingDummy?0:ranged?21:20),warning=effects.Draw("Attack warning",point,Vector2.one*.15f,Color.yellow) };
            enemy.health=enemy.MaxHealth;
            enemy.model.enabled=Active(enemy); enemy.warning.enabled=false; enemies.Add(enemy);
            enemy.tint=enemy.model.color;
        }
        public void CaptureState(MapSave state)
        {
            state.combatCleared=FloorCleared;
            state.defeatedEnemies=new List<string>(defeatedPlacements);
            state.defeatedEnemies.Sort(System.StringComparer.Ordinal);
        }
        public void RestoreState(MapSave state)
        {
            FloorCleared=state.combatCleared;
            defeatedPlacements.Clear();
            if(state.defeatedEnemies!=null)foreach(var id in state.defeatedEnemies)defeatedPlacements.Add(id);
            foreach(var enemy in enemies)
            {
                if(enemy.trainingDummy || (!FloorCleared && !defeatedPlacements.Contains(enemy.placementId??"")))continue;
                enemy.health=0;enemy.windup=0;enemy.path.Clear();enemy.warning.enabled=false;enemy.model.enabled=false;
            }
            if(hasPlacedHostiles && LivingHostileCount==0)FloorCleared=true;
        }
        public bool SetNightActive(bool active)
        {
            if(nightActive==active) return false;
            nightActive=active;
            foreach(var enemy in enemies)
            {
                if(enemy.trainingDummy) continue;
                enemy.windup=0; enemy.warning.enabled=false; enemy.path.Clear(); enemy.repath=0;
                if(active && enemy.health>0) { enemy.health=enemy.MaxHealth; enemy.model.transform.position=enemy.home; enemy.cooldown=1; enemy.respawn=0; }
                enemy.model.enabled=active && enemy.health>0;
            }
            return true;
        }
        public void Hit(Enemy enemy,float damage,Vector2 direction)
        {
            if(enemy.health<=0 || damage<=0)return;
            enemy.health-=damage;
            enemy.hitTime=.1f;enemy.stagger=.08f;enemy.windup=0;enemy.warning.enabled=false;enemy.model.color=new Color(1,.4f,.35f);
            effects.Sparks(enemy.model.transform.position,new Color(1,.9f,.65f));farm.ImpactFeedback(false);
            if(enemy.health<=0)
            {
                enemy.health=0; enemy.model.enabled=false; enemy.warning.enabled=false; enemy.windup=0; enemy.respawn=0;
                if(enemy.trainingDummy) enemy.respawn=3;
                else
                {
                    if(!string.IsNullOrEmpty(enemy.placementId))defeatedPlacements.Add(enemy.placementId);
                    if(hasPlacedHostiles && LivingHostileCount==0)FloorCleared=true;
                    defeated(enemy);
                }
            }
            else if(!enemy.trainingDummy)
            {
                Vector2 pushed=(Vector2)enemy.model.transform.position+direction*.35f;
                enemy.model.transform.position=new MapCollision(farm,true).Move(enemy.model.transform.position,pushed,Radius);
            }
        }
        public void HitMelee(PlantDefinition weapon,Vector2 direction)
        {
            var attackCollision=new MapCollision(farm);
            foreach(var enemy in enemies)
            {
                Vector2 offset=(Vector2)enemy.model.transform.position-farm.PlayerPosition;
                if(Active(enemy) && enemy.health>0 && offset.magnitude<=weapon.attackRange+.35f && attackCollision.Trace(farm.PlayerPosition,enemy.model.transform.position,0,true)>=1 && (offset.magnitude<.35f || Vector2.Dot(direction,offset.normalized)>.35f)) Hit(enemy,weapon.damage,direction);
            }
        }
        public void Tick(float seconds,MapCollision collision,PlayerCombatState player,ProjectileSystem projectiles)
        {
            var enemyCollision=new MapCollision(farm,true);
            foreach(var enemy in enemies) TickEnemy(enemy,seconds,collision,enemyCollision,player,projectiles);
            SeparateEnemies(seconds,enemyCollision);
        }
        private void TickEnemy(Enemy enemy,float seconds,MapCollision collision,MapCollision enemyCollision,PlayerCombatState player,ProjectileSystem projectiles)
        {
            if(!Active(enemy)) return;
            enemy.hitTime=Mathf.Max(0,enemy.hitTime-seconds);enemy.stagger=Mathf.Max(0,enemy.stagger-seconds);
            enemy.model.color=Color.Lerp(enemy.tint,new Color(1,.4f,.35f),Mathf.Clamp01(enemy.hitTime/.1f));
            if(enemy.health<=0)
            {
                if(!enemy.trainingDummy)return;
                enemy.respawn-=seconds;
                if(enemy.respawn<=0) { enemy.health=enemy.MaxHealth; enemy.model.transform.position=enemy.home; enemy.model.enabled=true; enemy.cooldown=1; }
                return;
            }
            Vector2 point=enemy.model.transform.position, offset=farm.PlayerPosition-point;
            if(enemy.trainingDummy) { enemy.model.sortingOrder=30-Mathf.RoundToInt(point.y*10); return; }
            if(enemy.stagger>0) return;
            bool active=!farm.IsSafe(farm.PlayerPosition) && offset.magnitude<enemy.monsterType.detectionRange;
            enemy.model.sortingOrder=30-Mathf.RoundToInt(point.y*10);
            enemy.model.flipX=offset.x<0;
            if(!active) { enemy.windup=0; enemy.warning.enabled=false; MoveEnemy(enemy,enemy.home,seconds*enemy.monsterType.returnSpeed,seconds,enemyCollision); enemy.cooldown=Mathf.Max(.5f,enemy.cooldown-seconds); return; }
            if(enemy.windup>0)
            {
                enemy.windup-=seconds; enemy.warning.enabled=true;
                effects.AttackWarning(enemy.warning,point,enemy.aim,enemy.ranged,enemy.model.sortingOrder);
                if(enemy.windup<=0)
                {
                    enemy.warning.enabled=false;
                    if(enemy.ranged) projectiles.Spawn(point,enemy.aim*enemy.monsterType.projectileSpeed,player.HalfHeartHealth*enemy.monsterType.attackHalfHearts,true,enemy.monsterType.projectileLifetime);
                    else if(offset.magnitude<enemy.monsterType.meleeHitRange && collision.Trace(point,farm.PlayerPosition,0,true)>=1) player.Damage(player.HalfHeartHealth*enemy.monsterType.attackHalfHearts);
                    enemy.cooldown=enemy.monsterType.attackCooldown;
                }
                return;
            }
            enemy.cooldown-=seconds;
            float reach=enemy.monsterType.attackRange;
            if(offset.magnitude<=reach && enemy.cooldown<=0 && collision.Trace(point,farm.PlayerPosition,.1f,true)>=1) { enemy.windup=Mathf.Max(.001f,enemy.monsterType.windupSeconds); enemy.aim=offset.normalized; }
            else if(offset.magnitude>reach*.85f || collision.Trace(point,farm.PlayerPosition,.1f,true)<1)
            {
                MoveEnemy(enemy,farm.PlayerPosition,seconds*enemy.monsterType.moveSpeed,seconds,enemyCollision);
            }
        }
        private void MoveEnemy(Enemy enemy,Vector2 target,float distance,float seconds,MapCollision enemyCollision)
        {
            Vector2 start=enemy.model.transform.position;
            enemy.repath-=seconds;
            Vector2 waypoint=target;
            if(enemyCollision.Trace(start,target,Radius)<1)
            {
                if(enemy.repath<=0 || Vector2.Distance(enemy.pathGoal,target)>.75f)
                { enemy.path=enemyCollision.FindPath(start,target,Radius); enemy.pathGoal=target; enemy.repath=.6f; }
                while(enemy.path.Count>0 && Vector2.Distance(start,enemy.path[0])<.12f) enemy.path.RemoveAt(0);
                if(enemy.path.Count==0) return;
                waypoint=enemy.path[0];
            }
            else { enemy.path.Clear(); enemy.repath=0; }
            enemy.model.transform.position=enemyCollision.Move(start,Vector2.MoveTowards(start,waypoint,distance),Radius);
        }
        private void SeparateEnemies(float seconds,MapCollision enemyCollision)
        {
            if(!nightActive) return;
            for(int i=0;i<enemies.Count;i++) for(int j=i+1;j<enemies.Count;j++)
            {
                var a=enemies[i]; var b=enemies[j]; if(a.health<=0 || b.health<=0 || a.trainingDummy || b.trainingDummy) continue;
                Vector2 pa=a.model.transform.position,pb=b.model.transform.position,delta=pa-pb;
                float distance=delta.magnitude; if(distance>=Radius*2) continue;
                Vector2 direction=distance>.001f?delta/distance:((i+j)%2==0?Vector2.right:Vector2.up);
                float push=Mathf.Min((Radius*2-distance)*.5f,seconds*2);
                a.model.transform.position=enemyCollision.Move(pa,pa+direction*push,Radius);
                b.model.transform.position=enemyCollision.Move(pb,pb-direction*push,Radius);
            }
        }
    }
}
