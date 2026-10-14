using UnityEngine;
namespace EverythingGrows
{
    public sealed class MapArea : MonoBehaviour
    {
        public Color outline=Color.yellow;
        public Rect Bounds => new Rect((Vector2)transform.position-new Vector2(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y))*.5f,new Vector2(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y)));
        private void OnDrawGizmos() { Gizmos.color=outline; Gizmos.DrawWireCube(transform.position,new Vector3(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y),0)); }
    }
}
