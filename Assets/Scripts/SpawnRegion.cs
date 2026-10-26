using System;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Spawn Region")]
    public sealed class SpawnRegion : ScriptableObject
    {
        [HideInInspector] public string id;
        public string label;
        public bool spawnEnabled=true;
        public string scenePath;
        public bool useMapBounds=true;
        public Rect area=new(-10,-8,20,16);
        [Min(.1f)] public float spawnInterval=8;
        [Min(0)] public int maxAlive=6;
        [Min(0)] public float viewPadding=3;
        [Min(0)] public float playerExclusionRadius=3;
        public Rect Bounds(FarmGame farm)=>useMapBounds?farm.WorldBounds:area;
        public static SpawnRegion[] ForScene(string scene)=>Resources.LoadAll<SpawnRegion>("").Where(r=>r.spawnEnabled && r.scenePath==scene).OrderBy(r=>r.id,StringComparer.Ordinal).ToArray();
        public static Rect CameraBounds(Camera camera,float padding)
        {
            if(camera==null || !camera.orthographic)return new Rect(float.MinValue/4,float.MinValue/4,float.MaxValue/2,float.MaxValue/2);
            Vector2 size=new(camera.orthographicSize*2*camera.aspect+padding*2,camera.orthographicSize*2+padding*2);
            return new Rect((Vector2)camera.transform.position-size*.5f,size);
        }
    }
}
