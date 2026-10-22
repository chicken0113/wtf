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
        public PlayerSave player=new();
        public List<MapSave> maps=new();
    }
    [Serializable] public sealed class PlayerSave
    {
        public List<ItemStackSave> inventory=new();
        public int gold,selected,removedSlots,removedHearts,kills;
        public float health,cycleElapsed;
        public Vector2 position;
        public bool gameOver;
    }
    [Serializable] public sealed class ItemStackSave { public string item; public int count; }
    [Serializable] public sealed class PlotSave
    {
        public Vector2Int cell;
        public string item;
        public bool wet,mature,ready,wilted,regrowing;
        public float progress,production;
    }
    [Serializable] public sealed class PieceSave { public string item; public Vector2 position,size; }
    [Serializable] public sealed class BuildingSave { public string item; public Vector2 position; }
    [Serializable] public sealed class MapSave
    {
        public string scene;
        public double updatedAt;
        public List<PlotSave> plots=new();
        public List<PieceSave> pieces=new();
        public List<BuildingSave> buildings=new();
        public List<string> removedObjects=new();
    }
}
