using UnityEngine;
namespace EverythingGrows
{
    public sealed class ShopNpc : MonoBehaviour
    {
        public string npcId="farm_merchant";
        [Min(.1f)] public float interactionRange=2.2f;
        private void LateUpdate() { var renderer=GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.sortingOrder=30-Mathf.RoundToInt(transform.position.y*10); }
        private void OnDrawGizmosSelected() { Gizmos.color=Color.cyan; Gizmos.DrawWireSphere(transform.position,interactionRange); }
    }
}
