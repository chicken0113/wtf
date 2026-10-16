using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class MapExitTypesSetup
{
    const string Request="Library/MapExitTypes.request",Result="Library/MapExitTypes.result";
    public const string DoorPath="Assets/Prefabs/Map/MapDoorExit.prefab",TouchPath="Assets/Prefabs/Map/MapBoundaryExit.prefab";
    static MapExitTypesSetup() { if(!Application.isBatchMode) EditorApplication.update+=Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        File.Delete(Request);
        try { Apply(); File.WriteAllText(Result,"SUCCESS"); } catch(Exception error) { Debug.LogException(error); File.WriteAllText(Result,error.ToString()); }
    }
    static SpriteRenderer Part(Transform parent,string name,Vector2 position,Vector2 size,int order)
    {
        var root=new GameObject(name); root.transform.SetParent(parent,false); root.transform.localPosition=position; root.transform.localScale=new Vector3(size.x,size.y,1);
        var model=root.AddComponent<SpriteRenderer>(); model.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MapSprites/TestBlock.png"); model.sortingOrder=order; return model;
    }
    static void MakePrefab(string path,bool door)
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null) return;
        var root=new GameObject(door?"Door Exit - E":"Boundary Exit - Touch"); var exit=root.AddComponent<MapExit>(); exit.requireInteract=door; exit.areaSize=door?new Vector2(1.8f,2.4f):new Vector2(2.4f,1.2f);
        var visual=root.AddComponent<MapExitVisual>(); visual.floor=Part(root.transform,"Travel area",Vector2.zero,exit.areaSize,-30);
        if(door) visual.accents=new[]{Part(root.transform,"Left post",Vector2.left,new Vector2(.2f,2.6f),20),Part(root.transform,"Right post",Vector2.right,new Vector2(.2f,2.6f),20),Part(root.transform,"Lintel",Vector2.up*1.3f,new Vector2(2.2f,.2f),21)};
        else
        {
            var left=Part(root.transform,"Direction upper",new Vector2(.15f,.16f),new Vector2(.6f,.12f),21); left.transform.localRotation=Quaternion.Euler(0,0,-35);
            var right=Part(root.transform,"Direction lower",new Vector2(.15f,-.16f),new Vector2(.6f,.12f),21); right.transform.localRotation=Quaternion.Euler(0,0,35);
            left.color=right.color=Color.white; visual.accents=Array.Empty<SpriteRenderer>();
        }
        visual.Refresh(); PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root);
    }
    public static void GeneratePrefabs() { MakePrefab(DoorPath,true); MakePrefab(TouchPath,false); }
    [MenuItem("GameObject/Everything Grows/Door Exit (E)",false,23)]
    public static void Door() { Place(DoorPath); }
    [MenuItem("GameObject/Everything Grows/Boundary Exit (Touch)",false,24)]
    public static void Touch() { Place(TouchPath); }
    static void Place(string path)
    {
        GeneratePrefabs(); var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        Undo.RegisterCreatedObjectUndo(root,"Place map exit"); if(Selection.activeTransform!=null) root.transform.SetParent(Selection.activeTransform,false); Selection.activeGameObject=root;
    }
    static void AddConnection(Scene scene)
    {
        bool farm=scene.path==CombatTestMapSetup.FarmScene;
        if(!farm && scene.path!=CombatTestMapSetup.CombatScene) return;
        var layout=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmMapLayout>()).FirstOrDefault(); if(layout==null) throw new Exception("Map layout missing.");
        if(layout.transform.Find("Touch Map Connection")!=null) return;
        var group=new GameObject("Touch Map Connection"); group.transform.SetParent(layout.transform,false); Undo.RegisterCreatedObjectUndo(group,"Add touch travel connection");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TouchPath),group.transform);
        root.transform.position=farm?new Vector2(9,0):new Vector2(-10,2.5f);
        var exit=root.GetComponent<MapExit>(); exit.destinationScene=farm?CombatTestMapSetup.CombatScene:CombatTestMapSetup.FarmScene; exit.arrivalId=farm?"combat_path_entry":"farm_path_entry"; PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
        var arrival=new GameObject("Touch path arrival").AddComponent<MapArrivalPoint>(); arrival.transform.SetParent(group.transform,false); arrival.transform.position=farm?new Vector2(9,-2):new Vector2(-8,2.5f); arrival.arrivalId=farm?"farm_path_entry":"combat_path_entry";
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    [MenuItem("Tools/Everything Grows/Map travel/Add touch connections to test maps")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play to add map connections.");
        GeneratePrefabs();
        var active=SceneManager.GetActiveScene();
        foreach(string path in new[]{CombatTestMapSetup.FarmScene,CombatTestMapSetup.CombatScene})
        {
            var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.IsValid() || !scene.isLoaded;
            if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try { AddConnection(scene); } finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        SceneManager.SetActiveScene(active); AssetDatabase.SaveAssets(); Debug.Log("MAP_EXIT_TYPES_SUCCESS");
    }
    public static void Batch() { if(!Application.isBatchMode) throw new Exception("Use isolated validation."); EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene); Apply(); }
}
