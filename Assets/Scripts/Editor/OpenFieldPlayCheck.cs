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
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        SessionState.SetBool(Key,true); SessionState.SetString(Key+"Error","");
        EditorApplication.isPlaying=true;
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
        if(farm.Shop!=null)
        {
            if(shopFrames==0)
            {
                var npc=farm.GetComponentInChildren<ShopNpc>();
                farm.MovePlayer(npc.transform.position-Vector3.up);
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
