using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public static class WorldConnectionValidator
{
    public static void Verify()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated validation.");
        WorldFrameworkSetup.Connect(); Validate(); WorldFrameworkCheck.Run();
    }
    [MenuItem("Tools/Everything Grows/World/Validate world connections")]
    public static void Validate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before validating scene assets.");
        string[] paths=TriangleWorldSetup.Installed?TriangleWorldSetup.Paths:new[]{CombatTestMapSetup.FarmScene,WorldFrameworkSetup.Town,WorldFrameworkSetup.Store,CombatTestMapSetup.CombatScene};
        var opened=new List<Scene>(); var maps=new Dictionary<string,FarmGame>(); var active=SceneManager.GetActiveScene(); int errors=0,checkedExits=0;
        void Error(string text,UnityEngine.Object context=null) { errors++; Debug.LogError("World connection: "+text,context); }
        try
        {
            foreach(string path in paths)
            {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null) { Error("Missing scene: "+path); continue; }
                if(!EditorBuildSettings.scenes.Any(entry=>entry.enabled && entry.path==path)) Error("Scene not enabled in Build Settings: "+path);
                var scene=SceneManager.GetSceneByPath(path);
                if(!scene.IsValid() || !scene.isLoaded) { scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive); opened.Add(scene); }
                var farms=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmGame>()).ToArray();
                if(farms.Length!=1) { Error("Expected one active FarmGame in "+path); continue; }
                var farm=farms[0]; maps[path]=farm;
                var points=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MapArrivalPoint>()).Where(point=>point.isActiveAndEnabled).ToArray();
                foreach(var group in points.GroupBy(point=>point.arrivalId)) if(string.IsNullOrWhiteSpace(group.Key) || group.Count()>1) Error("Empty or duplicate arrival ID in "+path+": "+group.Key,group.First());
                var collision=new MapCollision(farm);
                foreach(var point in points) if(!collision.CanStand(point.transform.position,farm.PlayerRadius)) Error("Arrival inside obstacle or outside map: "+point.arrivalId,point);
                if(!collision.CanStand(farm.SpawnPosition,farm.PlayerRadius)) Error("Player spawn is blocked in "+path,farm);
                var manager=farm.GetComponent<NpcManager>();
                if(manager!=null)
                {
                    foreach(var group in manager.npcs.Where(profile=>profile!=null).GroupBy(profile=>profile.id)) if(string.IsNullOrWhiteSpace(group.Key) || group.Count()>1) Error("Empty or duplicate NPC profile ID in "+path,manager);
                    foreach(var npc in farm.GetComponentsInChildren<ShopNpc>()) if(!manager.npcs.Any(profile=>profile!=null && profile.id==npc.npcId)) Error("NPC has no profile: "+npc.npcId,npc);
                }
            }
            foreach(var pair in maps)
            {
                var farm=pair.Value; var collision=new MapCollision(farm);
                foreach(var exit in farm.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MapExit>()).Where(exit=>exit.isActiveAndEnabled))
                {
                    checkedExits++;
                    if(OutdoorDoorCleanup.IsOutdoor(pair.Key)&&OutdoorDoorCleanup.IsOutdoor(exit.destinationScene)&&exit.requireInteract) Error("Outdoor travel must be automatic: "+exit.name,exit);
                    if(!maps.TryGetValue(exit.destinationScene,out var target)) { Error("Destination outside configured world or missing: "+exit.destinationScene,exit); continue; }
                    var targetPoints=target.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MapArrivalPoint>()).Where(point=>point.isActiveAndEnabled && point.arrivalId==exit.arrivalId).ToArray();
                    if(targetPoints.Length!=1) Error("Destination arrival missing or duplicated: "+exit.arrivalId,exit);
                    bool reachable=false;
                    for(int x=-1;x<=1;x++) for(int y=-1;y<=1;y++)
                    {
                        Vector2 point=exit.transform.TransformPoint(new Vector2(x*exit.areaSize.x*.4f,y*exit.areaSize.y*.4f));
                        reachable|=collision.CanStand(point,farm.PlayerRadius);
                    }
                    if(!reachable) Error("Exit area is blocked: "+exit.name,exit);
                }
            }
        }
        finally { for(int i=opened.Count-1;i>=0;i--) EditorSceneManager.CloseScene(opened[i],true); if(active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        if(errors>0) throw new Exception("World validation found "+errors+" error(s). See Console and select their objects.");
        Debug.Log("WORLD_CONNECTIONS_VALID: "+maps.Count+" maps, "+checkedExits+" exits; arrivals, collisions, build settings and NPC profiles validated. No scene placements changed.");
    }
}
