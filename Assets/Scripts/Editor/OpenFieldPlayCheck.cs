using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class OpenFieldPlayCheck
{
    const string Key="WtfOpenFieldPlayCheck";
    static double deadline;
    static int shopFrames;
    static OpenFieldPlayCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        {
            deadline=EditorApplication.timeSinceStartup+20;
            EditorApplication.update+=Tick;
            // Editor search indexing can throw independently of the running scene.
            // Only gameplay stack traces belong to this runtime check.
            Application.logMessageReceived+=(message,trace,type)=> { if(type==LogType.Exception && trace.Contains("EverythingGrows.")) SessionState.SetString(Key+"Error",message+"\n"+trace); };
        }
    }
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated batch project.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var data=new PlantDefinition[5]; var names=new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"};
        for(int i=0;i<5;i++) data[i]=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+names[i]+".asset");
        new GameObject("Play farm").AddComponent<FarmGame>().Configure(data);
        SessionState.SetBool(Key,true); SessionState.SetString(Key+"Error","");
        EditorApplication.isPlaying=true;
    }
    public static void RunAuthored()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated batch project.");
        EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");
        SessionState.SetBool(Key,true); SessionState.SetString(Key+"Error","");
        EditorApplication.isPlaying=true;
    }
    public static void RunInventory()
    {
        SessionState.SetBool(Key+"InventoryMode",true); RunAuthored();
    }
    static void CheckInventoryUi(FarmGame farm)
    {
        try
        {
            if(shopFrames==0)
            {
                if(farm.InventoryCount!=4 || farm.InventoryItem(0).effect!=GrowthEffect.Dig || farm.InventoryItem(1).effect!=GrowthEffect.Water || !farm.InventoryItem(2).isSeed || farm.InventoryQuantity(2)!=10 || farm.InventoryItem(3).effect!=GrowthEffect.Scythe) throw new Exception("Starting kit incorrect");
                if(farm.InventoryOpen || farm.Combat.HudRect.x>30 || farm.Combat.HudRect.y>30 || farm.Combat.HudRect.height>40) throw new Exception("Default HUD incorrect");
            }
            if(shopFrames==10)
            {
                farm.SetInventoryOpen(true); if(!farm.InventoryOpen || farm.Gold!=farm.Shop.Gold) throw new Exception("Inventory gold unavailable");
            }
            if(shopFrames==20)
            {
                var npc=farm.GetComponentInChildren<ShopNpc>(); farm.TeleportPlayer(npc.transform.position-Vector3.up);
                if(!farm.Shop.Open(npc)) throw new Exception("Merchant cannot open alongside inventory");
            }
            if(shopFrames++<30) return;
            int gold=farm.Gold;
            if(!farm.Shop.Buy(0) || !farm.Shop.Sell(0) || farm.Gold>=gold) throw new Exception("Inventory shop transactions incorrect");
            farm.Shop.Close(); farm.SetInventoryOpen(false);
            if(farm.InventoryOpen) throw new Exception("Inventory did not close");
            farm.SetInventoryOpen(true); int before=farm.InventoryQuantity(2); farm.Initialize();
            if(farm.InventoryQuantity(2)!=before) throw new Exception("Starter kit granted twice");
            Finish(true,"INVENTORY_UI_PLAY_SUCCESS: starter kit, hearts position, inventory toggle, gold, shop, single grant.");
        }
        catch(Exception error) { Finish(false,error.ToString()); }
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<2)
        {
            if(EditorApplication.timeSinceStartup>deadline) Finish(false,"Play check timeout");
            return;
        }
        string error=SessionState.GetString(Key+"Error","");
        if(!string.IsNullOrEmpty(error)) { Finish(false,error); return; }
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        if(farm==null || farm.Combat==null || Camera.main==null) { Finish(false,"Runtime initialization failed"); return; }
        int expectedEnemies=farm.Layout!=null?farm.Layout.GetComponentsInChildren<EnemyPlacement>().Count(marker=>!farm.IsSafe(marker.transform.position)):6;
        if(!farm.Cycle.MonstersActive) expectedEnemies=0;
        if(farm.Combat.EnemyCount!=expectedEnemies) { Finish(false,"Authored enemy count mismatch"); return; }
        int expectedGround=farm.Layout!=null?farm.Layout.GetComponentsInChildren<FarmGround>().Length:1600;
        if(farm.GetComponentsInChildren<SpriteRenderer>().Length<expectedGround) { Finish(false,"Open field was not loaded"); return; }
        if(SessionState.GetBool(Key+"InventoryMode",false)) { CheckInventoryUi(farm); return; }
        if(farm.Shop!=null)
        {
            if(shopFrames==0)
            {
                var npc=farm.GetComponentInChildren<ShopNpc>();
                farm.TeleportPlayer(npc.transform.position-Vector3.up);
                int gold=farm.Shop.Gold;
                if(!farm.Shop.Open(npc) || !farm.Shop.Buy(0) || !farm.Shop.Sell(0) || farm.Shop.Gold>=gold) { Finish(false,"Runtime shop transaction failed"); return; }
            }
            if(shopFrames++<5) return;
        }
        Finish(true,"OPEN_FIELD_PLAY_SUCCESS: runtime Awake, Update, camera and enemies.");
    }
    static void Finish(bool success,string message)
    {
        SessionState.SetBool(Key,false); EditorApplication.update-=Tick;
        if(success) Debug.Log(message); else Debug.LogError(message);
        EditorApplication.Exit(success?0:1);
    }
}
