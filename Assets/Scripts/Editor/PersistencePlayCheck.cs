using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class PersistencePlayCheck
{
    const string Key="WtfPersistenceCheck", Origin="Assets/Scenes/FirstGarden.unity", Destination="Assets/Scenes/PersistenceTestMap.unity";
    static int step,previousFarmId;
    static double deadline;
    static string mode;
    static PieceSave[] expectedDrops;
    static PersistencePlayCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        { deadline=EditorApplication.timeSinceStartup+90; mode=SessionState.GetString(Key+"Mode",""); EditorApplication.update+=Tick; }
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use isolated project and test save directory.");
        if(!Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)) throw new Exception("Destructive fixture creation requires a temp project.");
        MapSaveAuthoring.GenerateAssets();
        EditorSceneManager.OpenScene(Origin);
        foreach(var marker in UnityEngine.Object.FindObjectsByType<MapArrivalPoint>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(marker.gameObject);
        foreach(var marker in UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(marker.gameObject);
        UnityEngine.Object.FindFirstObjectByType<FarmGame>().Layout.combatSchedule=FarmMapLayout.CombatSchedule.NightOnly;
        MapSaveAuthoring.PrepareCurrentMap();
        var arrival=new GameObject("Farm return").AddComponent<MapArrivalPoint>(); arrival.arrivalId="farm_entry"; arrival.transform.position=new Vector2(0,-4);
        var exit=new GameObject("Farm exit").AddComponent<MapExit>(); exit.destinationScene=Destination; exit.arrivalId="test_entry"; exit.transform.position=new Vector2(4,-4);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Destination)!=null) AssetDatabase.DeleteAsset(Destination);
        if(!AssetDatabase.CopyAsset(Origin,Destination)) throw new Exception("Test destination copy failed");
        EditorSceneManager.OpenScene(Destination);
        UnityEngine.Object.FindFirstObjectByType<MapArrivalPoint>().arrivalId="test_entry";
        var back=UnityEngine.Object.FindFirstObjectByType<MapExit>(); back.destinationScene=Origin; back.arrivalId="farm_entry";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Origin,true),new EditorBuildSettingsScene(Destination,true)};
        EditorSceneManager.OpenScene(Origin); Begin("roundtrip");
    }
    public static void Resume()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use test save directory.");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Origin,true),new EditorBuildSettingsScene(Destination,true)};
        EditorSceneManager.OpenScene(Origin); Begin("resume");
    }
    public static void Backup()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use test save directory.");
        File.WriteAllText(GameSession.SavePath,"{ corrupt save");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Origin,true),new EditorBuildSettingsScene(Destination,true)};
        EditorSceneManager.OpenScene(Origin); Begin("backup");
    }
    static void Begin(string value) { SessionState.SetBool(Key,true); SessionState.SetString(Key+"Mode",value); EditorApplication.isPlaying=true; }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Save/travel check timeout at step "+step+" "+GameSession.LastError);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.2f || GameSession.Busy) return;
            var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); if(farm==null || farm.Combat==null) return;
            if(mode=="resume" || mode=="backup")
            {
                Require(farm.gameObject.scene.path==Destination,"Did not resume last saved scene");
                Require(farm.Gold==251 && farm.Combat.HeartCount==4 && farm.InventoryCount==7 && farm.Capacity==8,"Player state was not restored across process restart");
                Require(farm.InventoryItem(1).isSeed && farm.InventoryQuantity(1)==10,"Inventory quantities or starter single grant incorrect");
                Require(farm.WorldPieceCount==1,"Destination drop lost on restart");
                Finish(true,mode=="resume"?"PERSISTENCE_RESTART_SUCCESS":"PERSISTENCE_BACKUP_SUCCESS"); return;
            }
            if(step==0)
            {
                Require(farm.InventoryCount==4,"Starter kit missing");
                Require(!GameSession.Travel(farm,"Assets/Scenes/NoSuchScene.unity","entry") && !GameSession.Busy,"Invalid scene was accepted");
                using(var locked=new FileStream(GameSession.SavePath,FileMode.Open,FileAccess.Read,FileShare.None))
                    Require(!GameSession.Travel(farm,Destination,"test_entry") && !GameSession.Busy,"Failed save still changed maps");
                var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");
                Require(farm.AddItem(seed) && farm.PlantSelected(new Vector2Int(1,-2)),"Test crop planting");
                farm.Water(new Vector2(1,-2),.1f); farm.Tick(seed.growthSeconds*.25f);
                Require(farm.Combat.HarvestHeart(0),"Test heart harvest");
                farm.TeleportPlayer(new Vector2(22,14)); farm.Combat.DamagePlayer(farm.Combat.HalfHeartHealth);
                var building=farm.GetComponentInChildren<FarmBuilding>(); var renderer=building.GetComponentInChildren<SpriteRenderer>();
                var point=new Vector2(renderer.bounds.center.x,renderer.bounds.min.y+.1f); farm.TeleportPlayer(point-Vector2.up*.6f);
                Require(farm.HarvestBuildingAt(point),"Authored house harvest");
                farm.RemoveInventoryItem(0); farm.AddItem(Resources.Load<PlantDefinition>("Buildings/UpgradedHouse"));
                farm.TeleportPlayer(new Vector2(-5,-5)); Require(farm.PlaceHouse(new Vector2(-5,-3)),"Test house placement");
                var slot=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/InventorySlot.asset");
                farm.AddItem(slot); Require(farm.PlantSelected(new Vector2Int(-1,-3)),"Storage crop fixture failed");
                farm.Water(new Vector2(-1,-3),.1f); farm.Tick(slot.growthSeconds);
                var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
                while(farm.InventoryCount<7) Require(farm.AddItem(hoe),"Expanded inventory fixture failed");
                farm.DropItem(Resources.Load<PlantDefinition>("Crops/Potato"),new Vector2(3,-4));
                farm.Shop.RestoreGold(237);
                expectedDrops=farm.CaptureMap(Resources.Load<SaveItemCatalog>("SaveItemCatalog"),GameSession.WorldSeconds).pieces.ToArray();
                step=1; Require(GameSession.Travel(farm,Destination,"missing_arrival"),"Missing-arrival test could not start"); return;
            }
            if(step==1)
            {
                Require(farm.gameObject.scene.path==Origin && !string.IsNullOrEmpty(GameSession.LastError),"Missing arrival did not roll back");
                Require(farm.InventoryCount==7 && farm.Capacity==8 && farm.Gold==237 && farm.WorldPieceCount==2,"Rollback lost state");
                step=2; Require(GameSession.Travel(farm,Destination,"test_entry"),"Valid travel failed"); return;
            }
            if(step==2)
            {
                Require(farm.gameObject.scene.path==Destination && Vector2.Distance(farm.PlayerPosition,new Vector2(0,-4))<.2f,"Destination arrival incorrect");
                Require(farm.InventoryCount==7 && farm.Capacity==8 && farm.Combat.HeartCount==4 && farm.Combat.Health==70 && farm.Gold==237,"Carried player state lost");
                Require(farm.WorldPieceCount==0 && !farm.IsMature(new Vector2Int(1,-2)),"Maps share world state");
                GameSession.AdvanceTime(3); farm.Cycle.Tick(3); farm.Shop.RestoreGold(251);
                farm.DropItem(Resources.Load<PlantDefinition>("Crops/Tomato"),new Vector2(3,-4));
                step=3; Require(GameSession.Travel(farm,Origin,"farm_entry"),"Return travel failed"); return;
            }
            if(step==3)
            {
                Require(farm.gameObject.scene.path==Origin && farm.Gold==251 && farm.InventoryCount==7 && farm.Capacity==8,"Return player state incorrect");
                Require(farm.WorldPieceCount==2 && farm.GetComponentsInChildren<FarmBuilding>().Count(building=>building.runtimePlaced)==1 && farm.GetComponentsInChildren<FarmBuilding>().All(building=>building.runtimePlaced),"Drops, placed house or removed original house incorrect");
                Require(farm.CropProgress(new Vector2Int(1,-2))>=.39f,"Away-time growth not restored");
                var map=farm.CaptureMap(Resources.Load<SaveItemCatalog>("SaveItemCatalog"),GameSession.WorldSeconds);
                foreach(var piece in expectedDrops) Require(map.pieces.Any(saved=>saved.item==piece.item && Vector2.Distance(saved.position,piece.position)<.001f && Vector2.Distance(saved.size,piece.size)<.001f),"Dropped item position or size changed");
                Require(map.removedObjects.Count==1 && map.buildings.Count==1 && map.pieces.Count==2,"Map snapshot missing state");
                Require(GameSession.SaveAfterHouseTime(farm),"House-time save hook failed");
                farm.Shop.RestoreGold(999); previousFarmId=farm.GetInstanceID(); step=4; SceneManager.LoadSceneAsync(Origin,LoadSceneMode.Single); return;
            }
            if(step==4)
            {
                if(farm.GetInstanceID()==previousFarmId) return;
                Require(farm.Gold==251 && farm.WorldPieceCount==2,"Reload did not restore last committed state");
                step=5; Require(GameSession.Travel(farm,Destination,"test_entry"),"Final destination travel failed"); return;
            }
            if(step==5)
            {
                Require(farm.gameObject.scene.path==Destination && farm.Gold==251 && farm.WorldPieceCount==1,"Final destination restore failed");
                Require(GameSession.Save(farm),"Final autosave failed");
                Require(File.Exists(GameSession.SavePath) && File.Exists(GameSession.SavePath+".bak"),"Atomic save backup missing");
                Finish(true,"PERSISTENCE_ROUNDTRIP_SUCCESS: portal routing, rollback, inventory, hearts, gold, plants, away growth, dropped items, buildings, removed originals, reload and backup.");
            }
        }
        catch(Exception problem) { Finish(false,problem.ToString()); }
    }
    static void Finish(bool ok,string message)
    {
        SessionState.SetBool(Key,false); EditorApplication.update-=Tick;
        if(ok) Debug.Log(message); else Debug.LogError(message); EditorApplication.Exit(ok?0:1);
    }
}
