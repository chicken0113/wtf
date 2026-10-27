using UnityEngine;
namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Forage Definition")]
    public sealed class ForageDefinition : ScriptableObject
    {
        public string label;
        public PlantDefinition item;
        [Min(1)] public int quantity=1;
        public bool respawns=true;
        [Min(.1f)] public float respawnSeconds=60;
    }
}
