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
    static SingleWorldPlayCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+80;EditorApplication.update+=Tick;}}
    static void Check(bool result,string text){if(!result)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode&&!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))&&Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project and saves required");
        Check(SingleWorldSetup.Apply(),"Setup blocked");MapSaveAuthoring.RefreshCatalog();EditorSceneManager.OpenScene(GameMenu.FirstScene);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Single-world test timed out: "+step);
            if(!EditorApplication.isPlaying||GameSession.Busy)return;
            var f=GameSession.CurrentFarm;if(f==null||!f.IsInitialized||Time.timeSinceLevelLoad<.2f)return;f.enabled=false;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            Check(f.gameObject.scene.path==SingleWorldSetup.World,"Unexpected map transition");
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");
            if(step==0)
            {
                Check(f.PlayerPosition==f.SpawnPosition&&f.Combat.UsesRegions,"New game did not start in regional world");
                Check(f.InventoryCount==4,"Starting tools/seeds missing");
                var npc=f.GetComponentsInChildren<ShopNpc>().Single(n=>n.npcId==SingleWorldSetup.Trader);
                Check(!npc.IsVisiting,"Day merchant appeared");
                InventoryCheckAccess.Clear(f);f.TeleportPlayer(new Vector2(-13,-11));Check(f.DetachAt(f.PlayerPosition)&&f.SelectedItem==Resources.Load<PlantDefinition>("Equipment/Sword"),"Starting sword cannot be picked up");
                Check(f.Combat.Attack(f.SelectedItem,f.PlayerPosition+Vector2.right),"Weapon use failed");
                InventoryCheckAccess.Clear(f);var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");var cell=new Vector2Int(-12,-10);f.TeleportPlayer(cell);f.AddItem(seed);
                Check(f.Till(cell)&&f.PlantSelected(cell),"Camp planting failed");Check(f.Water(cell,.1f)>0,"Watering failed");f.Tick(seed.growthSeconds+.1f);Check(f.IsMature(cell)&&f.Collect(cell)&&f.InventoryCount>0,"Harvest failed");
                f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+1);f.TeleportPlayer(npc.transform.position+Vector3.down);
                Check(npc.IsVisiting&&f.Shop.Open(npc),"Fixed night shop inaccessible");int slot=InventoryCheckAccess.LastOccupied(f);Check(f.Shop.Sell(slot)&&f.Shop.Buy(0),"Harvest sale/seed purchase failed");f.Shop.Close();
                InventoryCheckAccess.Clear(f);f.AddItem(Resources.Load<PlantDefinition>("Equipment/RespawnToken"));Check(f.UseRespawnItem(f.SelectedItem,f.PlayerPosition,false),"Camp checkpoint failed");
                f.AddItem(seed);Check(GameSession.RespawnAfterDeath(f),"Same-world death recovery failed");
                Check(f.InventoryCount==0,"Death did not leave carried items");
                Check(GameSession.Save(f),"World save failed");var saved=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
                Check(saved.currentScene==SingleWorldSetup.World&&saved.maps.Single(m=>m.scene==SingleWorldSetup.World).deathCaches.Count==1,"Death cache/save missing");
                GameSession.ReleaseToTitle();SceneManager.LoadScene(GameMenu.FirstScene);step=1;return;
            }
            Check(f.GetComponentsInChildren<MapExit>().Length==0,"Reload restored old travel");
            var state=f.CaptureMap(catalog,GameSession.WorldSeconds);Check(state.deathCaches.Count==1&&f.Cycle.ShopsOpen,"Continue lost cache/time");
            f.TeleportPlayer(state.deathCaches[0].position);Check(f.InteractWorld(f.PlayerPosition)&&f.InventoryCount>0,"Saved death bag cannot be recovered");
            SessionState.EraseBool(Key);Debug.Log("SINGLE_WORLD_PLAY_CHECK_PASSED");EditorApplication.Exit(0);
        }
        catch(Exception error){SessionState.EraseBool(Key);Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
