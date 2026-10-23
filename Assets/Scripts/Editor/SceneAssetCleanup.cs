using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SceneAssetCleanup
{
    const string Request="Library/SceneAssetCleanup.request",Result="Library/SceneAssetCleanup.result";
    static readonly string[] Retired={"Assets/Scenes/Garden.unity","Assets/Scenes/StarterFarm.unity","Assets/Scenes/EditableField.unity","Assets/Scenes/DailyFarm.unity"};
    static string[] Current=>TriangleWorldSetup.Installed?TriangleWorldSetup.Paths:new[]{CombatTestMapSetup.FarmScene,WorldFrameworkSetup.Town,WorldFrameworkSetup.Store,CombatTestMapSetup.CombatScene};
    static SceneAssetCleanup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText(Result,"WAITING_FOR_PLAY_STOP");return;}
        if(Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty&&Retired.Contains(s.path))){File.WriteAllText(Result,"WAITING_FOR_RETIRED_SCENE_SAVE");return;}
        File.Delete(Request);try{Apply();File.WriteAllText(Result,"SUCCESS");}catch(Exception error){Debug.LogException(error);File.WriteAllText(Result,"ERROR: "+error);}
    }
    // One-time authoring entry retained for fixtures; no production menu.
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before cleaning scene assets.");
        foreach(var path in Current)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)throw new Exception("Required scene missing: "+path);
        // Asset dependencies catch GUID references; serialized travel paths need a separate check.
        var candidates=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!Retired.Contains(p)&&(p.EndsWith(".unity")||p.EndsWith(".prefab")||p.EndsWith(".asset"))).ToArray();
        foreach(var path in candidates)
        {
            var reference=AssetDatabase.GetDependencies(path,false).FirstOrDefault(Retired.Contains);
            if(reference!=null)throw new Exception("Retired scene still referenced by "+path+": "+reference);
            if(path.EndsWith(".unity")||path.EndsWith(".prefab"))
            {
                var text=File.ReadAllText(path);if(Retired.Any(retired=>text.Contains(retired)))throw new Exception("Travel reference to retired scene in "+path);
            }
        }
        var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>Retired.Contains(s.path)).ToArray();
        if(loaded.Any(s=>s.isDirty))throw new Exception("Save modified retired scenes before cleanup.");
        foreach(var scene in loaded)
        {
            if(SceneManager.GetActiveScene()==scene)
            {
                var farm=SceneManager.GetSceneByPath(CombatTestMapSetup.FarmScene);if(!farm.IsValid()||!farm.isLoaded)farm=EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene,OpenSceneMode.Additive);SceneManager.SetActiveScene(farm);
            }
            EditorSceneManager.CloseScene(scene,true);
        }
        EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>Current.Contains(s.path)).ToArray();
        int removed=0;foreach(var path in Retired)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)!=null){if(!AssetDatabase.DeleteAsset(path))throw new Exception("Could not remove "+path);removed++;}
        AssetDatabase.SaveAssets();Debug.Log("UNUSED_SCENES_CLEANED: "+removed+" old samples removed; current world maps retained.");
    }
    public static void Verify()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        Apply();if(Retired.Any(p=>File.Exists(p)||File.Exists(p+".meta"))||EditorBuildSettings.scenes.Length!=Current.Length)throw new Exception("Scene cleanup incomplete.");
        WorldConnectionValidator.Validate();WorldFrameworkCheck.Run();
    }
}
