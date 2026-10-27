using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
[InitializeOnLoad]
public static class ForagingCheck
{
    const string Key="ForagingCheckActive",Forest="Assets/Scenes/OpenWorld/ForestClearing.unity",Other="Assets/Scenes/Tests/TrainingGround.unity";
    static int step;static double deadline,readyAt;static Vector2 stonePoint,cloudPoint;static string stoneId,cloudId;
    static ForagingCheck(){if(Application.isBatchMode && SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project and saves only");
        EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");Check(ForageExampleSetup.Apply(),"Example placement failed");
        string authored=File.ReadAllText(Forest);Check(ForageExampleSetup.Apply() && File.ReadAllText(Forest)==authored,"Examples duplicated");
        var profile=Resources.Load<ForageDefinition>("Foraging/StoneFragments");profile.respawnSeconds=3;EditorUtility.SetDirty(profile);
        foreach(var region in SpawnRegion.ForScene(Forest)){region.maxAlive=0;EditorUtility.SetDirty(region);}AssetDatabase.SaveAssets();
        ProjectSetupCheck.Run();EditorSceneManager.OpenScene(Forest);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static MapSave Map(FarmGame f)=>f.CaptureMap(Resources.Load<SaveItemCatalog>("SaveItemCatalog"),GameSession.WorldSeconds);
    static ForageNodeSave Node(FarmGame f,string id)=>Map(f).forageNodes.Single(n=>n.id==id);
    static int Count(FarmGame f,PlantDefinition item)=>Enumerable.Range(0,f.Capacity).Where(i=>f.InventoryItem(i)==item).Sum(f.InventoryQuantity);
    static void Position(FarmGame f,Vector2 point){f.TeleportPlayer(point);Camera.main.transform.position=(Vector3)point+Vector3.back*10;}
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Foraging check timeout: "+step);
            if(!EditorApplication.isPlaying)return;var loaded=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(loaded!=null && loaded.IsInitialized)loaded.enabled=false;
            if(GameSession.Busy || Time.timeSinceLevelLoad<.1f)return;
            var f=GameSession.CurrentFarm;if(f==null || !f.IsInitialized)return;Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            var stone=Resources.Load<ForageDefinition>("Foraging/StoneFragments").item;var cloud=Resources.Load<ForageDefinition>("Foraging/CloudFind").item;
            if(step==0)
            {
                var nodes=f.GetComponentsInChildren<FarmItemPlacement>().Where(p=>p.forage!=null).ToArray();Check(nodes.Length==2,"Example nodes missing");
                var s=nodes.Single(n=>n.forage.item==stone);var c=nodes.Single(n=>n.forage.item==cloud);stonePoint=s.transform.position;cloudPoint=c.transform.position;stoneId=MapObjectId.Key(s);cloudId=MapObjectId.Key(c);
                InventoryCheckAccess.Clear(f);var sword=Resources.Load<PlantDefinition>("Equipment/Sword");for(int i=0;i<f.Capacity-1;i++)f.AddItem(sword);
                Position(f,stonePoint);Check(f.DetachAt(stonePoint),"Partial collection failed");Check(Node(f,stoneId).remaining==2 && Node(f,stoneId).readyAt==0 && Count(f,stone)==1,"Partial quantity lost or timer started too soon");
                Check(!f.DetachAt(stonePoint) && Node(f,stoneId).remaining==2,"Full bag consumed node");
                InventoryCheckAccess.Clear(f);Check(f.DetachAt(stonePoint),"Remaining collection failed");readyAt=Node(f,stoneId).readyAt;Check(Node(f,stoneId).remaining==0 && readyAt>GameSession.WorldSeconds && Count(f,stone)==2,"Respawn timer did not start on depletion");
                Position(f,cloudPoint);Directory.CreateDirectory(GameSession.SavePath+".tmp");Check(!f.DetachAt(cloudPoint),"Failed save accepted gathering");Check(Node(f,cloudId).remaining==1 && Count(f,cloud)==0,"Save failure lost/duplicated one-shot item");Directory.Delete(GameSession.SavePath+".tmp");Check(GameSession.Save(f),"Save retry failed");
                Check(f.DetachAt(cloudPoint) && Node(f,cloudId).remaining==0 && Node(f,cloudId).readyAt==0,"One-shot gathering failed");
                f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+1);
                Check(GameSession.Travel(f,Other,null,new Vector2(9,-5)),"Departure failed");step=1;return;
            }
            if(step==1)
            { GameSession.AdvanceTime(1);Check(GameSession.Travel(f,Forest,null,stonePoint),"Early return failed");step=2;return; }
            if(step==2)
            {
                Check(Node(f,stoneId).remaining==0 && Math.Abs(Node(f,stoneId).readyAt-readyAt)<.00001 && Node(f,cloudId).remaining==0,"Saved waiting/one-shot state reset");
                Check(GameSession.Travel(f,Other,null,new Vector2(9,-5)),"Second departure failed");step=3;return;
            }
            if(step==3)
            { GameSession.AdvanceTime(5);Check(GameSession.Travel(f,Forest,null,stonePoint),"Mature return failed");step=4;return; }
            if(step==4)
            {
                Check(!f.Cycle.IsDaytime && Node(f,stoneId).remaining==3 && Node(f,cloudId).remaining==0,"Remote/night regrowth or one-shot persistence failed");
                Position(f,stonePoint);int pieces=Map(f).pieces.Count;int slot=Enumerable.Range(0,f.Capacity).First(i=>f.InventoryItem(i)==cloud);Check(f.DropInventoryItems(slot,1),"Ordinary drop setup failed");
                GameSession.AdvanceTime(100);f.Tick(100);Check(Map(f).pieces.Count==pieces+1 && Node(f,stoneId).remaining==3 && Node(f,cloudId).remaining==0,"Loose drop regrew/disappeared or node accumulated batches");
                Check(GameSession.Save(f),"Final forage save failed");
                var invalid=ScriptableObject.CreateInstance<ForageDefinition>();invalid.quantity=0;var report=new SetupValidationReport();SetupDataValidator.CheckForage(invalid,null,report,new System.Collections.Generic.HashSet<PlantDefinition>(),false);Check(report.HasErrors,"Invalid forage accepted");UnityEngine.Object.Destroy(invalid);
                Finish(0,"FORAGING_SUCCESS: stable example authoring, partial/full inventory, depletion timer, failed-save rollback/retry, one-shot persistence, real map round-trips, remote night regrowth and ordinary drop separation.");
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1,"FORAGING_FAILED step "+step+": "+e.Message);}
    }
    static void Finish(int code,string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(code);}
}
