using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class MapSaveAuthoring
{
    const string CatalogPath="Assets/Resources/SaveItemCatalog.asset";
    static bool queued;
    static MapSaveAuthoring() { if(!Application.isBatchMode) ScheduleCatalog(); }
    public static void ScheduleCatalog()
    {
        if(queued) return; queued=true;
        EditorApplication.delayCall+=()=> { queued=false; if(EditorApplication.isCompiling || EditorApplication.isUpdating) { ScheduleCatalog(); return; } RefreshCatalog(); };
    }
    [MenuItem("Tools/Everything Grows/Refresh save item catalog")]
    public static void RefreshCatalog()
    {
        var entries=AssetDatabase.FindAssets("t:PlantDefinition").OrderBy(id=>id).Select(id=>new SaveItemCatalog.Entry {id=id,item=AssetDatabase.LoadAssetAtPath<PlantDefinition>(AssetDatabase.GUIDToAssetPath(id))}).Where(entry=>entry.item!=null).ToArray();
        var catalog=AssetDatabase.LoadAssetAtPath<SaveItemCatalog>(CatalogPath);
        if(catalog==null) { catalog=ScriptableObject.CreateInstance<SaveItemCatalog>(); catalog.entries=entries; AssetDatabase.CreateAsset(catalog,CatalogPath); }
        else
        {
            if(catalog.entries.Length==entries.Length && catalog.entries.Zip(entries,(a,b)=>a.id==b.id && a.item==b.item).All(same=>same)) return;
            catalog.entries=entries; EditorUtility.SetDirty(catalog);
        }
        AssetDatabase.SaveAssetIfDirty(catalog);
    }
    [MenuItem("Tools/Everything Grows/Map travel/Prepare current map IDs")]
    public static void PrepareCurrentMap()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene(); var used=new System.Collections.Generic.HashSet<string>();
        foreach(var root in scene.GetRootGameObjects()) foreach(var building in root.GetComponentsInChildren<FarmBuilding>(true))
        {
            var marker=building.GetComponent<MapObjectId>(); if(marker==null) marker=Undo.AddComponent<MapObjectId>(building.gameObject);
            if(string.IsNullOrEmpty(marker.Id) || !used.Add(marker.Id)) { Undo.RecordObject(marker,"Assign unique map object ID"); marker.AssignNew(); used.Add(marker.Id); }
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker); EditorUtility.SetDirty(marker);
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }
    [MenuItem("Tools/Everything Grows/Map travel/Enable current scene in Build Settings")]
    public static void EnableCurrentScene()
    {
        string path=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if(string.IsNullOrEmpty(path)) throw new Exception("Save the scene first.");
        var scenes=EditorBuildSettings.scenes.ToList(); var existing=scenes.Find(scene=>scene.path==path);
        if(existing!=null) existing.enabled=true; else scenes.Add(new EditorBuildSettingsScene(path,true)); EditorBuildSettings.scenes=scenes.ToArray();
    }
    [MenuItem("GameObject/Everything Grows/Map Exit",false,20)]
    public static void CreateExit() { Create<MapExit>("Map Exit"); }
    [MenuItem("GameObject/Everything Grows/Map Arrival Point",false,21)]
    public static void CreateArrival() { Create<MapArrivalPoint>("Map Arrival Point"); }
    static void Create<T>(string label) where T:Component
    {
        var root=new GameObject(label); Undo.RegisterCreatedObjectUndo(root,"Create "+label);
        if(Selection.activeTransform!=null) root.transform.SetParent(Selection.activeTransform,false);
        root.AddComponent<T>(); Selection.activeGameObject=root;
    }
    [MenuItem("Tools/Everything Grows/Map travel/Create placement prefabs")]
    public static void CreatePrefabs()
    {
        SavePrefab<MapExit>("Assets/Prefabs/Map/MapExit.prefab","Map Exit");
        SavePrefab<MapArrivalPoint>("Assets/Prefabs/Map/MapArrivalPoint.prefab","Map Arrival Point");
    }
    static void SavePrefab<T>(string path,string label) where T:Component
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null) return;
        var root=new GameObject(label); root.AddComponent<T>(); PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root);
    }
    public static void GenerateAssets() { RefreshCatalog(); CreatePrefabs(); }
}
public sealed class SaveCatalogImporter : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] oldPaths)
    {
        if(Application.isBatchMode) return;
        bool Relevant(string path) => path.EndsWith(".asset") && path!="Assets/Resources/SaveItemCatalog.asset";
        if(imported.Concat(deleted).Concat(moved).Any(Relevant)) MapSaveAuthoring.ScheduleCatalog();
    }
}
[CustomEditor(typeof(MapExit))]
public sealed class MapExitInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); var path=serializedObject.FindProperty("destinationScene");
        var selected=AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
        EditorGUI.BeginChangeCheck(); selected=(SceneAsset)EditorGUILayout.ObjectField("Destination Scene",selected,typeof(SceneAsset),false);
        if(EditorGUI.EndChangeCheck()) path.stringValue=selected!=null?AssetDatabase.GetAssetPath(selected):"";
        EditorGUILayout.PropertyField(serializedObject.FindProperty("arrivalId")); EditorGUILayout.PropertyField(serializedObject.FindProperty("areaSize")); var interact=serializedObject.FindProperty("requireInteract");
        interact.boolValue=EditorGUILayout.Popup("Activation",interact.boolValue?0:1,new[]{"Door - press F (teal)","Boundary - touch (orange)"})==0; serializedObject.ApplyModifiedProperties();
        if(!string.IsNullOrEmpty(path.stringValue) && !EditorBuildSettings.scenes.Any(scene=>scene.enabled && scene.path==path.stringValue))
        {
            EditorGUILayout.HelpBox("Destination must be enabled in Build Settings.",MessageType.Warning);
            if(GUILayout.Button("Enable destination in Build Settings"))
            {
                var scenes=EditorBuildSettings.scenes.ToList(); var found=scenes.Find(scene=>scene.path==path.stringValue);
                if(found!=null) found.enabled=true; else scenes.Add(new EditorBuildSettingsScene(path.stringValue,true)); EditorBuildSettings.scenes=scenes.ToArray();
            }
        }
        EditorGUILayout.HelpBox("Place this area in the Scene. The target scene needs a FarmGame and an active Map Arrival Point with the same Arrival Id. Door: stand inside and press F. Boundary: touch the orange area to travel automatically.",MessageType.Info);
    }
    void OnSceneGUI() { var exit=(MapExit)target; Handles.Label(exit.transform.position+Vector3.up,(exit.requireInteract?"[F Door] ":"[Touch] ")+exit.arrivalId+" → "+System.IO.Path.GetFileNameWithoutExtension(exit.destinationScene)); }
}
[CustomEditor(typeof(MapArrivalPoint))]
public sealed class MapArrivalInspector : Editor
{
    public override void OnInspectorGUI() { DrawDefaultInspector(); EditorGUILayout.HelpBox("Give each arrival in this scene a unique ID. Prefer a point outside the exit area. Overlapping a touch exit requires leaving and re-entering before it can travel.",MessageType.Info); }
    void OnSceneGUI() { var point=(MapArrivalPoint)target; Handles.Label(point.transform.position+Vector3.up,point.arrivalId); }
}
