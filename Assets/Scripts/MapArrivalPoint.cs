using UnityEngine;
using UnityEngine.SceneManagement;
namespace EverythingGrows
{
    [AddComponentMenu("Everything Grows/Map Arrival Point")]
    public sealed class MapArrivalPoint : MonoBehaviour
    {
        public string arrivalId="entry";
        public static MapArrivalPoint Find(Scene scene,string id)
        {
            MapArrivalPoint found=null;
            foreach(var point in Object.FindObjectsByType<MapArrivalPoint>(FindObjectsSortMode.None))
                if(point.gameObject.scene==scene && point.arrivalId==id)
                { if(found!=null) throw new System.InvalidOperationException("Duplicate arrival ID: "+id); found=point; }
            return found;
        }
        void OnDrawGizmos() { Gizmos.color=Color.cyan; Gizmos.DrawWireSphere(transform.position,.35f); Gizmos.DrawLine(transform.position,transform.position+Vector3.up*.8f); }
    }
}
