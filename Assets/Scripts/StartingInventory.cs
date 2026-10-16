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
        public StartingItem[] items;
    }
}
