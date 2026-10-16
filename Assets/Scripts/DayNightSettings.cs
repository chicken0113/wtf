using UnityEngine;
namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Day Night Settings")]
    public sealed class DayNightSettings : ScriptableObject
    {
        [Min(1)] public float daylightSeconds=180;
        [Min(1)] public float duskSeconds=30;
        [Min(1)] public float nightSeconds=120;
        [Min(1)] public float dawnSeconds=30;
        [Range(0,.9f)] public float nightDarkness=.6f;
    }
}
