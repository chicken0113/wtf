using UnityEngine;
namespace EverythingGrows
{
    // A new special action needs one effect asset/class, not changes to input or inventory.
    public abstract class ItemUseEffect : ScriptableObject
    {
        public bool repeatWhileHeld;
        public abstract bool TryUse(FarmGame farm,PlantDefinition item,Vector2 target);
    }
}
