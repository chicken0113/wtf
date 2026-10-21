using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class TownSketchCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated validation.");
        TownSketchLayout.Apply(); WorldFrameworkSetup.Connect(); WorldConnectionValidator.Validate();
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town); var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        var collision=new MapCollision(farm); var map=farm.Layout;
        var targets=map.GetComponentsInChildren<MapArrivalPoint>().Select(point=>(Vector2)point.transform.position)
            .Concat(map.GetComponentsInChildren<MapExit>().Select(exit=>(Vector2)exit.transform.position))
            .Concat(map.GetComponentsInChildren<ShopNpc>().Select(npc=>(Vector2)npc.transform.position+Vector2.down)).ToArray();
        foreach(var target in targets)
        {
            var path=collision.FindPath(farm.SpawnPosition,target,farm.PlayerRadius);
            if(!collision.CanStand(target,farm.PlayerRadius) || (Vector2.Distance(farm.SpawnPosition,target)>.3f && (path.Count==0 || Vector2.Distance(path.Last(),target)>.3f))) throw new Exception("Town route blocked to "+target);
        }
        if(map.transform.Find("Buildings/TownGuild")==null || map.transform.Find("Buildings/TownSmith").position.x<=0 || map.transform.Find("Buildings/TownGeneralStore").position.y>=0 || map.transform.Find("Buildings/TownHouse").position.x>=0) throw new Exception("Sketch landmarks missing or misplaced.");
        if(!map.GetComponentsInChildren<SpriteRenderer>().Any(renderer=>renderer.name=="Sea")) throw new Exception("Coast missing.");
        Debug.Log("TOWN_SKETCH_PATHS_SUCCESS: "+targets.Length+" arrival/exit/NPC routes reachable; landmark positions and coast checked.");
        WorldFrameworkCheck.Run();
    }
}
