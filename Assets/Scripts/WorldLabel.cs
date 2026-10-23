using UnityEngine;
namespace EverythingGrows
{
    public sealed class WorldLabel : MonoBehaviour
    {
        public string text;
        public float visibleDistance=6;
        public string DisplayText
        {
            get
            {
                var exit=GetComponentInParent<MapExit>();
                return (exit!=null&&exit.requireInteract)||GetComponentInParent<ShopNpc>()!=null
                    ? (text??"").Replace("[E]","[RMB]").Replace("[F]","[RMB]").Replace("E:","RMB:").Replace("F:","RMB:") : text;
            }
        }
        void OnGUI()
        {
            if(!Application.isPlaying || Camera.main==null) return;
            var npc=GetComponentInParent<ShopNpc>(); if(npc!=null && !npc.IsVisiting) return;
            var farm=Object.FindFirstObjectByType<FarmGame>();
            if(farm==null || farm.WorldInputBlocked || farm.Combat.IsGameOver || Vector2.Distance(farm.PlayerPosition,transform.position)>visibleDistance) return;
            var screen=Camera.main.WorldToScreenPoint(transform.position); if(screen.z<0 || screen.x<0 || screen.x>Screen.width || screen.y<0 || screen.y>Screen.height) return;
            var style=new GUIStyle(GUI.skin.box) {alignment=TextAnchor.MiddleCenter,fontSize=13};
            var content=new GUIContent(DisplayText); float width=style.CalcSize(content).x+14;
            GUI.Box(new Rect(screen.x-width*.5f,Screen.height-screen.y-24,width,24),content,style);
        }
    }
}
