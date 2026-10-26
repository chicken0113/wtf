using UnityEngine;
namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Item Effects/Set Respawn")]
    public sealed class RespawnUseEffect : ItemUseEffect
    {
        [Tooltip("Place a camping bed at the pointed position. Otherwise set the current player position. One item is consumed on success.")]
        public bool placeAnchor;
        public override bool TryUse(FarmGame farm,PlantDefinition item,Vector2 target) => farm.UseRespawnItem(item,target,placeAnchor);
    }
}
