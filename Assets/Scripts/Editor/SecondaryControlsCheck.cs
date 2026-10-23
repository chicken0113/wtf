using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class SecondaryControlsCheck
{
    private const string Key="SecondaryControlsCheck";
    private static int step,lastFrame=-1;
    private static double deadline;
    private static FarmGame farm;
    private static ShopNpc npc;
    private static NpcProfile profile;
    private static int previousGold,previousQuantity,previousSlots;
    private static bool guiDone;
    private static Action guiAssertion,beforeGui;
    private static Vector2 guiClick;
    private static Vector2 GuiWorld => new Vector2(Screen.width-4,Screen.height*.5f);
    static SecondaryControlsCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        { deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick; }
    }
    private static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) ||
            !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use isolated project and saves.");
        Check(!File.ReadAllText("Assets/Scripts/FarmGame.Input.cs").Contains("KeyCode.E)") &&
            !File.ReadAllText("Assets/Scripts/FarmGame.Input.cs").Contains("KeyCode.F)"),"E/F remain bound");
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
        gameView.Show();gameView.position=new Rect(100,100,1280,800);gameView.Focus();
        EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void Right(Vector2 world,Vector2? gui=null) => farm.ProcessSecondaryPointer(world,gui??GuiWorld);
    private static Vector2 InventorySlot => farm.InventoryPanelRect.position+new Vector2(38,102);
    private static Vector2 HotbarSlot => new Vector2((Screen.width-farm.HotbarCapacity*56)/2+26,Screen.height-46);
    private static void QueueGui(Vector2 point,Action assertion,Action before=null) { guiClick=point;guiAssertion=assertion;beforeGui=before;guiDone=false; }
    public static void DrawProbe()
    {
        if(guiAssertion==null || Event.current.type!=EventType.Repaint)return;
        var assertion=guiAssertion;guiAssertion=null;var previous=Event.current;
        try
        {
            // Editor updates may precede a player frame; perform the opening/closing press in this GUI frame.
            var prepare=beforeGui;beforeGui=null;prepare?.Invoke();
            Event.current=new Event {type=EventType.MouseDown,button=1,mousePosition=guiClick};
            var hud=typeof(FarmGame).GetField("hud",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(farm);
            // Exercise the production shared slot handler without synthetic scroll-view clipping.
            var slot=hud.GetType().GetMethod("DrawSlot",BindingFlags.Instance|BindingFlags.NonPublic);
            slot.Invoke(hud,new object[]{new Rect(guiClick-Vector2.one*26,Vector2.one*52),0,!farm.InventoryOpen});
            assertion();guiDone=true;
        }
        catch(Exception error) { Fail(error); }
        finally { Event.current=previous; }
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Secondary control timeout: "+step);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.4f || GameSession.Busy || Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(farm==null || !farm.IsInitialized)return;
            farm.enabled=false;Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
            if(step==0)
            {
                farm.gameObject.AddComponent<SecondaryControlsGuiProbe>();
                InventoryCheckAccess.Clear(farm);
                var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");
                Check(farm.AddItem(seed)&&farm.AddItem(seed)&&farm.AddItem(seed),"Seed fixture failed");
                farm.TeleportPlayer(new Vector2(0,-3));farm.Till(new Vector2Int(0,-2)); // StartingPlot may already have tilled this cell.
                profile=new NpcProfile {id="secondary_fixture",displayName="Fixture",shopEnabled=false};farm.Shop.npcs.Add(profile);
                var go=new GameObject("Secondary fixture NPC");go.transform.SetParent(farm.transform);go.transform.position=new Vector2(1,-2);
                go.AddComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MapSprites/TestBlock.png");
                npc=go.AddComponent<ShopNpc>();npc.npcId=profile.id;npc.daytimeOnly=false;
                Right(new Vector2(0,-2));Check(!farm.Shop.IsOpen && farm.InventoryQuantity(0)==2,"Nearby NPC stole pointed planting");
                Right(new Vector2(0,-2));Check(farm.InventoryQuantity(0)==2 && !farm.IsMature(new Vector2Int(0,-2)),"Immature crop was recovered");
                farm.Water(new Vector2(0,-2),.1f);farm.Tick(seed.growthSeconds);
                npc.transform.position=new Vector2(0,-2);Right(npc.transform.position);
                Check(farm.Shop.IsOpen && farm.IsMature(new Vector2Int(0,-2)),"NPC priority harvested underlying crop");
                Right(Vector2.zero,farm.Shop.PanelRect.center);Check(farm.Shop.IsOpen,"Inside dialogue right click closed it");
                Check(farm.ProcessMenuInput(false,true,false,false) && !farm.Shop.IsOpen,"Esc did not close dialogue");
                npc.transform.position=new Vector2(1,-2);Right(new Vector2(0,-2));
                Check(!farm.IsMature(new Vector2Int(0,-2)) && !farm.Shop.IsOpen,"Right click did not harvest");
                profile.shopEnabled=true;Right(npc.transform.position);Check(farm.Shop.IsTrading,"Right click did not open shop");
                previousGold=farm.Gold;previousQuantity=farm.InventoryQuantity(0);
                QueueGui(InventorySlot,()=>Check(farm.Gold==previousGold && farm.InventoryQuantity(0)==previousQuantity,"Opening click sold an item"),()=>{farm.Shop.Close();Right(npc.transform.position);});step=1;return;
            }
            if(step==1)
            {
                if(!guiDone)return;
                Right(npc.transform.position,InventorySlot);Check(farm.Shop.IsOpen,"Inventory panel click closed shop");
                int price=farm.Shop.SalePrice(farm.InventoryItem(0));
                QueueGui(InventorySlot,()=>Check(farm.Gold==previousGold+price && farm.InventoryQuantity(0)==previousQuantity-1 && farm.Shop.IsOpen,"UI right click did not sell exactly one: gold="+farm.Gold+", quantity="+farm.InventoryQuantity(0)+", expected="+(previousGold+price)+"/"+(previousQuantity-1)));step=2;return;
            }
            if(step==2)
            {
                if(!guiDone)return;
                previousSlots=farm.RemovedSlots;var point=HotbarSlot;
                Right(npc.transform.position,point);Check(!farm.Shop.IsOpen,"Outside right click did not close shop");
                QueueGui(point,()=>Check(farm.RemovedSlots==previousSlots,"Closing click detached a hotbar slot"),()=>{Right(npc.transform.position);Right(npc.transform.position,point);});step=3;return;
            }
            if(step==3)
            {
                if(!guiDone)return;
                Right(Vector2.zero,HotbarSlot);
                QueueGui(HotbarSlot,()=>Check(farm.RemovedSlots==previousSlots+1,"Ordinary hotbar right click did not detach slot"));step=4;return;
            }
            if(step==4)
            {
                if(!guiDone)return;
                int hearts=farm.Combat.HeartCount;Right(Vector2.zero,farm.Combat.HeartRect(0).center);
                Check(farm.Combat.HeartCount==hearts-1,"Right click did not harvest one HP heart");
                Right(npc.transform.position);Check(farm.Shop.IsOpen,"Shop reopen failed");
                Check(farm.ProcessMenuInput(false,true,false,false) && !farm.Shop.IsOpen,"Esc did not close shop");
                profile.shopEnabled=false;profile.guildQuestGiver=true;Right(npc.transform.position);
                Check(farm.Shop.IsQuestDesk,"Guild panel failed to open");
                Right(Vector2.zero,farm.Shop.PanelRect.center);Check(farm.Shop.IsQuestDesk,"Guild inside click closed panel");
                Right(Vector2.zero,GuiWorld);Check(!farm.Shop.IsOpen,"Guild outside click failed");
                var door=farm.GetComponentsInChildren<MapExit>().Single(e=>e.requireInteract && e.destinationScene==InteriorWorldSetup.Home);
                farm.TeleportPlayer(door.transform.position);int quantity=farm.InventoryCount;
                farm.DropItem(Resources.Load<PlantDefinition>("Crops/ParsnipSeeds"),door.transform.position);
                Right(door.transform.position);Check(GameSession.Busy && farm.InventoryCount==quantity,"Door priority picked up underlying item");step=5;return;
            }
            if(step==5)
            {
                Check(farm.gameObject.scene.path==InteriorWorldSetup.Home,"Right click door did not enter house");
                var door=farm.GetComponentInChildren<MapExit>();farm.TeleportPlayer(door.transform.position);
                Right(new Vector2(50,50));Check(!GameSession.Busy,"Unpointed door was activated");
                Right(door.transform.position);Check(GameSession.Busy,"Right click door did not exit house");step=6;return;
            }
            Check(farm.gameObject.scene.path==CombatTestMapSetup.FarmScene,"House did not return to farm");
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            Debug.Log("SECONDARY_CONTROLS_SUCCESS: pointed NPC/door priority, pickup/plant/harvest, no immature recovery, real UI sell/detach, consumed opening/closing clicks, heart harvest, dialogue/shop/guild outside close and Esc.");EditorApplication.Exit(0);
        }
        catch(Exception error) { Fail(error); }
    }
    private static void Fail(Exception error) { SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1); }
}
public sealed class SecondaryControlsGuiProbe : MonoBehaviour
{
    private void OnGUI()=>SecondaryControlsCheck.DrawProbe();
}
