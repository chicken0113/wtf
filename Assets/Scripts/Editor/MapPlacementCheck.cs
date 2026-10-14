using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class MapPlacementCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated batch project.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        var map=UnityEngine.Object.FindFirstObjectByType<FarmMapLayout>();
        var ground=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/GroundTile.prefab");
        var item=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/SwordPickup.prefab");
        void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
        int count=map.transform.Find("Ground").childCount;
        Check(MapPlacement.Place(map,ground,Vector2.zero,1,MapPlacement.Category.Ground)==null,"Duplicate ground allowed");
        Check(map.transform.Find("Ground").childCount==count,"Duplicate placement changed map");
        Undo.IncrementCurrentGroup();
        var placed=MapPlacement.Place(map,ground,new Vector2(26,0),1,MapPlacement.Category.Ground);
        Check(placed!=null && placed.transform.parent.name=="Ground" && PrefabUtility.IsPartOfPrefabInstance(placed),"Ground placement is not a prefab in the right layer");
        Undo.PerformUndo(); Check(map.transform.Find("Ground").childCount==count,"Placement undo failed");
        Undo.IncrementCurrentGroup();
        var sword=MapPlacement.Place(map,item,Vector2.zero,.8f,MapPlacement.Category.Items);
        Check(sword!=null && sword.GetComponent<FarmItemPlacement>().item!=null,"Item placement lost definition");
        int items=map.transform.Find("Items").childCount;
        Undo.IncrementCurrentGroup();
        Check(MapPlacement.Erase(map,Vector2.zero,.3f,MapPlacement.Category.Items),"Erase failed");
        Check(map.transform.Find("Items").childCount==items-1 && map.transform.Find("Ground").childCount==count,"Erase affected another layer");
        Undo.PerformUndo(); Check(map.transform.Find("Items").childCount==items,"Erase undo failed");
        Debug.Log("MAP_PLACEMENT_VALIDATION_SUCCESS: prefab references, layers, duplicate prevention, placement undo and deletion undo.");
    }
}
