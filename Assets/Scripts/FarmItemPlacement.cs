using UnityEngine;
namespace EverythingGrows
{
    public sealed class FarmItemPlacement : MonoBehaviour
    {
        public PlantDefinition item;
        [Tooltip("Optional gathering node. Empty retains an ordinary loose item; gathering settings take priority over Item.")]
        public ForageDefinition forage;
        public PlantDefinition DisplayItem => forage!=null?forage.item:item;
        private void OnDrawGizmosSelected() { Gizmos.color=Color.cyan; Gizmos.DrawWireSphere(transform.position,.65f); }
    }
}
