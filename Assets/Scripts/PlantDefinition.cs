using UnityEngine;
namespace EverythingGrows
{
    public enum GrowthEffect { Dig, Water, Produce, Storage, Weapon, Crop, Scythe, Heart, House }
    public enum PlantGrowthType { Legacy, Harvest, Producer, Functional }
    public enum PlantFunction { None, Till, Water, Storage }
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
        [Header("Planted growth")]
        [Tooltip("Legacy is retained for old assets only. Select a growth type in the Inspector.")]
        public PlantGrowthType growthType;
        public PlantFunction growthFunction;
        [Min(1)] public int harvestQuantity = 1;
        [Min(0), Tooltip("0 performs the function every tick. Storage is always passive.")]
        public float functionSeconds = 1;
        public PlantGrowthType GrowthType => growthType!=PlantGrowthType.Legacy?growthType:
            (isSeed && regrowSeconds>0 && harvestResult!=null?PlantGrowthType.Producer:
            effect==GrowthEffect.Produce && harvestResult==null?PlantGrowthType.Producer:
            effect==GrowthEffect.Dig || effect==GrowthEffect.Water || effect==GrowthEffect.Storage?PlantGrowthType.Functional:PlantGrowthType.Harvest);
        public PlantFunction Function => GrowthType!=PlantGrowthType.Functional?PlantFunction.None:
            growthType!=PlantGrowthType.Legacy?growthFunction:
            effect==GrowthEffect.Dig?PlantFunction.Till:effect==GrowthEffect.Water?PlantFunction.Water:effect==GrowthEffect.Storage?PlantFunction.Storage:PlantFunction.None;
        public float FunctionInterval => growthType==PlantGrowthType.Legacy?0:functionSeconds;
        public PlantDefinition HarvestItem => harvestResult!=null?harvestResult:this;
        public bool UsesRegrowth => GrowthType==PlantGrowthType.Producer && regrowSeconds>0;
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
