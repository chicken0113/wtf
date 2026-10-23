using System;
using UnityEngine;

namespace EverythingGrows
{
    [CreateAssetMenu(menuName="Everything Grows/Monster Type")]
    public sealed class MonsterType : ScriptableObject
    {
        public string label;
        [Min(0)] public int gold;
        [Tooltip("Each row rolls independently. Empty list means no item drops.")]
        public MonsterDrop[] drops=Array.Empty<MonsterDrop>();
        public static MonsterType Default(bool ranged) => Resources.Load<MonsterType>(ranged?"Monsters/Spitter":"Monsters/Chaser");
    }
    [Serializable] public sealed class MonsterDrop
    {
        public PlantDefinition item;
        [Range(0,1), Tooltip("0 = never, 1 = guaranteed. Each row rolls separately.")] public float probability=1;
        [Min(1)] public int minimum=1;
        [Min(1)] public int maximum=1;
    }
}
