using UnityEngine;

namespace EverythingGrows
{
    [AddComponentMenu("Everything Grows/Map Obstacle")]
    public sealed class MapObstacle : MonoBehaviour
    {
        public Vector2 size=Vector2.one;
        public Vector2 offset;
        public bool blocksMovement=true;
        public bool blocksProjectiles=true;
        public Rect Bounds
        {
            get
            {
                var bounds=new Bounds(transform.TransformPoint(offset),Vector3.zero);
                for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2)
                    bounds.Encapsulate(transform.TransformPoint(offset+new Vector2(Mathf.Abs(size.x)*x,Mathf.Abs(size.y)*y)*.5f));
                return new Rect(bounds.min,bounds.size);
            }
        }
        private void OnDrawGizmos()
        {
            if(!enabled) return;
            var rect=Bounds;
            Gizmos.color=new Color(1,.65f,.1f,.2f); Gizmos.DrawCube(rect.center,rect.size);
            Gizmos.color=new Color(1,.65f,.1f,.85f); Gizmos.DrawWireCube(rect.center,rect.size);
        }
    }
}
