using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
public static class WorldGenerationCheck
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project required");
        var settings=ScriptableObject.CreateInstance<WorldGenerationSettings>();settings.hideFlags=HideFlags.DontUnloadUnusedAsset;
        for(int seed=0;seed<24;seed++)
        {
            settings.seed=seed;settings.width=seed%3==0?40:64;settings.height=seed%3==0?40:56;
            var a=new WorldGenerationPlan(settings);var b=new WorldGenerationPlan(settings);
            Check(a.Cells.SequenceEqual(b.Cells)&&a.Reserved.SequenceEqual(b.Reserved)&&a.Boss==b.Boss&&a.Camps.SequenceEqual(b.Camps),"Seed is not deterministic");
            var land=a.Reachable(a.Start);Check(a.Camps.Append(a.Boss).All(p=>land.Contains(a.Index(p))),"Landmark is isolated: "+seed);
            Check(a.Cells.Count(t=>t==WorldGenerationPlan.Terrain.Water)>0&&a.Cells.Any(t=>t==WorldGenerationPlan.Terrain.Forest),"Terrain types missing");
            Check(Vector2.Distance(a.Start,a.Boss)>12,"Boss too close to start");
        }
        settings.seed=281026;settings.width=64;settings.height=56;
        string original=File.ReadAllText(SingleWorldSetup.World);var oldScene=EditorSceneManager.OpenScene(SingleWorldSetup.World);var farm=ManagementEditorUI.Components<FarmGame>(oldScene).Single();
        var marker=new GameObject("Unsaved generator preservation check");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker,oldScene);EditorSceneManager.MarkSceneDirty(oldScene);
        string generated=WorldSceneGenerator.Generate(settings,WorldSceneGenerator.DefaultWorld);
        Check(oldScene.isLoaded&&oldScene.isDirty&&marker!=null&&File.ReadAllText(SingleWorldSetup.World)==original,"Generator altered template or unsaved work");
        var next=EditorSceneManager.OpenScene(generated,OpenSceneMode.Additive);var f=ManagementEditorUI.Components<FarmGame>(next).Single();
        WorldSceneGenerator.Verify(f,new WorldGenerationPlan(settings));Check(SpawnRegion.ForScene(generated).Length==1,"Generated region missing");
        var collision=new MapCollision(f);var water=f.GetComponentsInChildren<MapObstacle>().First(o=>o.name=="Water row");Check(!collision.CanStand(water.transform.position,.25f)&&!water.blocksProjectiles,"Water collision mismatch");
        EditorSceneManager.CloseScene(next,true);UnityEngine.Object.DestroyImmediate(marker);
        Debug.Log("WORLD_GENERATION_CHECK_PASSED: 24 seeds; deterministic terrain, connected landmarks, actual paths, water collision, dirty template preserved");
    }
}
