using UnityEngine;
namespace EverythingGrows
{
    public sealed class FarmBuilding : MonoBehaviour
    {
        public PlantDefinition item;
        public PlantDefinition HarvestItem => item!=null?item:Resources.Load<PlantDefinition>("Buildings/House");
        public bool Contains(Vector2 point)
        {
            foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())
                if(renderer.enabled && new Rect(renderer.bounds.min,renderer.bounds.size).Contains(point)) return true;
            return false;
        }
        public Vector2 footprint=new Vector2(4.5f,1.8f);
        public Vector2 offset=new Vector2(0,-.8f);
        private void LateUpdate() { foreach(var renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.sortingOrder=30-Mathf.RoundToInt(transform.TransformPoint(offset-Vector2.up*footprint.y*.5f).y*10); }
        public bool Blocks(Vector2 point)
        {
            Vector2 local=transform.InverseTransformPoint(point);
            return new Rect(offset-footprint*.5f,footprint).Contains(local);
        }
        private void OnDrawGizmosSelected() { Gizmos.matrix=transform.localToWorldMatrix; Gizmos.color=Color.red; Gizmos.DrawWireCube(offset,footprint); }
    }
}
