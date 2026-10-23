using UnityEngine;
namespace EverythingGrows
{
    public sealed class FarmMapLayout : MonoBehaviour
    {
        public enum CombatSchedule { NightOnly, Peaceful, Always }
        [Tooltip("Peaceful: no enemies. Always: enemies remain active regardless of time.")]
        public CombatSchedule combatSchedule=CombatSchedule.NightOnly;
        public bool dayNightLighting=true;
        [Tooltip("Allow the existing passive health recovery in safe areas. Disabled in the hospital.")]
        public bool passiveHealthRecovery=true;
        public MapArea worldBounds;
        public MapArea safeArea;
        public Transform playerSpawn;
        public bool showSafeMarkers=true;
        private FarmGround[] grounds;
        public Rect World => worldBounds!=null?worldBounds.Bounds:new Rect(-24.5f,-16.5f,49,33);
        public Rect Safe => safeArea!=null?safeArea.Bounds:new Rect(-6.5f,-4.5f,13,9);
        public Vector2 Spawn => playerSpawn!=null?(Vector2)playerSpawn.position:Safe.center;
        public bool CanFarm(Vector2 point)
        {
            if(!World.Contains(point)) return false;
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) if(building.Blocks(point)) return false;
            grounds ??= GetComponentsInChildren<FarmGround>();
            foreach(var ground in grounds) if(ground!=null && ground.gameObject.activeInHierarchy && ground.canFarm && ground.Bounds.Contains(point)) return true;
            return false;
        }
        public void HideMarkers()
        {
            foreach(var item in GetComponentsInChildren<FarmItemPlacement>()) { var renderer=item.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=false; }
            foreach(var enemy in GetComponentsInChildren<EnemyPlacement>()) { var renderer=enemy.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=false; }
            if(playerSpawn!=null) { var renderer=playerSpawn.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=false; }
        }
    }
}
