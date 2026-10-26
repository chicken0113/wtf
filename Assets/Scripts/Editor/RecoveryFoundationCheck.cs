using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
[InitializeOnLoad]
public static class RecoveryFoundationCheck
{
    const string Key="RecoveryFoundationActive";
    static int step;static double deadline;static string cacheScene;static Vector2 cachePosition;
    static RecoveryFoundationCheck(){if(Application.isBatchMode && SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project/save required");
        MapSaveAuthoring.RefreshCatalog();EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");ProjectSetupCheck.Run();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static GameSaveData Saved()=>JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Recovery check timeout: "+step);
            if(!EditorApplication.isPlaying || GameSession.Busy || Time.timeSinceLevelLoad<.25f)return;
            var f=GameSession.CurrentFarm;if(f==null || !f.IsInitialized)return;f.enabled=false;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");
            var token=Resources.Load<PlantDefinition>("Equipment/RespawnToken");
            if(step==0)
            {
                InventoryCheckAccess.Clear(f);
                f.TeleportPlayer(new Vector2(2,0));f.AddItem(token);Check(f.UseSelectedItem(f.PlayerPosition,true),"Token use failed");
                Check(f.InventoryCount==0 && Saved().respawn.position==f.PlayerPosition,"Token not consumed/saved atomically");
                f.AddItem(Resources.Load<PlantDefinition>("Equipment/CampingBed"));
                f.TeleportPlayer(new Vector2(1,-2));Check(f.UseRespawnItem(f.SelectedItem,new Vector2(3,-2),true),"Camping bed placement failed");
                Check(f.InventoryCount==0 && Saved().maps.Single(m=>m.scene==f.gameObject.scene.path).respawnAnchors.Count==1,"Bed persistence failed");
                var bed=f.GetComponentsInChildren<RespawnAnchor>().Single();Check(f.InteractWorld(bed.transform.position),"Bed right-click failed");
                File.Delete(GameSession.SavePath+".bak");Directory.CreateDirectory(GameSession.SavePath+".tmp");
                f.AddItem(token);var before=f.CapturePlayer(catalog);Check(!f.UseRespawnItem(token,f.PlayerPosition,false),"Save failure accepted token");
                Check(f.InventoryCount==before.inventory.Count,"Save failure lost token");Directory.Delete(GameSession.SavePath+".tmp");Check(GameSession.Save(f),"Retry failed");
                f.AddItem(Resources.Load<PlantDefinition>("Equipment/Sword"));
                cachePosition=f.PlayerPosition;cacheScene=f.gameObject.scene.path;
                Check(GameSession.RespawnAfterDeath(f),"Same-scene death failed");
                Check(f.InventoryCount==0 && Saved().maps.Single(m=>m.scene==cacheScene).deathCaches.Count==1,"First cache lost");
                f.AddItem(token);Check(GameSession.RespawnAfterDeath(f),"Repeat death failed");
                Check(Saved().maps.Single(m=>m.scene==cacheScene).deathCaches.Count==2,"Previous cache overwritten");
                // Consume nothing on recovery if full. Partial recovery retains remaining quantities.
                f.TeleportPlayer(cachePosition);for(int i=0;i<f.Capacity;i++)f.AddItem(Resources.Load<PlantDefinition>("Equipment/Sword"));
                Check(f.InteractWorld(cachePosition),"Full-bag cache not handled");Check(Saved().maps.Single(m=>m.scene==cacheScene).deathCaches.Count==2,"Full bag deleted cache");
                f.RemoveInventoryItem(0);Check(f.InteractWorld(cachePosition),"Partial recovery failed");
                Check(Saved().maps.Single(m=>m.scene==cacheScene).deathCaches.Single(c=>c.position==cachePosition).items.Count==1,"Partial recovery discarded leftovers");
                InventoryCheckAccess.Clear(f);Check(f.InteractWorld(cachePosition),"Recovery failed");Check(f.InventoryCount>0,"Recovered nothing");
                Check(GameSession.Travel(f,TowerDungeonSetup.Battle,null,new Vector2(8,-5)),"Cross-map setup failed");step=1;return;
            }
            if(step==1)
            {
                f.AddItem(token);f.Combat.TickCombat(1.1f);f.Combat.DamagePlayer(100000);
                Check(GameSession.Busy && f.Combat.Health==f.Combat.HalfHeartHealth,"Lethal damage did not trigger spawn recovery");step=2;return;
            }
            if(step==2)
            {
                Check(f.gameObject.scene.path==cacheScene && f.InventoryCount==0,"Wrong cross-map respawn");
                var state=Saved();Check(state.maps.Single(m=>m.scene==TowerDungeonSetup.Battle).deathCaches.Count==1,"Cross-map cache lost");
                Check(state.maps.Single(m=>m.scene==cacheScene).deathCaches.Count>=1,"Older map cache lost");
                // Reload persisted scene and verify bed/cache restoration.
                var anchor=f.GetComponentsInChildren<RespawnAnchor>().Single();UnityEngine.Object.DestroyImmediate(anchor.gameObject);
                Check(GameSession.RespawnAfterDeath(f),"Missing anchor fallback failed");Check(Saved().respawn==null || !Saved().respawn.IsSet,"Missing bed remained registered");
                Check(GameSession.Travel(f,TowerDungeonSetup.Battle,null,new Vector2(8,-5)),"Return to cache failed");step=3;return;
            }
            if(step==3)
            {
                var map=Saved().maps.Single(m=>m.scene==TowerDungeonSetup.Battle);var position=map.deathCaches[0].position;
                f.TeleportPlayer(position);Check(f.InteractWorld(position) && f.InventoryCount>0,"Restored cache cannot be recovered");
                Check(Saved().maps.Single(m=>m.scene==TowerDungeonSetup.Battle).deathCaches.Count==0,"Recovered cache persisted duplicate");
                Finish(0,"RECOVERY_FOUNDATION_SUCCESS: consumable/bed spawn, failed-save rollback, repeated deaths, full-bag preservation, cross-map respawn and persisted recovery, removed bed fallback.");
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1,"RECOVERY_FOUNDATION_FAILED step "+step+": "+e.Message);}
    }
    static void Finish(int code,string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(code);}
}
