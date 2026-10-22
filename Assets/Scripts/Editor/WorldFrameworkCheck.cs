using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class WorldFrameworkCheck
{
    const string Key="WtfWorldCheck"; static int step,expectedGold; static bool resume; static double deadline;
    static WorldFrameworkCheck()
    { if(Application.isBatchMode && SessionState.GetBool(Key,false)) { deadline=EditorApplication.timeSinceStartup+100; resume=SessionState.GetBool(Key+"Resume",false); EditorApplication.update+=Tick; } }
    static int menuStep;
    static bool menusVerified;
    static float menuStarted,pausedCycle;
    static double pausedWorld;
    static bool CheckMenus(FarmGame farm)
    {
        if(menuStep==0)
        {
            Require(farm.ProcessMenuInput(true,false,false,false) && farm.InventoryOpen,"Tab did not open inventory");
            menuStarted=Time.realtimeSinceStartup; pausedCycle=farm.Cycle.Elapsed; pausedWorld=GameSession.WorldSeconds; menuStep=1; return false;
        }
        if(Time.realtimeSinceStartup-menuStarted<.2f) return false;
        if(menuStep<4) Require(farm.Cycle.Elapsed==pausedCycle && GameSession.WorldSeconds==pausedWorld,"Simulation continued behind a menu");
        var exit=farm.GetComponentsInChildren<MapExit>().First();
        if(menuStep==1)
        {
            Require(!exit.TryActivate(farm,true),"Travel occurred behind inventory");
            Require(farm.ProcessMenuInput(false,true,false,false) && !farm.InventoryOpen,"Escape did not close inventory");
            Require(farm.ProcessMenuInput(false,false,true,false) && farm.MapOpen,"M did not open map"); menuStep=2;
        }
        else if(menuStep==2)
        {
            Require(!exit.TryActivate(farm,true),"Travel occurred behind map");
            Require(farm.ProcessMenuInput(false,false,true,false) && !farm.MapOpen,"M did not close map");
            var npc=farm.GetComponentInChildren<ShopNpc>(); farm.TeleportPlayer(npc.transform.position-Vector3.up);
            Require(farm.Shop.Open(npc) && farm.Shop.IsTrading,"Menu test shop failed"); menuStep=3;
        }
        else if(menuStep==3)
        {
            Require(farm.ProcessMenuInput(false,false,false,true) && !farm.InventoryOpen,"E was not consumed while closing shop");
            menuStep=4;
        }
        else
        {
            Require(farm.Cycle.Elapsed>pausedCycle && GameSession.WorldSeconds>pausedWorld,"Simulation did not resume after menu close");
            Debug.Log("FOUNDATION_MENUS_SUCCESS: real-frame inventory, map and shop pause; blocked exits; menu key consumption; resume."); return true;
        }
        menuStarted=Time.realtimeSinceStartup; return false;
    }
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Run() { Start(false); }
    public static void Resume() { Start(true); }
    static void Start(bool value)
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use isolated project and test saves.");
        EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene); WorldFrameworkSetup.Connect();
        SessionState.SetBool(Key,true); SessionState.SetBool(Key+"Resume",value); EditorApplication.isPlaying=true;
    }
    static MapExit Exit(FarmGame farm,string target,bool door) => farm.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==target && e.requireInteract==door);
    static void Travel(FarmGame farm,string target,bool door)
    {
        var exit=Exit(farm,target,door); farm.TeleportPlayer(exit.transform.position);
        Require(new MapCollision(farm).CanStand(farm.PlayerPosition,farm.PlayerRadius),"Exit blocked: "+target);
        Require(exit.TryActivate(farm,door),"Exit failed: "+target+" "+GameSession.LastError);
    }
    static void At(FarmGame farm,string scene,string arrival)
    {
        Require(farm.gameObject.scene.path==scene,"Unexpected scene at step "+step);
        var point=MapArrivalPoint.Find(farm.gameObject.scene,arrival); Require(point!=null && Vector2.Distance(farm.PlayerPosition,point.transform.position)<.05f,"Wrong arrival at step "+step);
        Require(new MapCollision(farm).CanStand(farm.PlayerPosition,farm.PlayerRadius),"Arrival overlaps obstacle");
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("World check timeout at "+step+" "+GameSession.LastError);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.3f || GameSession.Busy || Time.unscaledTime<GameSession.ArrivalGraceUntil) return;
            var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); if(farm==null || farm.Combat==null) return;
            Require(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(OutdoorDoorCleanup.IsOutdoor(farm.gameObject.scene.path)) Require(!farm.GetComponentsInChildren<MapExit>(true).Any(e=>e.requireInteract&&OutdoorDoorCleanup.IsOutdoor(e.destinationScene)),"Outdoor E door remains");
            if(resume)
            {
                Require(farm.gameObject.scene.path==WorldFrameworkSetup.Town && farm.InventoryCount==4 && farm.InventoryQuantity(2)==10 && farm.Gold>0 && farm.Gold<333,"World did not resume correctly");
                Finish("WORLD_RESTART_SUCCESS"); return;
            }
            if(step==0)
            {
                if(!menusVerified) { menusVerified=CheckMenus(farm); if(!menusVerified) return; }
                Require(farm.InventoryCount==4,"Starter kit changed");
                var npc=farm.GetComponentInChildren<ShopNpc>(); farm.TeleportPlayer(npc.transform.position-Vector3.up);
                Require(farm.Shop.Open(npc) && farm.Shop.IsTrading,"Existing farm shop no longer works"); farm.Shop.Close();
                farm.Shop.RestoreGold(333);
                var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds"); farm.AddItem(seed); farm.TeleportPlayer(new Vector2(0,-2));
                Require(farm.PlantSelected(new Vector2Int(0,-2)),"Farm crop setup failed"); farm.Water(new Vector2(0,-2),.1f); farm.Tick(seed.growthSeconds);
                farm.DropItem(Resources.Load<PlantDefinition>("Equipment/Heart"),new Vector2(2,-4));
                step=1; Travel(farm,WorldFrameworkSetup.Town,false); return;
            }
            if(step==1)
            {
                At(farm,WorldFrameworkSetup.Town,"town_from_farm"); Require(farm.InventoryCount==4 && farm.Gold==333 && farm.Combat.EnemyCount==0,"Town lost state or is hostile");
                Require(farm.GetComponentsInChildren<ShopNpc>().Length==3,"Residents missing");
                var guide=farm.GetComponentsInChildren<ShopNpc>().Single(n=>n.npcId=="town_guide"); farm.TeleportPlayer((Vector2)guide.transform.position+Vector2.down);
                Require(farm.Shop.OpenNearby() && farm.InventoryOpen && !farm.Shop.IsTrading && !farm.Shop.Buy(0) && !farm.Shop.Sell(0),"Dialogue unexpectedly trades"); farm.Shop.Close();
                farm.Cycle.RestoreElapsed(farm.Cycle.Settings.daylightSeconds+farm.Cycle.Settings.duskSeconds+1);
                Require(guide.IsVisiting && farm.Shop.Open(guide) && farm.Combat.EnemyCount==0,"Resident dialogue unavailable at night"); farm.Shop.Close(); farm.Cycle.RestoreElapsed(0);
                Snapshot(farm,"TownOverview.png",Vector2.zero,farm.WorldBounds.height*.55f);
                step=2; Travel(farm,WorldFrameworkSetup.Store,true); return;
            }
            if(step==2)
            {
                At(farm,WorldFrameworkSetup.Store,"store_entry"); Require(farm.Gold==333 && farm.InventoryCount==4,"Store entry changed state");
                var npc=farm.GetComponentInChildren<ShopNpc>(); farm.TeleportPlayer(new Vector2(0,.8f)); Require(new MapCollision(farm).CanStand(farm.PlayerPosition,farm.PlayerRadius),"Counter approach blocked");
                Require(farm.Shop.Open(npc) && farm.Shop.IsTrading,"Store not usable over counter");
                int offer=farm.Shop.npcs[0].offers.FindIndex(o=>o.item==farm.InventoryItem(2)); var stock=farm.Shop.npcs[0].offers[offer];
                Require(farm.Shop.Buy(offer) && farm.InventoryQuantity(2)==11,"Store purchase failed");
                int price=farm.Shop.SalePrice(stock.item); Require(farm.Shop.Sell(2) && farm.InventoryQuantity(2)==10,"Store sale failed");
                expectedGold=333-stock.buyPrice+price; Require(farm.Gold==expectedGold,"Store prices changed");
                farm.Cycle.Tick(farm.Cycle.Settings.daylightSeconds+1); Require(!npc.IsVisiting && !farm.Shop.IsOpen && !farm.Shop.Buy(offer),"Store stayed open at night"); farm.Cycle.RestoreElapsed(0);
                Snapshot(farm,"StoreInterior.png",Vector2.zero,6.5f);
                step=3; Travel(farm,WorldFrameworkSetup.Town,true); return;
            }
            if(step==3) { At(farm,WorldFrameworkSetup.Town,"town_from_store"); Require(farm.Gold==expectedGold,"Gold lost on store return"); step=4; Travel(farm,CombatTestMapSetup.CombatScene,false); return; }
            if(step==4) { At(farm,CombatTestMapSetup.CombatScene,"combat_path_entry"); Require(farm.Combat.EnemyCount==2,"Dungeon enemies missing"); step=5; Travel(farm,WorldFrameworkSetup.Town,false); return; }
            if(step==5) { At(farm,WorldFrameworkSetup.Town,"town_from_dungeon_path"); step=6; Travel(farm,CombatTestMapSetup.FarmScene,false); return; }
            if(step==6)
            {
                At(farm,CombatTestMapSetup.FarmScene,"farm_path_entry"); Require(farm.IsMature(new Vector2Int(0,-2)) && farm.WorldPieceCount==1 && farm.Gold==expectedGold && farm.InventoryCount==4,"Farm state or inventory lost on return");
                step=7; Travel(farm,WorldFrameworkSetup.Town,false); return;
            }
            if(step==7) { At(farm,WorldFrameworkSetup.Town,"town_from_farm"); step=8; Travel(farm,CombatTestMapSetup.CombatScene,false); return; }
            if(step==8) { At(farm,CombatTestMapSetup.CombatScene,"combat_path_entry"); step=9; Travel(farm,WorldFrameworkSetup.Town,false); return; }
            if(step==9)
            {
                At(farm,WorldFrameworkSetup.Town,"town_from_dungeon_path"); Require(farm.Gold==expectedGold && farm.InventoryCount==4 && File.Exists(GameSession.SavePath),"Final autosave/state incorrect");
                // Repeat automatic return before checking the direct shortcut.
                step=10; Travel(farm,CombatTestMapSetup.FarmScene,false); return;
            }
            if(step==10) { At(farm,CombatTestMapSetup.FarmScene,"farm_path_entry"); step=11; Travel(farm,WorldFrameworkSetup.Town,false); return; }
            if(step==11) { At(farm,WorldFrameworkSetup.Town,"town_from_farm"); if(farm.GetComponentsInChildren<Transform>().Any(item=>item.name=="Sketch layout 2026-10-21")) { step=12; Travel(farm,CombatTestMapSetup.FarmScene,false); return; } Finish("WORLD_FRAMEWORK_PLAY_SUCCESS: all four scenes, both route types, dialogue and night residents, daytime store, buy/sell, shared gold/inventory, farm crop/drop preservation, collision and autosaves."); }
            if(step==12) { At(farm,CombatTestMapSetup.FarmScene,"farm_path_entry"); step=13; Travel(farm,CombatTestMapSetup.CombatScene,false); return; }
            if(step==13) { At(farm,CombatTestMapSetup.CombatScene,"combat_from_farm"); step=14; Travel(farm,CombatTestMapSetup.FarmScene,false); return; }
            if(step==14) { At(farm,CombatTestMapSetup.FarmScene,"farm_from_dungeon"); Require(farm.Gold==expectedGold && farm.InventoryCount==4 && farm.IsMature(new Vector2Int(0,-2)),"Shortcut lost player/farm state"); step=15; Travel(farm,WorldFrameworkSetup.Town,false); return; }
            if(step==15) { At(farm,WorldFrameworkSetup.Town,"town_from_farm"); Finish("TOWN_SKETCH_PLAY_SUCCESS: world roundtrips, store/NPCs, farm-dungeon shortcut both ways, inventory/gold/crops, autosave."); }
        }
        catch(Exception error) { SessionState.SetBool(Key,false); EditorApplication.update-=Tick; Debug.LogException(error); EditorApplication.Exit(1); }
    }
    static void Finish(string text) { SessionState.SetBool(Key,false); EditorApplication.update-=Tick; Debug.Log(text); EditorApplication.Exit(0); }
    static void Snapshot(FarmGame farm,string path,Vector2 position,float size)
    {
        var camera=Camera.main; camera.transform.position=new Vector3(position.x,position.y,-10); camera.orthographicSize=size;
        var target=new RenderTexture(1280,800,24); camera.targetTexture=target; farm.Cycle.RefreshLighting(); camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,800,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}
