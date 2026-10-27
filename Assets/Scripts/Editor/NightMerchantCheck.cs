using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
[InitializeOnLoad]
public static class NightMerchantCheck
{
    const string Key="NightMerchantCheckActive",Forest="Assets/Scenes/ForestClearing.unity",Store="Assets/Scenes/GeneralStore.unity";
    static int step,seed;static double deadline;static Vector2 chosen;
    static NightMerchantCheck(){if(Application.isBatchMode && SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project and saves only");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");Check(NightMerchantExampleSetup.Apply(),"Visitor authoring failed");string scene=File.ReadAllText(Forest);Check(NightMerchantExampleSetup.Apply() && File.ReadAllText(Forest)==scene,"Visitor authoring duplicated objects");
        var opened=EditorSceneManager.OpenScene(Forest);var manager=UnityEngine.Object.FindFirstObjectByType<NpcManager>();manager.npcs.Single(p=>p.id==NightMerchantExampleSetup.Id).visitChance=1;EditorUtility.SetDirty(manager);EditorSceneManager.SaveScene(opened);
        ProjectSetupCheck.Run();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static NpcProfile Profile(FarmGame f)=>f.Shop.npcs.Single(p=>p.id==NightMerchantExampleSetup.Id);
    static ShopNpc Visitor(FarmGame f)=>f.GetComponentsInChildren<ShopNpc>().Single(n=>n.npcId==NightMerchantExampleSetup.Id);
    static GameSaveData Saved()=>JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
    static NightVisitSave Visit()=>Saved().nightVisits.Single(v=>v.scene==Forest && v.profileId==NightMerchantExampleSetup.Id);
    static float Night(FarmGame f)=>f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+1;
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Merchant test timeout: "+step);if(!EditorApplication.isPlaying)return;
            var loaded=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(loaded!=null && loaded.IsInitialized)loaded.enabled=false;
            if(GameSession.Busy || Time.timeSinceLevelLoad<.1f)return;var f=GameSession.CurrentFarm;if(f==null || !f.IsInitialized)return;Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(step==0)
            {
                var npc=Visitor(f);Check(!npc.IsVisiting && !f.Shop.CanTrade,"Daytime merchant remained");
                f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+1);Check(!npc.IsVisiting,"Merchant appeared during dusk");
                f.Cycle.RestoreElapsed(Night(f));Check(npc.IsVisiting && f.Shop.CanTrade,"Night visitor missing");chosen=npc.transform.position;
                Check(Profile(f).visitLocations.Any(p=>Vector2.Distance(p.position,chosen)<.001f),"Visitor chose non-candidate location");
                f.TeleportPlayer(chosen+Vector2.down);InventoryCheckAccess.Clear(f);Check(f.Shop.Open(npc) && f.Shop.Buy(0),"Night buying failed");
                int slot=Enumerable.Range(0,f.Capacity).First(i=>f.InventoryItem(i)!=null);Check(f.Shop.Sell(slot),"Night selling failed");
                f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+f.Cycle.Settings.nightSeconds+1);Check(!npc.IsVisiting && !f.Shop.IsOpen && !f.InventoryOpen,"Dawn did not close shop");
                f.Cycle.RestoreElapsed(Night(f));Profile(f).visitChance=0;f.Shop.UpdateAvailability();Check(npc.IsVisiting && Vector2.Distance(npc.transform.position,chosen)<.001f,"Same-night visit rerolled");
                f.Cycle.Tick(f.Cycle.CycleSeconds);Check(f.Cycle.CycleNumber==1 && !npc.IsVisiting,"Next night probability zero ignored");Check(GameSession.Save(f),"Absent-visit save failed");seed=Saved().merchantSeed;Check(seed!=0 && !Visit().appears,"Absent outcome not recorded");
                Check(GameSession.Travel(f,Store,null,new Vector2(0,-2)),"Store travel failed");step=1;return;
            }
            if(step==1)
            {
                var npc=f.GetComponentsInChildren<ShopNpc>().First(n=>f.Shop.npcs.Any(p=>p.id==n.npcId && p.shopEnabled));Check(npc.IsVisiting,"Fixed indoor merchant not available at night");
                f.Cycle.RestoreElapsed(0,f.Cycle.CycleNumber);Check(!npc.IsVisiting && !f.Shop.CanTrade,"Fixed indoor merchant remained by day");
                f.Cycle.RestoreElapsed(Night(f),1);Check(npc.IsVisiting,"Fixed merchant did not return at night");Check(GameSession.Travel(f,Forest,null,new Vector2(10,-5)),"Forest return failed");step=2;return;
            }
            if(step==2)
            {
                var npc=Visitor(f);Check(f.Cycle.CycleNumber==1 && Profile(f).visitChance==1 && !npc.IsVisiting,"Same-night absence rerolled on map return");
                f.Cycle.Tick(f.Cycle.CycleSeconds);Check(f.Cycle.CycleNumber==2 && npc.IsVisiting,"Next night did not permit new visit");chosen=npc.transform.position;Check(GameSession.Save(f),"Resume snapshot failed");
                Check(Visit().cycleNumber==2 && Visit().appears && Saved().merchantSeed==seed,"Seed/night record changed");
                step=3;GameSession.ReleaseToTitle();SceneManager.LoadScene(Forest,LoadSceneMode.Single);return;
            }
            if(step==3)
            {
                var npc=Visitor(f);Check(npc.IsVisiting && f.Cycle.CycleNumber==2 && Vector2.Distance(npc.transform.position,chosen)<.001f && Saved().merchantSeed==seed,"Disk reload lost visit position/night/seed");
                Profile(f).visitChance=0;f.Shop.UpdateAvailability();Check(npc.IsVisiting,"Reloaded night rerolled");
                var profile=Profile(f);profile.visitChance=float.NaN;var report=new SetupValidationReport();FarmSetupValidator.Check(f,GameSetupResources.Load(),report,true,false);Check(report.HasErrors,"Invalid chance accepted");profile.visitChance=1;
                Finish(0,"NIGHT_MERCHANT_SUCCESS: strict night schedule, dusk/dawn hiding, buy/sell and forced closure, fixed indoor merchants, per-night success/absence cache, map travel and real disk reload preserving night/seed/position, next-night roll and invalid config diagnostics.");
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1,"NIGHT_MERCHANT_FAILED step "+step+": "+e.Message);}
    }
    static void Finish(int code,string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(code);}
}
