using UnityEngine;

namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Farming Levels")]
    public sealed class FarmingLevelSettings : ScriptableObject
    {
        [Tooltip("Each entry is the XP needed for the next level: element 0 = level 1 to 2. Empty means level 1 is the maximum.")]
        public int[] experienceToNextLevel = {100,150,200,300};
        public int MaxLevel => 1+(experienceToNextLevel?.Length??0);
    }
}
