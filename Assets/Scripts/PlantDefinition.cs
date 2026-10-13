using UnityEngine;
namespace EverythingGrows
{
    public enum GrowthEffect { Dig, Water, Produce, Storage }
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
    }
}
