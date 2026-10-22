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
    }
}
