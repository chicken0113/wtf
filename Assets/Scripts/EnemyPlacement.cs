using UnityEngine;
namespace EverythingGrows
{
    public sealed class EnemyPlacement : MonoBehaviour
    {
        public bool ranged;
        private void OnDrawGizmosSelected() { Gizmos.color=Color.red; Gizmos.DrawWireSphere(transform.position,7); }
    }
}
