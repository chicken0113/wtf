using UnityEngine;
namespace EverythingGrows
{
    public enum GrowthEffect { Dig, Water, Produce, Storage, Weapon, Crop, Scythe, Heart }
    [CreateAssetMenu(menuName="Everything Grows/Plant Definition")]
    public sealed class PlantDefinition : ScriptableObject
    {
        public string label;
        public Color color = Color.white;
        public GrowthEffect effect;
        [Min(.1f)] public float growthSeconds = 6;
        [Min(.1f)] public float effectRadius = 2;
        [Min(.1f)] public float productionSeconds = 8;
        [Min(1)] public int storageBonus = 3;
        [Header("Harvest transformation")]
        public PlantDefinition harvestResult;
        public bool transformed;
        public int iconIndex = -1;
        [Min(0)] public int sellPrice = 5;
        [Header("Crop farming")]
        public bool isSeed;
        public bool stackable;
        [Min(0)] public float regrowSeconds;
        [Header("Combat")]
        public bool ranged;
        [Min(1)] public float damage = 15;
        [Min(.1f)] public float attackRange = 1.8f;
        [Min(.1f)] public float attackInterval = .45f;
    }
}
