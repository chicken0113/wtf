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
        Undo.IncrementCurrentGroup();
        var overlay=MapPlacement.Place(map,ground,Vector2.zero,1,MapPlacement.Category.Ground);
        Check(overlay!=null && map.transform.Find("Ground").childCount==count+1,"Overlapping placement blocked or replaced the original");
        Check(overlay.GetComponent<SpriteRenderer>().sortingOrder>ground.GetComponent<SpriteRenderer>().sortingOrder,"Overlay not visible above original");
        Undo.PerformUndo(); Check(map.transform.Find("Ground").childCount==count,"Overlay undo removed original");
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
        var window=ScriptableObject.CreateInstance<FarmMapWindow>();
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        window.Choose(ground);
        typeof(FarmMapWindow).GetMethod("SetPainting",flags).Invoke(window,new object[]{true});
        window.Choose(item);
        Check((bool)typeof(FarmMapWindow).GetField("painting",flags).GetValue(window),"Prefab switching stopped painting");
        window.Choose(null);
        Check(!(bool)typeof(FarmMapWindow).GetField("painting",flags).GetValue(window),"Clearing prefab left painting active");
        UnityEngine.Object.DestroyImmediate(window);
        Debug.Log("MAP_PLACEMENT_VALIDATION_SUCCESS: overlapping blocks, visible overlays, undo, scoped erase and continuous prefab switching.");
    }
}
