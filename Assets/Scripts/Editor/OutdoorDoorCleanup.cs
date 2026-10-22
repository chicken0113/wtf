using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class OutdoorDoorCleanup
{
    const string Request="Library/OutdoorDoorCleanup.request",Result="Library/OutdoorDoorCleanup.result";
    static readonly string[] Outdoors={CombatTestMapSetup.FarmScene,WorldFrameworkSetup.Town,CombatTestMapSetup.CombatScene};
    public static bool IsOutdoor(string path)=>Outdoors.Contains(path)||new[]{TriangleWorldSetup.Passage,TriangleWorldSetup.Field,TriangleWorldSetup.Forest,TriangleWorldSetup.Training}.Contains(path);
    static OutdoorDoorCleanup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static bool Dirty()=>Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(s=>s.isDirty&&IsOutdoor(s.path));
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText(Result,"WAITING_FOR_PLAY_STOP");return;}
        if(Dirty()){File.WriteAllText(Result,"WAITING_FOR_SCENE_SAVE");return;}
        File.Delete(Request);try{Apply();File.WriteAllText(Result,"SUCCESS");}catch(Exception error){Debug.LogException(error);File.WriteAllText(Result,"ERROR: "+error);}
    }
    [MenuItem("Tools/Everything Grows/World/Remove outdoor E doors")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Dirty())throw new Exception("Stop Play and save modified outdoor scenes before removing doors.");
        var active=SceneManager.GetActiveScene();var opened=new List<Scene>();var scenes=new List<Scene>();int removed=0;
        try
        {
            foreach(string path in Outdoors)
            {
                var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid()||!scene.isLoaded){scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened.Add(scene);}scenes.Add(scene);
            }
            foreach(var scene in scenes)
            {
                var exits=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MapExit>(true)).ToArray();
                foreach(var door in exits.Where(e=>e.requireInteract&&IsOutdoor(e.destinationScene)))
                {
                    if(door.GetComponent<FarmGame>()!=null)throw new Exception("MapExit must have its own object: "+door.name);
                    if(!exits.Any(e=>e.isActiveAndEnabled&&!e.requireInteract&&e.destinationScene==door.destinationScene))throw new Exception("Automatic route missing in "+scene.path+" to "+door.destinationScene);
                }
            }
            foreach(var scene in scenes)
            {
                var doors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MapExit>(true)).Where(e=>e.requireInteract&&IsOutdoor(e.destinationScene)).ToArray();
                foreach(var door in doors)
                {
                    foreach(var arrival in door.GetComponentsInChildren<MapArrivalPoint>(true))Undo.SetTransformParent(arrival.transform,door.transform.parent,"Preserve travel arrival");
                    Undo.DestroyObjectImmediate(door.gameObject);removed++;
                }
                if(doors.Length>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            }
            Debug.Log("OUTDOOR_E_DOORS_REMOVED: "+removed+"; automatic routes and building interior doors preserved.");
        }
        finally{for(int i=opened.Count-1;i>=0;i--)EditorSceneManager.CloseScene(opened[i],true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);}
    }
    public static void Verify()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        Apply();WorldConnectionValidator.Validate();EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);PelicanTownLayout.Capture(UnityEngine.Object.FindFirstObjectByType<FarmGame>(),"AutomaticTravelTown.png");WorldFrameworkCheck.Run();
    }
}
