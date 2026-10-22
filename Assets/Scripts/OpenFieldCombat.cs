using UnityEngine;

namespace EverythingGrows
{
    // Serialized scene component and stable entry point; subsystem classes own combat state.
    public sealed class OpenFieldCombat : MonoBehaviour
    {
        [SerializeField,Min(.1f)] private float enemyRadius=.35f;
        [SerializeField,Range(1,5)] private int startingHearts=5;
        [SerializeField,Min(1)] private float healthPerHeart=20;
        internal float EnemyRadius => enemyRadius;
        internal int StartingHearts => startingHearts;
        internal float HealthPerHeart => healthPerHeart;
        private FarmGame farm;
        private PlayerCombatState playerState;
        private EnemyCombatSystem enemySystem;
        private ProjectileSystem projectiles;
        private CombatEffects effects;
        public int HeartCount => playerState!=null?playerState.HeartCount:startingHearts;
        public int RemovedHearts => playerState!=null?playerState.RemovedHearts:0;
        public float MaxHealth => playerState!=null?playerState.MaxHealth:HeartCount*healthPerHeart;
        public float HalfHeartHealth => playerState!=null?playerState.HalfHeartHealth:healthPerHeart*.5f;
        public float Health => playerState!=null?playerState.Health:100;
        public bool IsGameOver => playerState!=null&&playerState.IsGameOver;
        public bool IsRolling => playerState!=null&&playerState.IsRolling;
        public float RollProgress => playerState!=null?playerState.RollProgress:1;
        public Vector2 RollDirection => playerState!=null?playerState.RollDirection:Vector2.zero;
        public float PlayerHitFlash => playerState!=null?playerState.PlayerHitFlash:0;
        public int Kills { get; private set; }
        public int LivingHostileCount => enemySystem!=null?enemySystem.LivingHostileCount:0;
        public int EnemyCount => enemySystem!=null?enemySystem.EnemyCount:0;
        public float HeartFill(int index) => playerState.HeartFill(index);
        public Rect HudRect => new Rect(14,14,HeartCount*38,32);
        public Rect HeartRect(int index) => new Rect(HudRect.x+2+index*38,HudRect.y+2,32,28);
        public int HeartAt(Vector2 guiPoint)
        {
            for(int i=0;i<HeartCount;i++) if(HeartRect(i).Contains(guiPoint)) return i;
            return -1;
        }
        public static bool Safe(Vector2 point) => Mathf.Abs(point.x)<=6.5f&&Mathf.Abs(point.y)<=4.5f;
        public bool HarvestHeart(int index) => playerState.HarvestHeart(index);
        public bool RestoreHeart() => playerState.RestoreHeart();
        public bool Roll(Vector2 direction) => playerState.Roll(direction);
        public bool RequestRoll(Vector2 direction) => playerState.RequestRoll(direction);
        public void DamagePlayer(float damage) => playerState.Damage(damage);
        public void Afterimage(SpriteRenderer source) => effects.Afterimage(source);
        public void ToolEffect(Vector2 point,GrowthEffect kind) => effects.ToolEffect(point,kind);
        public void SpawnEnemy(Vector2 point,bool ranged,Vector2? size=null,bool trainingDummy=false) => enemySystem.Spawn(point,ranged,size,trainingDummy);
        public void RestorePlayerState(float health,int heartsRemoved,int kills,bool gameOver)
        {
            playerState.Restore(health,heartsRemoved,gameOver);
            Kills=Mathf.Max(0,kills);
        }
        public void SetNightActive(bool active)
        {
            if(enemySystem.SetNightActive(active)&&!active) projectiles.ClearHostile();
        }
        private void EnemyDefeated(EnemyCombatSystem.Enemy enemy)
        {
            Kills++;
            GameSession.RecordGuildKill(farm,enemy.ranged);
            farm.DropCombatMaterial(enemy.model.transform.position,enemy.ranged);
        }
        public void Initialize(FarmGame owner)
        {
            farm=owner;
            effects=new CombatEffects(transform);
            playerState=new PlayerCombatState(farm,this,effects);
            enemySystem=new EnemyCombatSystem(farm,this,effects,EnemyDefeated);
            projectiles=new ProjectileSystem(farm,effects);
            if(farm.Layout!=null)
            {
                foreach(var marker in farm.Layout.GetComponentsInChildren<EnemyPlacement>()) SpawnEnemy(marker.transform.position,marker.ranged,new Vector2(Mathf.Abs(marker.transform.lossyScale.x),Mathf.Abs(marker.transform.lossyScale.y)),marker.trainingDummy);
                return;
            }
            SpawnEnemy(new Vector2(10,2),false); SpawnEnemy(new Vector2(12,-3),true);
            SpawnEnemy(new Vector2(-11,-2),false); SpawnEnemy(new Vector2(-13,4),true);
            SpawnEnemy(new Vector2(3,9),false); SpawnEnemy(new Vector2(-4,-10),true);
        }
        public bool Attack(PlantDefinition weapon,Vector2 target)
        {
            if(!playerState.TryBeginAttack(weapon)) return false;
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
                projectiles.Spawn(start,direction*speed,weapon.damage,false,weapon.attackRange/speed);
                effects.MuzzleFlash(muzzle,direction);
            }
            else
            {
                effects.MeleeArc(farm.PlayerPosition,direction,weapon.attackRange);
                enemySystem.HitMelee(weapon,direction);
            }
            return true;
        }
        public void TickCombat(float seconds)
        {
            if(IsGameOver||seconds<=0||farm==null) return;
            var collision=new MapCollision(farm);
            playerState.Tick(seconds);
            enemySystem.Tick(seconds,collision,playerState,projectiles);
            projectiles.Tick(seconds,collision,enemySystem,playerState);
            effects.Tick(seconds);
        }
        public static bool SegmentCircle(Vector2 start,Vector2 end,Vector2 center,float radius,out float entry)
        => ProjectileSystem.SegmentCircle(start,end,center,radius,out entry);
        public static float SegmentDistance(Vector2 start,Vector2 end,Vector2 point)
        => ProjectileSystem.SegmentDistance(start,end,point);
        private void OnGUI()
        {
            if(farm==null) return;
            var previousColor=GUI.color;
            for(int i=0;i<HeartCount;i++)
            {
                var rect=HeartRect(i); GUI.color=new Color(.55f,.5f,.5f); effects.Art.Icon(rect,30);
                float fill=HeartFill(i);
                if(fill>0)
                {
                    GUI.color=Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(rect.x,rect.y,rect.width*fill,rect.height),effects.Art.Get(29).texture,new Rect(0,0,fill,1));
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
            foreach(var enemy in enemySystem.Items) if(enemySystem.Active(enemy) && enemy.health>0 && Vector2.Distance(farm.PlayerPosition,enemy.model.transform.position)<7)
            {
                var point=camera.WorldToScreenPoint(enemy.model.transform.position+Vector3.up*.7f);
                if(point.z<0) continue;
                var previous=GUI.color; GUI.color=new Color(.2f,.1f,.1f); GUI.DrawTexture(new Rect(point.x-22,Screen.height-point.y,44,4),Texture2D.whiteTexture);
                GUI.color=new Color(.9f,.35f,.25f); GUI.DrawTexture(new Rect(point.x-22,Screen.height-point.y,44*enemy.health/55,4),Texture2D.whiteTexture); GUI.color=previous;
            }
        }
        private void OnDestroy() => effects?.Dispose();
    }
}
