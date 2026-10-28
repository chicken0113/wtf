using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
[InitializeOnLoad]
public static class SingleWorldPlayCheck
{
    const string Key="SingleWorldPlayCheck";
    static int step;static double deadline;
    static string Target=>SessionState.GetString(Key+"Scene",SingleWorldSetup.World);
    static SingleWorldPlayCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+80;EditorApplication.update+=Tick;}}
    static void Check(bool result,string text){if(!result)throw new Exception(text);}
    public static void Run()=>RunWorld(SingleWorldSetup.World);
    public static void RunGenerated()=>RunWorld(GameMenu.FirstScene);
    static void RunWorld(string target)
    {
        Check(Application.isBatchMode&&!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))&&Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project and saves required");
        if(target==SingleWorldSetup.World)Check(SingleWorldSetup.Apply(),"Setup blocked");SessionState.SetString(Key+"Scene",target);MapSaveAuthoring.RefreshCatalog();EditorSceneManager.OpenScene(target);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Single-world test timed out: "+step);
            if(!EditorApplication.isPlaying||GameSession.Busy)return;
            var f=GameSession.CurrentFarm;if(f==null||!f.IsInitialized||Time.timeSinceLevelLoad<.2f)return;f.enabled=false;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            Check(f.gameObject.scene.path==Target,"Unexpected map transition");
            if(f.Presentation3D!=null)
            {
                var mesh=f.GetComponentsInChildren<ModelView3D>().FirstOrDefault(v=>v.name=="Farmer");Check(mesh!=null&&mesh.Model!=null,"Player has no real 3D model");mesh.Sync();Check(mesh.GetComponent<SpriteRenderer>().forceRenderingOff,"Player sprite is still visible");
                var point=f.PlayerPosition+new Vector2(1,2);Check(Vector2.Distance(WorldProjection.GroundPoint(Camera.main,Camera.main.WorldToScreenPoint(point)),point)<.002f,"3D play projection mismatch");
            }
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");
            if(step==0)
            {
                Check(f.PlayerPosition==f.SpawnPosition&&f.Combat.UsesRegions,"New game did not start in regional world");
                Check(f.InventoryCount==4,"Starting tools/seeds missing");
                var start=f.PlayerPosition;f.MovePlayer(start+Vector2.right*.5f);Check(Vector2.Distance(f.PlayerPosition,start+Vector2.right*.5f)<.01f,"Planar movement failed");f.UpdateAim(f.PlayerPosition+Vector2.right,false);Check(f.AimDirection.x>.99f,"Mouse aim direction failed");
                if(f.Presentation3D!=null){var model=f.GetComponentsInChildren<ModelView3D>().First(v=>v.name=="Farmer");model.Sync();Check(Vector2.Distance(model.Model.transform.position,f.PlayerPosition)<.01f,"Mesh did not follow player movement");}f.TeleportPlayer(start);
                var npc=f.GetComponentsInChildren<ShopNpc>().Single(n=>n.npcId==SingleWorldSetup.Trader);
                Check(!npc.IsVisiting,"Day merchant appeared");
                InventoryCheckAccess.Clear(f);f.TeleportPlayer(f.GetComponentsInChildren<FarmItemPlacement>().First(p=>p.item==Resources.Load<PlantDefinition>("Equipment/Sword")).transform.position);Check(f.DetachAt(f.PlayerPosition)&&f.SelectedItem==Resources.Load<PlantDefinition>("Equipment/Sword"),"Starting sword cannot be picked up");
                Check(f.Combat.Attack(f.SelectedItem,f.PlayerPosition+Vector2.right),"Weapon use failed");
                InventoryCheckAccess.Clear(f);var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");var cell=Vector2Int.RoundToInt(f.SpawnPosition);f.TeleportPlayer(cell);f.AddItem(seed);
                Check(f.Till(cell)&&f.PlantSelected(cell),"Camp planting failed");Check(f.Water(cell,.1f)>0,"Watering failed");f.Tick(seed.growthSeconds+.1f);Check(f.IsMature(cell)&&f.Collect(cell)&&f.InventoryCount>0,"Harvest failed");
                f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+1);f.TeleportPlayer(npc.transform.position+Vector3.down);
                Check(npc.IsVisiting&&f.Shop.Open(npc),"Fixed night shop inaccessible");int slot=InventoryCheckAccess.LastOccupied(f);Check(f.Shop.Sell(slot)&&f.Shop.Buy(0),"Harvest sale/seed purchase failed");f.Shop.Close();
                InventoryCheckAccess.Clear(f);f.AddItem(Resources.Load<PlantDefinition>("Equipment/RespawnToken"));Check(f.UseRespawnItem(f.SelectedItem,f.PlayerPosition,false),"Camp checkpoint failed");
                f.AddItem(seed);Check(GameSession.RespawnAfterDeath(f),"Same-world death recovery failed");
                Check(f.InventoryCount==0,"Death did not leave carried items");
                Check(GameSession.Save(f),"World save failed");var saved=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
                Check(saved.currentScene==Target&&saved.maps.Single(m=>m.scene==Target).deathCaches.Count==1,"Death cache/save missing");
                GameSession.ReleaseToTitle();SceneManager.LoadScene(Target);step=1;return;
            }
            Check(f.GetComponentsInChildren<MapExit>().Length==0,"Reload restored old travel");
            var state=f.CaptureMap(catalog,GameSession.WorldSeconds);Check(state.deathCaches.Count==1&&f.Cycle.ShopsOpen,"Continue lost cache/time");
            f.TeleportPlayer(state.deathCaches[0].position);Check(f.InteractWorld(f.PlayerPosition)&&f.InventoryCount>0,"Saved death bag cannot be recovered");
            SessionState.EraseBool(Key);Debug.Log("SINGLE_WORLD_PLAY_CHECK_PASSED");EditorApplication.Exit(0);
        }
        catch(Exception error){SessionState.EraseBool(Key);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
