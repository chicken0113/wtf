using System;
using UnityEngine;
namespace EverythingGrows
{
    [DisallowMultipleComponent,AddComponentMenu("Everything Grows/Map Object ID")]
    public sealed class MapObjectId : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;
        public void AssignNew() { id=Guid.NewGuid().ToString("N"); }
        public static string Key(Component component)
        {
            var marker=component.GetComponent<MapObjectId>();
            if(marker!=null && !string.IsNullOrEmpty(marker.id)) return marker.id;
            return FallbackKey(component);
        }
        public static string FallbackKey(Component component)
        {
            string path="";
            for(var t=component.transform;t!=null;t=t.parent) path="/"+t.name+"["+t.GetSiblingIndex()+"]"+path;
            return path;
        }
        void Reset() { AssignNew(); }
    }
}
