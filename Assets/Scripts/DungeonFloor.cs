using UnityEngine;
namespace EverythingGrows
{
    public enum DungeonFloorKind { Preparation, Normal, Boss }
    public enum FloorClearRule { AllHostiles, NoCombat }
    [DisallowMultipleComponent]
    public sealed class DungeonFloor : MonoBehaviour
    {
        [Min(1)] public int floorNumber=2;
        public DungeonFloorKind kind= DungeonFloorKind.Normal;
        public FloorClearRule clearRule=FloorClearRule.AllHostiles;
        public bool BlocksStairs(int livingHostiles)=>clearRule==FloorClearRule.AllHostiles && livingHostiles>0;
    }
}
