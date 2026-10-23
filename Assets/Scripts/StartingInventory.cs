using System;
using UnityEngine;
namespace EverythingGrows
{
    [Serializable] public sealed class StartingItem
    {
        public PlantDefinition item;
        [Min(1)] public int quantity=1;
    }
    [CreateAssetMenu(menuName="Everything Grows/Starting Inventory")]
    public sealed class StartingInventory : ScriptableObject
    {
        [Header("Inventory capacity (applied when Play starts)")]
        [Range(1,9)] public int hotbarSlots=4;
        [Min(0)] public int storageSlots=1;
        public StartingItem[] items;
    }
}
