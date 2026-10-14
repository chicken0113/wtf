using UnityEngine;
namespace EverythingGrows
{
    public sealed class FarmItemPlacement : MonoBehaviour
    {
        public PlantDefinition item;
        private void OnDrawGizmosSelected() { Gizmos.color=Color.cyan; Gizmos.DrawWireSphere(transform.position,.65f); }
    }
}
