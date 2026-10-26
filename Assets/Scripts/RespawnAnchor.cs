using UnityEngine;
namespace EverythingGrows
{
    [DisallowMultipleComponent, AddComponentMenu("Everything Grows/Respawn Anchor")]
    public sealed class RespawnAnchor : MonoBehaviour
    {
        public Vector2 spawnOffset=Vector2.down;
        public Vector2 interactionSize=new(1.2f,1.6f);
        [HideInInspector] public bool runtimePlaced;
        [HideInInspector] public string runtimeId;
        public string Key => runtimePlaced?runtimeId:MapObjectId.Key(this);
        public Vector2 SpawnPosition => (Vector2)transform.position+spawnOffset;
        public bool Contains(Vector2 point) => new Rect((Vector2)transform.position-interactionSize*.5f,interactionSize).Contains(point);
        void OnDrawGizmosSelected() { Gizmos.color=Color.cyan;Gizmos.DrawWireCube(transform.position,interactionSize);Gizmos.DrawWireSphere(SpawnPosition,.25f); }
    }
}
