using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    public sealed class OpenFieldCombat : MonoBehaviour
    {
        private FarmGame farm;
        private FarmArt art;
        private Sprite square;
        private float attackCooldown, invulnerability, rollTime, rollCooldown, quietTime;
        private Vector2 rollDirection;
        private readonly List<Enemy> enemies=new();
        private readonly List<Shot> shots=new();
        private readonly List<Flash> flashes=new();
        [SerializeField,Range(1,5)] private int startingHearts=5;
        [SerializeField,Min(1)] private float healthPerHeart=20;
        private int removedHearts;
        public int HeartCount => Mathf.Max(1,startingHearts-removedHearts);
        public float MaxHealth => HeartCount*healthPerHeart;
        public float Health { get; private set; } = 100;
        public Rect HudRect => new Rect(Screen.width-230,10,220,78);
        public Rect HeartRect(int index) => new Rect(HudRect.x+12+index*38,HudRect.y+22,32,28);
        public int HeartAt(Vector2 guiPoint)
        {
            for(int i=0;i<HeartCount;i++) if(HeartRect(i).Contains(guiPoint)) return i;
            return -1;
        }
        public bool HarvestHeart(int index)
        {
            if(index<0 || index>=HeartCount) return false;
            if(HeartCount<=1) { farm.Notify("Keep your last heart to stay alive."); return false; }
            var heart=Resources.Load<PlantDefinition>("Equipment/Heart");
            if(heart==null) { farm.Notify("Heart item data is missing."); return false; }
            farm.DropItem(heart,farm.PlayerPosition+Vector2.down*.65f);
            removedHearts++; Health=Mathf.Min(Health,MaxHealth);
            farm.Notify("One heart harvested. Maximum HP decreased. Pick it up with RMB."); return true;
        }
        public bool RestoreHeart()
        {
            if(removedHearts<=0) { farm.Notify("All heart slots are already attached."); return false; }
            removedHearts--; Health=Mathf.Min(MaxHealth,Health+healthPerHeart); return true;
        }
        public int Kills { get; private set; }
        public int EnemyCount { get { int n=0; foreach(var enemy in enemies) if(enemy.health>0) n++; return n; } }
        public bool IsRolling => rollTime>0;
        public static bool Safe(Vector2 point) => Mathf.Abs(point.x)<=6.5f && Mathf.Abs(point.y)<=4.5f;
        private bool IsSafe(Vector2 point) => farm.IsSafe(point);
        private sealed class Enemy
        {
            public Vector2 home, aim;
            public SpriteRenderer model, warning;
            public bool ranged;
            public float health=55,cooldown=1,windup,respawn;
        }
        private sealed class Shot { public SpriteRenderer model; public Vector2 velocity; public float damage,life=2.5f; public bool hostile; }
        private sealed class Flash { public SpriteRenderer model; public float time=.16f; }
        public void Initialize(FarmGame owner)
        {
            farm=owner; Health=MaxHealth; art=new FarmArt(); square=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);
            if(farm.Layout!=null)
            {
                foreach(var marker in farm.Layout.GetComponentsInChildren<EnemyPlacement>()) SpawnEnemy(marker.transform.position,marker.ranged,new Vector2(Mathf.Abs(marker.transform.lossyScale.x),Mathf.Abs(marker.transform.lossyScale.y)));
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
        public void SpawnEnemy(Vector2 point,bool ranged,Vector2? size=null)
        {
            if(IsSafe(point)) return;
            var enemy=new Enemy { home=point,ranged=ranged,model=Draw(ranged?"Spitter":"Chaser",point,size??Vector2.one*1.2f,Color.white,ranged?21:20),warning=Draw("Attack warning",point,Vector2.one*.15f,Color.yellow) };
            enemy.warning.enabled=false; enemies.Add(enemy);
        }
        public bool Roll(Vector2 direction)
        {
            if(rollCooldown>0 || direction.sqrMagnitude<.01f) return false;
            rollDirection=direction.normalized; rollTime=.2f; rollCooldown=.85f; invulnerability=Mathf.Max(invulnerability,.25f); return true;
        }
        public bool Attack(PlantDefinition weapon,Vector2 target)
        {
            if(weapon==null || weapon.effect!=GrowthEffect.Weapon || attackCooldown>0 || IsRolling) return false;
            attackCooldown=weapon.attackInterval;
            Vector2 direction=(target-farm.PlayerPosition).normalized; if(direction.sqrMagnitude<.1f) direction=Vector2.right;
            if(weapon.ranged) Shoot(farm.PlayerPosition+direction*.55f,direction*10,weapon.damage,false,weapon.attackRange/10);
            else
            {
                var flash=Draw("Sword swing",farm.PlayerPosition+direction*.9f,new Vector2(weapon.attackRange,.13f),new Color(1,.9f,.45f));
                flash.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg); flashes.Add(new Flash {model=flash});
                foreach(var enemy in enemies)
                {
                    Vector2 offset=(Vector2)enemy.model.transform.position-farm.PlayerPosition;
                    if(enemy.health>0 && offset.magnitude<=weapon.attackRange+.35f && (offset.magnitude<.35f || Vector2.Dot(direction,offset.normalized)>.35f)) Hit(enemy,weapon.damage,direction);
                }
            }
            return true;
        }
        private void Shoot(Vector2 point,Vector2 velocity,float damage,bool hostile,float life=2.5f)
        {
            var model=Draw(hostile?"Enemy shot":"Player shot",point,Vector2.one*(hostile?.22f:.12f),hostile?new Color(1,.35f,.25f):new Color(1,.9f,.45f));
            shots.Add(new Shot { model=model,velocity=velocity,damage=damage,hostile=hostile,life=Mathf.Max(.1f,life) });
        }
        private void Hit(Enemy enemy,float damage,Vector2 direction)
        {
            enemy.health-=damage;
            if(enemy.health<=0)
            {
                enemy.health=0; enemy.model.enabled=false; enemy.warning.enabled=false; enemy.windup=0; enemy.respawn=25;
                Kills++; farm.DropCombatMaterial(enemy.model.transform.position,enemy.ranged);
            }
            else
            {
                Vector2 pushed=(Vector2)enemy.model.transform.position+direction*.35f;
                if(!IsSafe(pushed)) enemy.model.transform.position=pushed;
            }
        }
        public void DamagePlayer(float damage)
        {
            if(invulnerability>0 || IsSafe(farm.PlayerPosition)) return;
            Health=Mathf.Max(0,Health-damage); invulnerability=.65f; quietTime=0;
            if(Health<=0)
            {
                // Solo prototype recovery; inventory is preserved. Planted resurrection is a later milestone.
                farm.MovePlayer(farm.SpawnPosition); Health=MaxHealth; invulnerability=1.5f; rollTime=0;
                farm.Notify("You collapsed. Returned to the farm; your items are safe.");
            }
        }
        public void TickCombat(float seconds)
        {
            if(seconds<=0 || farm==null) return;
            attackCooldown=Mathf.Max(0,attackCooldown-seconds); invulnerability=Mathf.Max(0,invulnerability-seconds);
            rollCooldown=Mathf.Max(0,rollCooldown-seconds); quietTime+=seconds;
            if(rollTime>0) { float active=Mathf.Min(rollTime,seconds); farm.MovePlayer(farm.PlayerPosition+rollDirection*active*9); rollTime=Mathf.Max(0,rollTime-seconds); }
            if(IsSafe(farm.PlayerPosition) && quietTime>2) Health=Mathf.Min(MaxHealth,Health+seconds*14);
            foreach(var enemy in enemies)
            {
                if(enemy.health<=0)
                {
                    enemy.respawn-=seconds;
                    if(enemy.respawn<=0 && Vector2.Distance(farm.PlayerPosition,enemy.home)>4) { enemy.health=55; enemy.model.transform.position=enemy.home; enemy.model.enabled=true; enemy.cooldown=1; }
                    continue;
                }
                Vector2 point=enemy.model.transform.position, offset=farm.PlayerPosition-point;
                bool active=!IsSafe(farm.PlayerPosition) && offset.magnitude<7;
                enemy.model.sortingOrder=30-Mathf.RoundToInt(point.y*10);
                enemy.model.flipX=offset.x<0;
                if(!active) { enemy.windup=0; enemy.warning.enabled=false; enemy.model.transform.position=Vector2.MoveTowards(point,enemy.home,seconds*1.7f); enemy.cooldown=Mathf.Max(.5f,enemy.cooldown-seconds); continue; }
                if(enemy.windup>0)
                {
                    enemy.windup-=seconds; enemy.warning.enabled=true;
                    enemy.warning.transform.position=point+Vector2.up*.65f;
                    if(enemy.windup<=0)
                    {
                        enemy.warning.enabled=false;
                        if(enemy.ranged) Shoot(point,enemy.aim*4.5f,12,true);
                        else if(offset.magnitude<1.35f) DamagePlayer(16);
                        enemy.cooldown=enemy.ranged?1.7f:1;
                    }
                    continue;
                }
                enemy.cooldown-=seconds;
                float reach=enemy.ranged?4.8f:1.05f;
                if(offset.magnitude<=reach && enemy.cooldown<=0) { enemy.windup=.5f; enemy.aim=offset.normalized; }
                else if(offset.magnitude>reach*.85f)
                {
                    Vector2 next=Vector2.MoveTowards(point,farm.PlayerPosition,seconds*(enemy.ranged?1.3f:2));
                    if(!IsSafe(next)) enemy.model.transform.position=next;
                }
            }
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i]; Vector2 start=shot.model.transform.position, end=start+shot.velocity*Mathf.Min(seconds,shot.life); shot.life-=seconds; bool hit=false;
                if(shot.hostile)
                {
                    if(IsSafe(end)) hit=true;
                    else if(SegmentDistance(start,end,farm.PlayerPosition)<.35f) { DamagePlayer(shot.damage); hit=true; }
                }
                else foreach(var enemy in enemies) if(enemy.health>0 && SegmentDistance(start,end,enemy.model.transform.position)<.5f)
                { Hit(enemy,shot.damage,shot.velocity.normalized); hit=true; break; }
                shot.model.transform.position=end;
                if(hit || shot.life<=0) { Remove(shot.model.gameObject); shots.RemoveAt(i); }
            }
            for(int i=flashes.Count-1;i>=0;i--) { flashes[i].time-=seconds; if(flashes[i].time<=0) { Remove(flashes[i].model.gameObject); flashes.RemoveAt(i); } }
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
            GUI.Box(HudRect,IsSafe(farm.PlayerPosition)?"FARM / SAFE":"WILD FIELD / COMBAT");
            var previousColor=GUI.color;
            for(int i=0;i<HeartCount;i++)
            {
                var rect=HeartRect(i); GUI.color=new Color(.24f,.16f,.18f); art.Icon(rect,29);
                float fill=Mathf.Clamp01((Health-i*healthPerHeart)/healthPerHeart);
                if(fill>0)
                {
                    GUI.color=Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,rect.width*fill,rect.height),art.Get(29).texture,new Rect(0,0,fill,1));
                }
            }
            GUI.color=previousColor;
            int hovered=HeartAt(Event.current.mousePosition);
            GUI.Label(new Rect(Screen.width-218,60,208,24),hovered>=0?(HeartCount>1?"E: harvest one heart":"Keep the final heart"):
                "HP "+Mathf.CeilToInt(Health)+" / "+Mathf.CeilToInt(MaxHealth)+"  |  Defeated "+Kills);
            var camera=Camera.main;
            foreach(var enemy in enemies) if(enemy.health>0 && Vector2.Distance(farm.PlayerPosition,enemy.model.transform.position)<7)
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
