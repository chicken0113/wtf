using System;
using System.Collections.Generic;
using UnityEngine;
namespace EverythingGrows
{
    public enum GuildEnemyKind { Any, Melee, Ranged }
    [Serializable] public sealed class GuildQuestDefinition
    {
        [Tooltip("Permanent save identifier. Do not rename after publishing.")] public string id;
        public string title;
        [TextArea] public string description;
        public string targetScene="Assets/Scenes/LegacyVillage/CombatTest.unity";
        public GuildEnemyKind enemyKind;
        [Min(1)] public int requiredKills=2;
        [Min(0)] public int rewardGold=50;
        public bool repeatable=true;
    }
    [CreateAssetMenu(menuName="Everything Grows/Guild quests")]
    public sealed class GuildQuestCatalog : ScriptableObject
    {
        public List<GuildQuestDefinition> quests=new();
    }
    [Serializable] public sealed class GuildQuestSave
    {
        public string id,targetScene;
        public GuildEnemyKind enemyKind;
        public int requiredKills,rewardGold,progress,completedRuns;
        public bool accepted;
    }
}
