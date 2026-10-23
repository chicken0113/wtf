using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class EditorSessionSetup
{
    const string Request = "Library/WtfSession.request";
    const string Result = "Library/WtfSession.result.json";
    [Serializable] private sealed class Status { public string state, project, scene, error; public bool playing, hasFarm, hasCamera, orthographic; }
    static EditorSessionSetup() { if (!Application.isBatchMode) EditorApplication.update += Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        string mode=File.ReadAllText(Request).Trim();
        if((mode=="setup" || mode=="map" || mode=="starterfarm" || mode=="hospital") && EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(mode=="hospital" && Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)) return;
        File.Delete(Request);
        try { if(mode=="setup") SetupCurrent(); else if(mode=="map") EditableMapBuilder.Setup(); else if(mode=="starterfarm") StarterFarmBuilder.Setup(); else if(mode=="hospital") HospitalSetup.Apply(); else if(mode!="status") throw new Exception("Unknown request"); WriteStatus(mode=="setup"?"SETUP_SUCCESS":mode=="map"?"MAP_SUCCESS":mode=="starterfarm"?"STARTER_FARM_SUCCESS":mode=="hospital"?"HOSPITAL_SUCCESS":"STATUS",null); }
        catch(Exception error) { Debug.LogException(error); WriteStatus("ERROR",error.ToString()); }
    }
    static T FindInScene<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    static void WriteStatus(string state,string error)
    {
        var scene=SceneManager.GetActiveScene(); var camera=FindInScene<Camera>(scene);
        File.WriteAllText(Result,JsonUtility.ToJson(new Status { state=state,project=Application.dataPath,scene=scene.path,playing=EditorApplication.isPlayingOrWillChangePlaymode,
            hasFarm=FindInScene<FarmGame>(scene)!=null,hasCamera=camera!=null,orthographic=camera!=null && camera.orthographic,error=error },true));
    }
    // One-time authoring entry retained for fixtures; no production menu.
    public static void SetupCurrent()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before setting up the scene.");
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid() || !scene.isLoaded) throw new Exception("No active scene.");
        string[] names={"Hoe","WateringCan","Stone","Cloud","InventorySlot"};
        var definitions=names.Select(name=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+name+".asset")).ToArray();
        if(definitions.Any(data=>data==null)) throw new Exception("Plant definitions are missing.");
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Set up 2D farming scene");
        var farm=FindInScene<FarmGame>(scene);
        if(farm==null) { var root=new GameObject("Farm"); SceneManager.MoveGameObjectToScene(root,scene); Undo.RegisterCreatedObjectUndo(root,"Create farm"); farm=Undo.AddComponent<FarmGame>(root); }
        Undo.RecordObject(farm,"Configure farm"); farm.Configure(definitions); EditorUtility.SetDirty(farm);
        var camera=FindInScene<Camera>(scene);
        if(camera==null) { var root=new GameObject("Main Camera"); SceneManager.MoveGameObjectToScene(root,scene); Undo.RegisterCreatedObjectUndo(root,"Create camera"); camera=Undo.AddComponent<Camera>(root); }
        Undo.RecordObjects(new UnityEngine.Object[]{camera,camera.transform,camera.gameObject},"Configure top-down camera");
        camera.gameObject.SetActive(true); camera.gameObject.tag="MainCamera"; camera.enabled=true;
        camera.orthographic=true; camera.orthographicSize=6.7f; camera.transform.SetPositionAndRotation(new Vector3(0,0,-10),Quaternion.identity);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.22f,.22f);
        EditorSettings.defaultBehaviorMode=EditorBehaviorMode.Mode2D;
        if(SceneView.lastActiveSceneView!=null) { var view=SceneView.lastActiveSceneView; view.in2DMode=true; view.LookAt(Vector3.zero,Quaternion.identity,8,true); }
        EditorSceneManager.MarkSceneDirty(scene);
        string path=string.IsNullOrEmpty(scene.path)?AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/FirstGarden.unity"):scene.path;
        if(!EditorSceneManager.SaveScene(scene,path)) throw new Exception("Could not save the current scene.");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)}.Concat(EditorBuildSettings.scenes.Where(entry=>entry.path!=path)).ToArray();
        Undo.CollapseUndoOperations(group); Selection.activeGameObject=farm.gameObject;
        Debug.Log("WTF_CURRENT_SCENE_SETUP_SUCCESS: "+path);
    }
}
