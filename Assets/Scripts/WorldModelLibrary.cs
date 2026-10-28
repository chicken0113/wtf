using UnityEngine;
namespace EverythingGrows
{
    public enum ModelKind3D { Effect, Player, Merchant, Enemy, RangedEnemy, Tree, Ground, Soil, Plant, Seed, Hoe, WateringCan, Sword, Bow, Stone, Cloud, Bed, Heart, House, Border, Projectile, Water, ForestGround }
    [CreateAssetMenu(menuName="Everything Grows/3D model library")]
    public sealed class WorldModelLibrary : ScriptableObject
    {
        [System.Serializable] public struct Entry { public ModelKind3D kind; public GameObject prefab; }
        public Entry[] models=System.Array.Empty<Entry>();
        public GameObject Get(ModelKind3D kind){foreach(var entry in models)if(entry.kind==kind)return entry.prefab;return null;}
        public GameObject Item(PlantDefinition item)
        {
            if(item==null)return Get(ModelKind3D.Effect);if(item.modelPrefab!=null)return item.modelPrefab;
            if(item.useEffect is RespawnUseEffect)return Get(ModelKind3D.Bed);
            return Get(item.isSeed?ModelKind3D.Seed:item.effect==GrowthEffect.Dig?ModelKind3D.Hoe:item.effect==GrowthEffect.Water?ModelKind3D.WateringCan:item.effect==GrowthEffect.Weapon?(item.ranged?ModelKind3D.Bow:ModelKind3D.Sword):item.effect==GrowthEffect.Heart?ModelKind3D.Heart:item.effect==GrowthEffect.House?ModelKind3D.House:item.effect==GrowthEffect.Crop?ModelKind3D.Plant:item.effect==GrowthEffect.Storage?ModelKind3D.Bed:item.effect==GrowthEffect.Produce?ModelKind3D.Stone:ModelKind3D.Plant);
        }
    }
}
