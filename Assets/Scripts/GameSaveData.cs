using System;
using System.Collections.Generic;
using UnityEngine;
namespace EverythingGrows
{
    [Serializable] public sealed class GameSaveData
    {
        public int version=1;
        public string currentScene;
        public string houseReturnScene;
        public Vector2 houseReturnPosition;
        public double worldSeconds;
        public int merchantSeed;
        public List<NightVisitSave> nightVisits=new();
        public RespawnSave respawn;
        public RespawnSave defaultRespawn;
        public PlayerSave player=new();
        public List<MapSave> maps=new();
        public List<GuildQuestSave> guildQuests=new();
    }
    [Serializable] public sealed class PlayerSave
    {
        public List<ItemStackSave> inventory=new();
        public int gold,selected,removedSlots,removedHearts,kills;
        public bool inventoryUsesSlots;
        public int farmingExperience; // Optional version-1 total XP; missing legacy value is zero.
        public float health,cycleElapsed;
        public long cycleNumber;
        public Vector2 position;
        public bool gameOver;
    }
    [Serializable] public sealed class ItemStackSave { public string item; public int count; public int slot; }
    [Serializable] public sealed class PlotSave
    {
        public Vector2Int cell;
        public string item;
        public bool wet,mature,ready,wilted,regrowing;
        public float progress,production;
    }
    [Serializable] public sealed class PieceSave { public string item; public Vector2 position,size; }
    [Serializable] public sealed class BuildingSave { public string item; public Vector2 position; }
    [Serializable] public sealed class RespawnSave { public string scene,anchor; public Vector2 position; public bool IsSet => !string.IsNullOrEmpty(scene); }
    [Serializable] public sealed class DeathCacheSave { public string id; public Vector2 position; public List<ItemStackSave> items=new(); }
    [Serializable] public sealed class RespawnAnchorSave { public string id; public Vector2 position; }
    [Serializable] public sealed class RegionalEnemySave
    {
        public string id,type,region,placementId;
        public Vector2 position,home,size;
        public float health;
        public bool provoked,bossEngaged,bossReturning;
        public double bossRespawnAt;
    }
    [Serializable] public sealed class ForageNodeSave
    {
        public string id,item;
        public int remaining;
        public bool harvested;
        public double readyAt;
    }
    [Serializable] public sealed class NightVisitSave
    {
        public string scene,profileId;
        public long cycleNumber;
        public bool appears;
        public Vector2 position;
    }
    [Serializable] public sealed class MapSave
    {
        public string scene;
        public List<DeathCacheSave> deathCaches=new();
        public List<RespawnAnchorSave> respawnAnchors=new();
        public double updatedAt;
        public List<ForageNodeSave> forageNodes=new();
        public List<PlotSave> plots=new();
        public List<PieceSave> pieces=new();
        public List<BuildingSave> buildings=new();
        public List<string> removedObjects=new();
        // Optional version-1 fields: missing legacy data means no recorded clear/kills.
        public bool regionalCombatSaved;
        public List<RegionalEnemySave> regionalEnemies=new();
        public bool combatCleared;
        public List<string> defeatedEnemies=new();
    }
}
