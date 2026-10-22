using UnityEngine;
namespace EverythingGrows
{
    [AddComponentMenu("Everything Grows/Map Exit")]
    public sealed class MapExit : MonoBehaviour
    {
        [Tooltip("Use the Scene field in the custom Inspector.")] public string destinationScene;
        public string arrivalId="entry";
        public Vector2 areaSize=new Vector2(1.5f,1.5f);
        public bool requireInteract=true;
        private bool blockedUntilExit;
        public Color ModeColor => requireInteract?new Color(.3f,.85f,.75f):new Color(1,.62f,.18f);
        public bool TouchesPlayer(FarmGame farm)
        {
            Vector2 point=transform.InverseTransformPoint(farm.PlayerPosition);
            Vector2 extent=Vector2.zero;
            for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2)
            {
                Vector2 corner=transform.InverseTransformVector(new Vector2(x,y)*farm.PlayerRadius);
                extent=Vector2.Max(extent,new Vector2(Mathf.Abs(corner.x),Mathf.Abs(corner.y)));
            }
            return new Rect(-areaSize*.5f-extent,areaSize+extent*2).Contains(point);
        }
        public static void PrepareArrival(FarmGame farm)
        {
            foreach(var exit in Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))
                if(exit.gameObject.scene==farm.gameObject.scene) exit.blockedUntilExit=!exit.requireInteract && exit.TouchesPlayer(farm);
        }
        public bool TryActivate(FarmGame farm,bool interactPressed)
        {
            if(!isActiveAndEnabled || farm==null || farm.gameObject.scene!=gameObject.scene) return false;
            bool touching=TouchesPlayer(farm);
            if(!touching) blockedUntilExit=false;
            if(farm.WorldInputBlocked || farm.Combat==null || farm.Combat.IsGameOver || GameSession.Busy || Time.unscaledTime<GameSession.ArrivalGraceUntil) return false;
            if(requireInteract) return interactPressed && Contains(farm.PlayerPosition) && Travel(farm);
            if(!touching || blockedUntilExit) return false;
            blockedUntilExit=true;
            return Travel(farm);
        }
        public bool Contains(Vector2 point) => new Rect(-areaSize*.5f,areaSize).Contains(transform.InverseTransformPoint(point));
        public bool Travel(FarmGame farm) => TryGetComponent<HouseInteriorLink>(out var house)
            ? house.Travel(farm,this) : GameSession.Travel(farm,destinationScene,arrivalId);
        public static bool TryInteract(FarmGame farm)
        {
            bool pressed=Input.GetKeyDown(KeyCode.F);
            foreach(var exit in Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))
                if(exit.TryActivate(farm,pressed)) return true;
            return false;
        }
        public static bool TryAutomatic(FarmGame farm)
        {
            foreach(var exit in Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))
                if(!exit.requireInteract&&exit.TryActivate(farm,false))return true;
            return false;
        }
        public static bool TryDoor(FarmGame farm)
        {
            MapExit nearest=null;float distance=float.PositiveInfinity;
            foreach(var exit in Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))
            {
                if(!exit.isActiveAndEnabled||!exit.requireInteract||exit.gameObject.scene!=farm.gameObject.scene||!exit.Contains(farm.PlayerPosition))continue;
                float candidate=Vector2.Distance(farm.PlayerPosition,exit.transform.position);
                if(candidate<distance){nearest=exit;distance=candidate;}
            }
            return nearest!=null&&nearest.TryActivate(farm,true);
        }
        void OnGUI()
        {
            if(!Application.isPlaying || !requireInteract || GameSession.Busy) return;
            var farm=Object.FindFirstObjectByType<FarmGame>();
            if(farm==null || farm.Combat==null || farm.Combat.IsGameOver || farm.WorldInputBlocked || !Contains(farm.PlayerPosition)) return;
            var point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*.7f);
            GUI.Label(new Rect(point.x-70,Screen.height-point.y,140,24),"F: travel");
        }
        void OnDrawGizmos()
        {
            Gizmos.matrix=transform.localToWorldMatrix; Gizmos.color=new Color(ModeColor.r,ModeColor.g,ModeColor.b,.2f); Gizmos.DrawCube(Vector3.zero,areaSize);
            Gizmos.color=ModeColor; Gizmos.DrawWireCube(Vector3.zero,areaSize);
        }
    }
}
