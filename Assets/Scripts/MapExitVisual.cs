using UnityEngine;
namespace EverythingGrows
{
    [ExecuteAlways,RequireComponent(typeof(MapExit))]
    public sealed class MapExitVisual : MonoBehaviour
    {
        public SpriteRenderer floor;
        public SpriteRenderer[] accents;
        void OnEnable() { Refresh(); }
        void Update() { Refresh(); }
        public void Refresh()
        {
            var exit=GetComponent<MapExit>(); var tint=exit.ModeColor;
            if(floor!=null && floor.color!=tint) floor.color=tint;
            if(accents!=null) foreach(var accent in accents) if(accent!=null && accent.color!=tint) accent.color=tint;
        }
    }
}
