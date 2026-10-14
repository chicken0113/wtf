using UnityEngine;
namespace EverythingGrows
{
    public sealed class FarmMapLayout : MonoBehaviour
    {
        public MapArea worldBounds;
        public MapArea safeArea;
        public Transform playerSpawn;
        private FarmGround[] grounds;
        public Rect World => worldBounds!=null?worldBounds.Bounds:new Rect(-24.5f,-16.5f,49,33);
        public Rect Safe => safeArea!=null?safeArea.Bounds:new Rect(-6.5f,-4.5f,13,9);
        public Vector2 Spawn => playerSpawn!=null?(Vector2)playerSpawn.position:Safe.center;
        public bool CanFarm(Vector2 point)
        {
            if(!World.Contains(point)) return false;
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
