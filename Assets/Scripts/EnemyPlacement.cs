using UnityEngine;
namespace EverythingGrows
{
    public sealed class EnemyPlacement : MonoBehaviour
    {
        [Tooltip("Shared monster type and rewards. Empty uses Chaser/Spitter defaults based on Ranged. No per-placement reward overrides.")]
        public MonsterType monsterType;
        public bool ranged;
        [Tooltip("Stationary practice target: uses normal weapon hits, never attacks or drops loot.")]
        public bool trainingDummy;
        private void OnDrawGizmosSelected() { Gizmos.color=Color.red; Gizmos.DrawWireSphere(transform.position,7); }
    }
}
