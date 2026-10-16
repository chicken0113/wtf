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
        public bool Contains(Vector2 point) => new Rect(-areaSize*.5f,areaSize).Contains(transform.InverseTransformPoint(point));
        public bool Travel(FarmGame farm) => GameSession.Travel(farm,destinationScene,arrivalId);
        public static bool TryInteract(FarmGame farm)
        {
            if(farm.InventoryOpen || GameSession.Busy || Time.unscaledTime<GameSession.ArrivalGraceUntil) return false;
            foreach(var exit in Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None))
                if(exit.gameObject.scene==farm.gameObject.scene && exit.Contains(farm.PlayerPosition) && (!exit.requireInteract || Input.GetKeyDown(KeyCode.E)))
                { exit.Travel(farm); return true; }
            return false;
        }
        void OnGUI()
        {
            if(!Application.isPlaying || !requireInteract || GameSession.Busy) return;
            var farm=Object.FindFirstObjectByType<FarmGame>();
            if(farm==null || farm.Combat==null || farm.Combat.IsGameOver || farm.InventoryOpen || !Contains(farm.PlayerPosition)) return;
            var point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*.7f);
            GUI.Label(new Rect(point.x-70,Screen.height-point.y,140,24),"E: travel");
        }
        void OnDrawGizmos()
        {
            Gizmos.matrix=transform.localToWorldMatrix; Gizmos.color=new Color(.3f,1,.4f,.2f); Gizmos.DrawCube(Vector3.zero,areaSize);
            Gizmos.color=Color.green; Gizmos.DrawWireCube(Vector3.zero,areaSize);
        }
    }
}
