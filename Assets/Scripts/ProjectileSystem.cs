using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    internal sealed class ProjectileSystem
    {
        private sealed class Shot { public SpriteRenderer model; public Vector2 velocity; public float damage,life=2.5f; public bool hostile,cancelled; public string ownerId; }
        private readonly FarmGame farm;
        private readonly CombatEffects effects;
        private readonly List<Shot> shots = new();
        public ProjectileSystem(FarmGame farm, CombatEffects effects) { this.farm=farm;this.effects=effects; }
        public void Spawn(Vector2 point,Vector2 velocity,float damage,bool hostile,float life=2.5f) => SpawnOwned(point,velocity,damage,hostile,life,null);
        public void SpawnOwned(Vector2 point,Vector2 velocity,float damage,bool hostile,float life,string ownerId)
        {
            var model=effects.Draw(hostile?"Enemy shot":"Player shot",point,hostile?Vector2.one*.22f:new Vector2(.3f,.12f),hostile?new Color(1,.35f,.25f):new Color(1,.9f,.45f));
            model.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
            shots.Add(new Shot { ownerId=ownerId,model=model,velocity=velocity,damage=damage,hostile=hostile,life=Mathf.Max(.001f,life) });
        }
        public void ClearOwner(string ownerId)
        {
            // Mark instead of removing: a lethal shot may trigger boss reset during this list's own iteration.
            foreach(var shot in shots)if(shot.ownerId==ownerId){shot.cancelled=true;shot.life=0;shot.model.enabled=false;}
        }
        public void ClearHostile()
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                if(!shots[i].hostile) continue;
                FarmWorldView.Remove(shots[i].model.gameObject);
                shots.RemoveAt(i);
            }
        }
        public void Tick(float seconds, MapCollision collision, EnemyCombatSystem enemies, PlayerCombatState player)
        {
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i];
                if(shot.cancelled){FarmWorldView.Remove(shot.model.gameObject);shots.RemoveAt(i);continue;}
                Vector2 start=shot.model.transform.position, end=start+shot.velocity*Mathf.Min(seconds,shot.life);
                shot.life-=seconds; float radius=shot.hostile?.11f:.06f;
                float wall=collision.Trace(start,end,radius,true), first=wall;
                EnemyCombatSystem.Enemy victim=null; bool playerHit=false;
                if(shot.hostile)
                {
                    if(!farm.Combat.UsesRegions && MapCollision.Intersect(start,end,farm.SafeBounds,out float safeEntry)) first=Mathf.Min(first,safeEntry);
                    if(!farm.IsSafe(farm.PlayerPosition) && SegmentCircle(start,end,farm.PlayerPosition,farm.PlayerRadius+radius,out float entry) && entry<first)
                    { first=entry; playerHit=true; }
                }
                else foreach(var enemy in enemies.Items)
                {
                    if(enemies.Active(enemy) && enemy.health>0 && SegmentCircle(start,end,enemy.model.transform.position,enemies.Radius+radius,out float entry) && entry<first)
                    { first=entry; victim=enemy; }
                }
                if(playerHit) player.Damage(shot.damage);
                if(victim!=null) enemies.Hit(victim,shot.damage,shot.velocity.normalized);
                shot.model.transform.position=Vector2.Lerp(start,end,first);
                if(first<1 || shot.life<=0) { FarmWorldView.Remove(shot.model.gameObject); shots.RemoveAt(i); }
            }
        }
        public static bool SegmentCircle(Vector2 start,Vector2 end,Vector2 center,float radius,out float entry)
        {
            entry=0; Vector2 delta=end-start,offset=start-center;
            float c=offset.sqrMagnitude-radius*radius; if(c<=0) return true;
            float a=delta.sqrMagnitude; if(a<.000001f) return false;
            float b=Vector2.Dot(offset,delta),discriminant=b*b-a*c;
            if(discriminant<0) return false;
            entry=(-b-Mathf.Sqrt(discriminant))/a; return entry>=0 && entry<=1;
        }
        public static float SegmentDistance(Vector2 start,Vector2 end,Vector2 point)
        {
            Vector2 segment=end-start; float t=segment.sqrMagnitude<.0001f?0:Mathf.Clamp01(Vector2.Dot(point-start,segment)/segment.sqrMagnitude);
            return Vector2.Distance(start+segment*t,point);
        }
    }
}
