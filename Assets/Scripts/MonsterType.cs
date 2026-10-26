using System;
using UnityEngine;

namespace EverythingGrows
{
    public enum MonsterBehavior { LegacyPlacement, Melee, Ranged }
    [CreateAssetMenu(menuName="Everything Grows/Monster Type")]
    public sealed class MonsterType : ScriptableObject
    {
        public string label;
        [HideInInspector] public string saveId;
        public string SaveKey => string.IsNullOrEmpty(saveId)?name:saveId;
        public bool isBoss;
        public SpawnRegion[] spawnRegions=Array.Empty<SpawnRegion>();
        public MonsterBehavior behavior;
        [Min(1)] public float maxHealth=55;
        [Min(.1f)] public float moveSpeed=2;
        [Min(.1f)] public float returnSpeed=1.7f;
        [Min(.1f)] public float detectionRange=7;
        [Min(.1f)] public float attackRange=1.05f;
        [Min(.1f)] public float meleeHitRange=1.35f;
        [Min(1), Tooltip("Damage measured in half hearts.")] public int attackHalfHearts=1;
        [Min(0)] public float windupSeconds=.5f;
        [Min(.01f)] public float attackCooldown=1;
        [Min(.1f)] public float projectileSpeed=4.5f;
        [Min(.1f)] public float projectileLifetime=2.5f;
        [Min(0)] public int gold;
        [Tooltip("Each row rolls independently. Empty list means no item drops.")]
        public MonsterDrop[] drops=Array.Empty<MonsterDrop>();
        public bool IsRanged(bool fallback) => behavior==MonsterBehavior.LegacyPlacement?fallback:behavior==MonsterBehavior.Ranged;
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
