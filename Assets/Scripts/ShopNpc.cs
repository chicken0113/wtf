using UnityEngine;
namespace EverythingGrows
{
    public sealed class ShopNpc : MonoBehaviour
    {
        [Tooltip("Non-shop NPCs only. Merchant shops use the shared night schedule regardless of this legacy field.")]
        public bool daytimeOnly=true;
        public string npcId="farm_merchant";
        [Min(.1f)] public float interactionRange=2.2f;
        private bool homeCaptured;
        private Vector2 homePosition;
        public Vector2 HomePosition { get { if(!homeCaptured){homePosition=transform.position;homeCaptured=true;}return homePosition; } }
        private Renderer[] visuals;
        private bool[] visualStates;
        private Collider2D[] colliders;
        private bool[] colliderStates;
        public bool IsVisiting { get; private set; } = true;
        public void SetVisiting(bool visiting)
        {
            if(visuals==null)
            {
                visuals=GetComponentsInChildren<Renderer>(true); visualStates=new bool[visuals.Length];
                for(int i=0;i<visuals.Length;i++) visualStates[i]=visuals[i].enabled;
                colliders=GetComponentsInChildren<Collider2D>(true); colliderStates=new bool[colliders.Length];
                for(int i=0;i<colliders.Length;i++) colliderStates[i]=colliders[i].enabled;
            }
            IsVisiting=visiting;
            for(int i=0;i<visuals.Length;i++) if(visuals[i]!=null) visuals[i].enabled=visiting && visualStates[i];
            for(int i=0;i<colliders.Length;i++) if(colliders[i]!=null) colliders[i].enabled=visiting && colliderStates[i];
        }
        private void LateUpdate() { var renderer=GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.sortingOrder=30-Mathf.RoundToInt(transform.position.y*10); }
        private void OnDrawGizmosSelected() { Gizmos.color=Color.cyan; Gizmos.DrawWireSphere(transform.position,interactionRange); }
    }
}
