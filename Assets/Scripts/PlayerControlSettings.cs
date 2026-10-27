using UnityEngine;
namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Player controls")]
    public sealed class PlayerControlSettings : ScriptableObject
    {
        [Min(.1f)] public float moveSpeed=4.5f;
        [Min(.1f)] public float rollSpeed=11;
        [Min(.02f)] public float rollDuration=.22f;
        [Min(0)] public float rollCooldown=.65f;
        [Range(0,.3f)] public float rollInputBuffer=.12f;
        [Header("Action feedback")]
        [HideInInspector] public float projectileSpeed=18; // Legacy serialized value; ranged items now own speed.
        [Range(0,.3f)] public float weaponRecoil=.12f;
        [Range(0,.2f)] public float cameraShake=.06f;
        [Min(.02f)] public float toolDragInterval=.1f;
    }
}
