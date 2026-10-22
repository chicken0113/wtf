using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    public sealed class OpenFieldCombat : MonoBehaviour
    {
        private FarmGame farm;
        private FarmArt art;
        private Sprite square;
        private float attackCooldown, invulnerability, rollTime, rollCooldown, quietTime, rollDurationAtStart, playerHitTime;
        private Vector2 rollDirection;
        private Vector2 bufferedRollDirection;
        private float rollBufferTime;
        private bool nightActive;
        [SerializeField,Min(.1f)] private float enemyRadius=.35f;
        private MapCollision collision;
        private MapCollision enemyCollision;
        private readonly List<Enemy> enemies=new();
        private readonly List<Shot> shots=new();
        private readonly List<Flash> flashes=new();
        [SerializeField,Range(1,5)] private int startingHearts=5;
        [SerializeField,Min(1)] private float healthPerHeart=20;
        private int removedHearts;
        public int HeartCount => Mathf.Max(0,startingHearts-removedHearts);
        public bool IsGameOver { get; private set; }
        public float MaxHealth => HeartCount*healthPerHeart;
        public float HalfHeartHealth => healthPerHeart*.5f;
        public float HeartFill(int index)
        {
            float filledHalves=Mathf.Floor(Health/HalfHeartHealth+.0001f);
            return Mathf.Clamp01((filledHalves-index*2)*.5f);
        }
        public float Health { get; private set; } = 100;
        public Rect HudRect => new Rect(14,14,HeartCount*38,32);
        public Rect HeartRect(int index) => new Rect(HudRect.x+2+index*38,HudRect.y+2,32,28);
        public int HeartAt(Vector2 guiPoint)
        {
            for(int i=0;i<HeartCount;i++) if(HeartRect(i).Contains(guiPoint)) return i;
            return -1;
        }
        public bool HarvestHeart(int index)
        {
            if(IsGameOver || index<0 || index>=HeartCount) return false;
            var heart=Resources.Load<PlantDefinition>("Equipment/Heart");
            if(heart==null) { farm.Notify("Heart item data is missing."); return false; }
            if(!farm.ReceiveHarvest(heart)) return false;
            removedHearts++; Health=Mathf.Min(Health,MaxHealth);
            if(HeartCount==0)
            {
                IsGameOver=true; rollTime=0; farm.Shop?.Close();
                farm.Notify("Game over. You harvested your last heart."); return true;
            }
            farm.Notify("One heart harvested. Maximum HP decreased."); return true;
        }
        public int RemovedHearts => removedHearts;
        public void RestorePlayerState(float health,int heartsRemoved,int kills,bool gameOver)
        {
            removedHearts=Mathf.Clamp(heartsRemoved,0,startingHearts); Health=Mathf.Clamp(health,0,MaxHealth); Kills=Mathf.Max(0,kills);
            IsGameOver=gameOver || HeartCount==0; invulnerability=1; rollTime=rollBufferTime=0;
        }
        public bool RestoreHeart()
        {
            if(IsGameOver) return false;
            if(removedHearts<=0) { farm.Notify("All heart slots are already attached."); return false; }
            removedHearts--; Health=Mathf.Min(MaxHealth,Health+healthPerHeart); return true;
        }
        public int Kills { get; private set; }
        public int EnemyCount { get { int n=0; foreach(var enemy in enemies) if(nightActive && enemy.health>0) n++; return n; } }
        public bool IsRolling => rollTime>0;
        public float RollProgress => rollDurationAtStart>0?Mathf.Clamp01(1-rollTime/rollDurationAtStart):1;
        public Vector2 RollDirection => rollDirection;
        public float PlayerHitFlash => Mathf.Clamp01(playerHitTime/.12f);
        public static bool Safe(Vector2 point) => Mathf.Abs(point.x)<=6.5f && Mathf.Abs(point.y)<=4.5f;
        private bool IsSafe(Vector2 point) => farm.IsSafe(point);
        private sealed class Enemy
        {
            public Vector2 home, aim, pathGoal;
            public List<Vector2> path=new();
            public float repath;
            public SpriteRenderer model, warning;
            public bool ranged, trainingDummy;
            public float health=55,cooldown=1,windup,respawn,hitTime,stagger;
            public Color tint;
        }
        private sealed class Shot { public SpriteRenderer model; public Vector2 velocity; public float damage,life=2.5f; public bool hostile; }
        private sealed class Flash { public SpriteRenderer model; public float time,duration; public Color tint; public Vector2 velocity; }
        public void Initialize(FarmGame owner)
        {
            farm=owner; Health=MaxHealth; art=new FarmArt(); square=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);
            if(farm.Layout!=null)
            {
                foreach(var marker in farm.Layout.GetComponentsInChildren<EnemyPlacement>()) SpawnEnemy(marker.transform.position,marker.ranged,new Vector2(Mathf.Abs(marker.transform.lossyScale.x),Mathf.Abs(marker.transform.lossyScale.y)),marker.trainingDummy);
                return;
            }
            SpawnEnemy(new Vector2(10,2),false); SpawnEnemy(new Vector2(12,-3),true);
            SpawnEnemy(new Vector2(-11,-2),false); SpawnEnemy(new Vector2(-13,4),true);
            SpawnEnemy(new Vector2(3,9),false); SpawnEnemy(new Vector2(-4,-10),true);
        }
        private SpriteRenderer Draw(string name,Vector2 point,Vector2 size,Color color,int icon=-1)
        {
            var root=new GameObject(name); root.transform.SetParent(transform,false); root.transform.position=point; root.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=icon<0?square:art.Get(icon); renderer.color=color; renderer.sortingOrder=50; return renderer;
        }
        private bool Active(Enemy enemy) => nightActive || enemy.trainingDummy;
        public void SpawnEnemy(Vector2 point,bool ranged,Vector2? size=null,bool trainingDummy=false)
        {
            if(IsSafe(point) && !trainingDummy) return;
            var enemy=new Enemy { home=point,ranged=ranged,trainingDummy=trainingDummy,model=Draw(trainingDummy?"Training dummy":ranged?"Spitter":"Chaser",point,size??Vector2.one*1.2f,trainingDummy?new Color(.85f,.65f,.32f):Color.white,trainingDummy?0:ranged?21:20),warning=Draw("Attack warning",point,Vector2.one*.15f,Color.yellow) };
            enemy.model.enabled=Active(enemy); enemy.warning.enabled=false; enemies.Add(enemy);
            enemy.tint=enemy.model.color;
        }
        public void SetNightActive(bool active)
        {
            if(nightActive==active) return;
            nightActive=active;
            foreach(var enemy in enemies)
            {
                if(enemy.trainingDummy) continue;
                enemy.windup=0; enemy.warning.enabled=false; enemy.path.Clear(); enemy.repath=0;
                if(active) { enemy.health=55; enemy.model.transform.position=enemy.home; enemy.cooldown=1; enemy.respawn=0; }
                enemy.model.enabled=active && enemy.health>0;
            }
            if(!active) for(int i=shots.Count-1;i>=0;i--) if(shots[i].hostile)
            { Remove(shots[i].model.gameObject); shots.RemoveAt(i); }
        }
        public bool Roll(Vector2 direction)
        {
            if(IsGameOver || IsRolling || rollCooldown>0 || direction.sqrMagnitude<.01f) return false;
            rollDirection=direction.normalized;
            rollTime=farm.Controls!=null?Mathf.Max(.02f,farm.Controls.rollDuration):.22f;
            rollDurationAtStart=rollTime;
            rollCooldown=farm.Controls!=null?Mathf.Max(0,farm.Controls.rollCooldown):.65f;
            invulnerability=Mathf.Max(invulnerability,rollTime);rollBufferTime=0;return true;
        }
        public bool RequestRoll(Vector2 direction)
        {
            if(IsGameOver||direction.sqrMagnitude<.01f)return false;
            if(Roll(direction))return true;
            bufferedRollDirection=direction.normalized;
            rollBufferTime=farm.Controls!=null?Mathf.Clamp(farm.Controls.rollInputBuffer,0,.3f):.12f;return rollBufferTime>0;
        }
        public bool Attack(PlantDefinition weapon,Vector2 target)
        {
            if(IsGameOver || weapon==null || weapon.effect!=GrowthEffect.Weapon || attackCooldown>0 || IsRolling) return false;
            attackCooldown=weapon.attackInterval;
            Vector2 direction=(target-farm.PlayerPosition).normalized; if(direction.sqrMagnitude<.1f) direction=Vector2.right;
            farm.WeaponFeedback(direction,weapon.ranged,weapon.attackInterval);
            if(weapon.ranged)
            {
                float speed=farm.Controls!=null?Mathf.Max(1,farm.Controls.projectileSpeed):18;
                // Keep the flash in front of walls; sweep the shot from the player
                // so even a target touching the player cannot fall behind its origin.
                Vector2 start=farm.PlayerPosition, muzzle=start+direction*Mathf.Min(.45f,weapon.attackRange*.25f);
                float clear=new MapCollision(farm).Trace(start,muzzle,.06f,true);
                muzzle=Vector2.Lerp(start,muzzle,Mathf.Max(0,clear-.01f));
                Shoot(start,direction*speed,weapon.damage,false,weapon.attackRange/speed);
                var flash=Effect("Muzzle flash",muzzle,new Vector2(.3f,.18f),new Color(1,.9f,.45f),.07f);
                flash.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            }
            else
            {
                float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
                float radius=weapon.attackRange*.8f;
                for(int i=0;i<12;i++)
                {
                    float degrees=angle-65+(i+.5f)*130/12;
                    Vector2 radial=new Vector2(Mathf.Cos(degrees*Mathf.Deg2Rad),Mathf.Sin(degrees*Mathf.Deg2Rad));
                    var flash=Effect("Sword swing",farm.PlayerPosition+radial*radius,new Vector2(radius*.2f,.07f),new Color(1,.9f,.45f),.12f);
                    flash.transform.rotation=Quaternion.Euler(0,0,degrees+90);
                }
                var attackCollision=new MapCollision(farm);
                foreach(var enemy in enemies)
                {
                    Vector2 offset=(Vector2)enemy.model.transform.position-farm.PlayerPosition;
                    if(Active(enemy) && enemy.health>0 && offset.magnitude<=weapon.attackRange+.35f && attackCollision.Trace(farm.PlayerPosition,enemy.model.transform.position,0,true)>=1 && (offset.magnitude<.35f || Vector2.Dot(direction,offset.normalized)>.35f)) Hit(enemy,weapon.damage,direction);
                }
            }
            return true;
        }
        private void Shoot(Vector2 point,Vector2 velocity,float damage,bool hostile,float life=2.5f)
        {
            var model=Draw(hostile?"Enemy shot":"Player shot",point,hostile?Vector2.one*.22f:new Vector2(.3f,.12f),hostile?new Color(1,.35f,.25f):new Color(1,.9f,.45f));
            model.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
            shots.Add(new Shot { model=model,velocity=velocity,damage=damage,hostile=hostile,life=Mathf.Max(.001f,life) });
        }
        private SpriteRenderer Effect(string name,Vector2 point,Vector2 size,Color tint,float duration,Vector2 velocity=default)
        {
            var model=Draw(name,point,size,tint); model.sortingOrder=35-Mathf.RoundToInt(point.y*10);
            flashes.Add(new Flash {model=model,time=duration,duration=duration,tint=tint,velocity=velocity}); return model;
        }
        public void Afterimage(SpriteRenderer source)
        {
            var tint=source.color; tint.a=.23f;
            var model=Effect("Dodge trail",source.transform.position,source.transform.lossyScale,tint,.12f);
            model.sprite=source.sprite;model.flipX=source.flipX;model.flipY=source.flipY;
            model.transform.rotation=source.transform.rotation;model.sortingOrder=source.sortingOrder-1;
        }
        private void Sparks(Vector2 point,Color tint)
        {
            for(int i=0;i<5;i++)
            {
                float angle=i*Mathf.PI*2/5;
                Vector2 velocity=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*1.8f;
                Effect("Impact spark",point,Vector2.one*.09f,tint,.14f,velocity);
            }
        }
        public void ToolEffect(Vector2 point,GrowthEffect kind)
        {
            Sparks(point,kind==GrowthEffect.Water?new Color(.35f,.75f,1):kind==GrowthEffect.Scythe?new Color(.6f,.85f,.3f):new Color(.6f,.38f,.18f));
        }
        private void Hit(Enemy enemy,float damage,Vector2 direction)
        {
            enemy.health-=damage;
            enemy.hitTime=.1f;enemy.stagger=.08f;enemy.windup=0;enemy.warning.enabled=false;enemy.model.color=new Color(1,.4f,.35f);
            Sparks(enemy.model.transform.position,new Color(1,.9f,.65f));farm.ImpactFeedback(false);
            if(enemy.health<=0)
            {
                enemy.health=0; enemy.model.enabled=false; enemy.warning.enabled=false; enemy.windup=0; enemy.respawn=25;
                if(enemy.trainingDummy) enemy.respawn=3;
                else { Kills++; GameSession.RecordGuildKill(farm,enemy.ranged); farm.DropCombatMaterial(enemy.model.transform.position,enemy.ranged); }
            }
            else if(!enemy.trainingDummy)
            {
                Vector2 pushed=(Vector2)enemy.model.transform.position+direction*.35f;
                enemy.model.transform.position=new MapCollision(farm,true).Move(enemy.model.transform.position,pushed,enemyRadius);
            }
        }
        public void DamagePlayer(float damage)
        {
            if(IsGameOver || damage<=0 || invulnerability>0 || IsSafe(farm.PlayerPosition)) return;
            float steppedDamage=Mathf.Ceil(damage/HalfHeartHealth)*HalfHeartHealth;
            Health=Mathf.Max(0,Health-steppedDamage); invulnerability=.65f; quietTime=0;
            playerHitTime=.12f;farm.ImpactFeedback(true);Sparks(farm.PlayerPosition,new Color(1,.4f,.3f));
            if(Health<=0)
            {
                // Solo prototype recovery; inventory is preserved. Planted resurrection is a later milestone.
                farm.TeleportPlayer(farm.SpawnPosition); Health=MaxHealth; invulnerability=1.5f; rollTime=rollBufferTime=0;
                farm.Notify("You collapsed. Returned to the farm; your items are safe.");
            }
        }
        public void TickCombat(float seconds)
        {
            if(IsGameOver || seconds<=0 || farm==null) return;
            collision=new MapCollision(farm); enemyCollision=new MapCollision(farm,true);
            attackCooldown=Mathf.Max(0,attackCooldown-seconds); invulnerability=Mathf.Max(0,invulnerability-seconds);
            playerHitTime=Mathf.Max(0,playerHitTime-seconds);
            float previousRollCooldown=rollCooldown;
            rollCooldown=Mathf.Max(0,rollCooldown-seconds); quietTime+=seconds;
            if(rollBufferTime>0)
            {
                bool ready=!IsRolling&&previousRollCooldown<=Mathf.Min(seconds,rollBufferTime);
                rollBufferTime=Mathf.Max(0,rollBufferTime-seconds);
                if(ready)Roll(bufferedRollDirection);
            }
            if(rollTime>0) { float active=Mathf.Min(rollTime,seconds);float speed=farm.Controls!=null?Mathf.Max(.1f,farm.Controls.rollSpeed):11;farm.MovePlayer(farm.PlayerPosition+rollDirection*active*speed); rollTime=Mathf.Max(0,rollTime-seconds); }
            if(IsSafe(farm.PlayerPosition) && quietTime>2) Health=Mathf.Min(MaxHealth,Health+seconds*14);
            foreach(var enemy in enemies)
            {
                if(!Active(enemy)) continue;
                enemy.hitTime=Mathf.Max(0,enemy.hitTime-seconds);enemy.stagger=Mathf.Max(0,enemy.stagger-seconds);
                enemy.model.color=Color.Lerp(enemy.tint,new Color(1,.4f,.35f),Mathf.Clamp01(enemy.hitTime/.1f));
                if(enemy.health<=0)
                {
                    enemy.respawn-=seconds;
                    if(enemy.respawn<=0 && (enemy.trainingDummy || Vector2.Distance(farm.PlayerPosition,enemy.home)>4)) { enemy.health=55; enemy.model.transform.position=enemy.home; enemy.model.enabled=true; enemy.cooldown=1; }
                    continue;
                }
                Vector2 point=enemy.model.transform.position, offset=farm.PlayerPosition-point;
                if(enemy.trainingDummy) { enemy.model.sortingOrder=30-Mathf.RoundToInt(point.y*10); continue; }
                if(enemy.stagger>0) continue;
                bool active=!IsSafe(farm.PlayerPosition) && offset.magnitude<7;
                enemy.model.sortingOrder=30-Mathf.RoundToInt(point.y*10);
                enemy.model.flipX=offset.x<0;
                if(!active) { enemy.windup=0; enemy.warning.enabled=false; MoveEnemy(enemy,enemy.home,seconds*1.7f,seconds); enemy.cooldown=Mathf.Max(.5f,enemy.cooldown-seconds); continue; }
                if(enemy.windup>0)
                {
                    enemy.windup-=seconds; enemy.warning.enabled=true;
                    float length=enemy.ranged?4.4f:1.2f;
                    enemy.warning.transform.position=point+enemy.aim*length*.5f;
                    enemy.warning.transform.localScale=new Vector3(length,.045f,1);
                    enemy.warning.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(enemy.aim.y,enemy.aim.x)*Mathf.Rad2Deg);
                    enemy.warning.color=new Color(1,.4f,.18f,.55f);
                    enemy.warning.sortingOrder=enemy.model.sortingOrder+1;
                    if(enemy.windup<=0)
                    {
                        enemy.warning.enabled=false;
                        if(enemy.ranged) Shoot(point,enemy.aim*4.5f,HalfHeartHealth,true);
                        else if(offset.magnitude<1.35f && collision.Trace(point,farm.PlayerPosition,0,true)>=1) DamagePlayer(HalfHeartHealth);
                        enemy.cooldown=enemy.ranged?1.7f:1;
                    }
                    continue;
                }
                enemy.cooldown-=seconds;
                float reach=enemy.ranged?4.8f:1.05f;
                if(offset.magnitude<=reach && enemy.cooldown<=0 && collision.Trace(point,farm.PlayerPosition,.1f,true)>=1) { enemy.windup=.5f; enemy.aim=offset.normalized; }
                else if(offset.magnitude>reach*.85f || collision.Trace(point,farm.PlayerPosition,.1f,true)<1)
                {
                    MoveEnemy(enemy,farm.PlayerPosition,seconds*(enemy.ranged?1.3f:2),seconds);
                }
            }
            SeparateEnemies(seconds);
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i]; Vector2 start=shot.model.transform.position, end=start+shot.velocity*Mathf.Min(seconds,shot.life);
                shot.life-=seconds; float radius=shot.hostile?.11f:.06f;
                float wall=collision.Trace(start,end,radius,true), first=wall;
                Enemy victim=null; bool playerHit=false;
                if(shot.hostile)
                {
                    if(MapCollision.Intersect(start,end,farm.SafeBounds,out float safeEntry)) first=Mathf.Min(first,safeEntry);
                    if(!IsSafe(farm.PlayerPosition) && SegmentCircle(start,end,farm.PlayerPosition,farm.PlayerRadius+radius,out float entry) && entry<first)
                    { first=entry; playerHit=true; }
                }
                else foreach(var enemy in enemies)
                {
                    if(Active(enemy) && enemy.health>0 && SegmentCircle(start,end,enemy.model.transform.position,enemyRadius+radius,out float entry) && entry<first)
                    { first=entry; victim=enemy; }
                }
                if(playerHit) DamagePlayer(shot.damage);
                if(victim!=null) Hit(victim,shot.damage,shot.velocity.normalized);
                shot.model.transform.position=Vector2.Lerp(start,end,first);
                if(first<1 || shot.life<=0) { Remove(shot.model.gameObject); shots.RemoveAt(i); }
            }
            for(int i=flashes.Count-1;i>=0;i--)
            {
                var flash=flashes[i];flash.time-=seconds;
                if(flash.time<=0) { Remove(flash.model.gameObject); flashes.RemoveAt(i);continue; }
                flash.model.transform.position+=(Vector3)(flash.velocity*seconds);
                var tint=flash.tint;tint.a*=flash.time/flash.duration;flash.model.color=tint;
            }
        }
        private void MoveEnemy(Enemy enemy,Vector2 target,float distance,float seconds)
        {
            Vector2 start=enemy.model.transform.position;
            enemy.repath-=seconds;
            Vector2 waypoint=target;
            if(enemyCollision.Trace(start,target,enemyRadius)<1)
            {
                if(enemy.repath<=0 || Vector2.Distance(enemy.pathGoal,target)>.75f)
                { enemy.path=enemyCollision.FindPath(start,target,enemyRadius); enemy.pathGoal=target; enemy.repath=.6f; }
                while(enemy.path.Count>0 && Vector2.Distance(start,enemy.path[0])<.12f) enemy.path.RemoveAt(0);
                if(enemy.path.Count==0) return;
                waypoint=enemy.path[0];
            }
            else { enemy.path.Clear(); enemy.repath=0; }
            enemy.model.transform.position=enemyCollision.Move(start,Vector2.MoveTowards(start,waypoint,distance),enemyRadius);
        }
        private void SeparateEnemies(float seconds)
        {
            if(!nightActive) return;
            for(int i=0;i<enemies.Count;i++) for(int j=i+1;j<enemies.Count;j++)
            {
                var a=enemies[i]; var b=enemies[j]; if(a.health<=0 || b.health<=0 || a.trainingDummy || b.trainingDummy) continue;
                Vector2 pa=a.model.transform.position,pb=b.model.transform.position,delta=pa-pb;
                float distance=delta.magnitude; if(distance>=enemyRadius*2) continue;
                Vector2 direction=distance>.001f?delta/distance:((i+j)%2==0?Vector2.right:Vector2.up);
                float push=Mathf.Min((enemyRadius*2-distance)*.5f,seconds*2);
                a.model.transform.position=enemyCollision.Move(pa,pa+direction*push,enemyRadius);
                b.model.transform.position=enemyCollision.Move(pb,pb-direction*push,enemyRadius);
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
        private void Remove(GameObject root) { root.SetActive(false); if(Application.isPlaying) Destroy(root); else DestroyImmediate(root); }
        private void OnGUI()
        {
            if(farm==null) return;
            var previousColor=GUI.color;
            for(int i=0;i<HeartCount;i++)
            {
                var rect=HeartRect(i); GUI.color=new Color(.55f,.5f,.5f); art.Icon(rect,30);
                float fill=HeartFill(i);
                if(fill>0)
                {
                    GUI.color=Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,rect.width*fill,rect.height),art.Get(29).texture,new Rect(0,0,fill,1));
                }
            }
            GUI.color=previousColor;
            if(IsGameOver)
            {
                int previousDepth=GUI.depth; GUI.depth=-100;
                var tint=GUI.color; GUI.color=new Color(0,0,0,.8f);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture); GUI.color=tint;
                var style=new GUIStyle(GUI.skin.label) {fontSize=32,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
                GUI.Label(new Rect(0,Screen.height*.5f-60,Screen.width,60),"GAME OVER",style);
                style.fontSize=16; style.fontStyle=FontStyle.Normal;
                GUI.Label(new Rect(0,Screen.height*.5f,Screen.width,60),"You harvested your last heart.\nRestart Play to try again.",style);
                GUI.depth=previousDepth; return;
            }
            var camera=Camera.main;
            foreach(var enemy in enemies) if(Active(enemy) && enemy.health>0 && Vector2.Distance(farm.PlayerPosition,enemy.model.transform.position)<7)
            {
                var point=camera.WorldToScreenPoint(enemy.model.transform.position+Vector3.up*.7f);
                if(point.z<0) continue;
                var previous=GUI.color; GUI.color=new Color(.2f,.1f,.1f); GUI.DrawTexture(new Rect(point.x-22,Screen.height-point.y,44,4),Texture2D.whiteTexture);
                GUI.color=new Color(.9f,.35f,.25f); GUI.DrawTexture(new Rect(point.x-22,Screen.height-point.y,44*enemy.health/55,4),Texture2D.whiteTexture); GUI.color=previous;
            }
        }
        private void OnDestroy()
        {
            art?.Dispose(); if(square!=null) { if(Application.isPlaying) Destroy(square); else DestroyImmediate(square); }
        }
    }
}
