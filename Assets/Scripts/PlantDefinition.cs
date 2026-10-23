using UnityEngine;
namespace EverythingGrows
{
    public enum GrowthEffect { Dig, Water, Produce, Storage, Weapon, Crop, Scythe, Heart, House }
    [CreateAssetMenu(menuName="Everything Grows/Plant Definition")]
    public sealed class PlantDefinition : ScriptableObject
    {
        public string label;
        [TextArea(2,5)] public string description;
        public Color color = Color.white;
        public GrowthEffect effect;
        [Header("Farming progression")]
        [Min(1), Tooltip("Required farming level to plant this item. Does not restrict use, trade or pickup.")]
        public int requiredFarmingLevel = 1;
        [Min(0), Tooltip("XP per successful harvest of this planted item, including repeat harvests. Not awarded by pickup or hoe removal.")]
        public int harvestExperience = 10;
        [Min(.1f)] public float growthSeconds = 6;
        [Min(.1f)] public float effectRadius = 2;
        [Min(.1f)] public float productionSeconds = 8;
        [Min(0)] public int storageBonus = 3;
        [Header("Harvest transformation")]
        public PlantDefinition harvestResult;
        public bool transformed;
        public int iconIndex = -1;
        [Header("Item appearance")]
        public Sprite itemSprite;
        [Min(.1f)] public float plantedDisplaySize=.85f;
        [Min(.1f)] public float matureDisplaySize=1;
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
        public string UsageText
        {
            get
            {
                if(!string.IsNullOrWhiteSpace(description))return description;
                if(isSeed)return "Plant on tilled soil and water to grow a crop.";
                return effect switch {
                    GrowthEffect.Dig => "Left click to till soil or remove a planted object.",
                    GrowthEffect.Water => "Left click to water tilled soil.",
                    GrowthEffect.Scythe => "A farming tool. Try planting it to see what grows.",
                    GrowthEffect.Weapon => "Equip in the hotbar and left click to attack.",
                    GrowthEffect.Storage => "Plant and water to increase inventory capacity.",
                    GrowthEffect.Heart => "Left click to reattach a missing heart slot.",
                    GrowthEffect.House => "Left click to place a house.",
                    _ => "Sell it, or plant it to see what grows."
                };
            }
        }
    }
}
