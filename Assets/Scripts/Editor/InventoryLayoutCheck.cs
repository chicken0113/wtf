using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class InventoryLayoutCheck
{
    private const string Key="InventoryLayoutVisual";
    private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static int step;
    private static double deadline;
    private static FarmGame farm;
    private static bool guiDone;
    private static string imagePath;
    static InventoryLayoutCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        { deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick; }
    }
    private static void Check(bool value,string text) { if(!value)throw new Exception(text); }
    private static void Guard()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) &&
            Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project/save only");
    }
    private static FarmGame Reload()
    {
        GameSession.ResetForTests();EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();f.Initialize(false);GameSession.Bind(f);
        Check(f.IsInitialized && string.IsNullOrEmpty(GameSession.LastError),"Restore failed: "+GameSession.LastError);return f;
    }
    public static void Run()
    {
        Guard();var settings=Resources.Load<StartingInventory>("StartingInventory");settings.hotbarSlots=4;settings.storageSlots=4;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        var f=Reload();Check(f.Capacity==8 && f.HotbarCapacity==4 && f.StorageCapacity==4,"Independent capacity settings failed: "+f.Capacity+"/"+f.HotbarCapacity+"/"+f.StorageCapacity);
        var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
        var water=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/WateringCan.asset");
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");
        Check(f.AddItem(hoe)&&f.AddItem(water)&&f.AddItem(seed)&&f.AddItem(seed),"Loadout failed");
        Check(f.MoveInventoryItem(0,6) && f.InventoryItem(0)==null && f.InventoryItem(6)==hoe && f.InventoryItem(1)==water,"Empty-slot move shifted neighbours");
        Check(f.MoveInventoryItem(6,1) && f.InventoryItem(1)==hoe && f.InventoryItem(6)==water,"Cross-region swap failed");
        Check(f.RemoveInventoryItem(1) && f.InventoryItem(1)==null && f.InventoryItem(2)==seed && f.InventoryQuantity(2)==2 && f.InventoryItem(6)==water,"Removing a slot shifted or lost stacks");
        Check(!f.MoveInventoryItem(1,0) && !f.MoveInventoryItem(2,8) && !f.MoveInventoryItem(-1,0),"Invalid move accepted");
        f.Shop.RestoreGold(345);Check(GameSession.Save(f),"Indexed save failed");
        f=Reload();Check(f.Gold==345 && f.InventoryCount==2 && f.InventoryItem(0)==null && f.InventoryItem(2)==seed && f.InventoryQuantity(2)==2 && f.InventoryItem(6)==water,"Reload compacted slots or lost quantities");
        var legacy=f.CapturePlayer(catalog);legacy.inventoryUsesSlots=false;legacy.inventory.Clear();
        legacy.inventory.Add(new ItemStackSave {item=catalog.Id(water),count=1});legacy.inventory.Add(new ItemStackSave {item=catalog.Id(seed),count=7});
        f.RestorePlayer(legacy,catalog);f.ReconcileRestoredInventory();
        Check(f.InventoryItem(0)==water && f.InventoryItem(1)==seed && f.InventoryQuantity(1)==7 && f.InventoryItem(6)==null,"Legacy ordered inventory lost items");
        var invalid=f.CapturePlayer(catalog);invalid.inventory[1].slot=invalid.inventory[0].slot;bool rejected=false;
        try { f.RestorePlayer(invalid,catalog); } catch(InvalidOperationException) { rejected=true; }
        Check(rejected && f.InventoryItem(0)==water && f.InventoryQuantity(1)==7,"Duplicate slot corrupted live inventory");
        var oversized=f.CapturePlayer(catalog);oversized.inventory[1].slot=12;int drops=f.WorldPieceCount;
        f.RestorePlayer(oversized,catalog);f.ReconcileRestoredInventory();
        Check(f.InventoryCount==1 && f.InventoryItem(0)==water && f.WorldPieceCount==drops+7,"Capacity reduction lost out-of-range stacks");
        InventoryCheckAccess.Clear(f);f.AddItem(hoe);f.AddItem(water);f.AddItem(seed);f.MoveInventoryItem(2,7);
        Check(f.DetachSlot() && f.Capacity==7 && f.InventoryItem(7)==null && Enumerable.Range(0,7).Any(i=>f.InventoryItem(i)==seed),"Slot detachment lost last-slot item");
        Check(f.RestoreSlot() && f.Capacity==8,"Slot restore failed after fixed-slot detachment");
        Debug.Log("INVENTORY_LAYOUT_RULES_SUCCESS: independent capacity, stable holes/move/swap/remove, indexed reload, legacy migration, duplicate rejection, overflow drops and detachable slot safety.");
    }
    public static void RunVisual()
    {
        Guard();var settings=Resources.Load<StartingInventory>("StartingInventory");settings.hotbarSlots=4;settings.storageSlots=8;
        EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.position=new Rect(100,100,1280,800);view.Focus();
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static object Hud=>typeof(FarmGame).GetField("hud",Members).GetValue(farm);
    private static object Call(object hud,string name,params object[] args)=>hud.GetType().GetMethod(name,Members).Invoke(hud,args);
    private static void Slot(object hud,int index,Vector2 point,EventType type)
    {
        Event.current=new Event {type=type,button=0,mousePosition=point};
        Call(hud,"DrawSlot",new Rect(point-Vector2.one*26,Vector2.one*52),index,index<farm.HotbarCapacity);
    }
    public static void Probe()
    {
        if(farm==null)return;var hud=Hud;
        if(!guiDone && Event.current.type==EventType.Repaint)
        {
            var previous=Event.current;
            try
            {
                int drops=farm.WorldPieceCount;var hoe=farm.InventoryItem(0);
                Slot(hud,0,new Vector2(26,26),EventType.MouseDown);
                Event.current=new Event {type=EventType.MouseDrag,button=0,mousePosition=new Vector2(126,26)};Call(hud,"UpdateDrag",new Vector2(126,26));
                Slot(hud,4,new Vector2(126,26),EventType.MouseUp);Call(hud,"UpdateDrag",new Vector2(126,26));
                Check(farm.InventoryItem(0)==null && farm.InventoryItem(4)==hoe,"UI drag did not move hotbar item into backpack");
                Slot(hud,4,new Vector2(26,26),EventType.MouseDown);
                hud.GetType().GetField("hoveredSlot",Members).SetValue(hud,-1);
                Event.current=new Event {type=EventType.MouseUp,button=0,mousePosition=new Vector2(226,26)};Call(hud,"UpdateDrag",new Vector2(226,26));
                Check(farm.InventoryItem(4)==hoe && farm.WorldPieceCount==drops,"Outside drop lost item");
                Slot(hud,4,new Vector2(26,26),EventType.MouseDown);farm.ProcessMenuInput(false,true,false);
                Event.current=new Event {type=EventType.Repaint};Call(hud,"Draw");
                Check((int)hud.GetType().GetField("dragSource",Members).GetValue(hud)==-1 && farm.InventoryItem(4)==hoe,"Esc did not cancel drag safely");
                farm.ProcessMenuInput(true,false,false);guiDone=true;
                Debug.Log("INVENTORY_LAYOUT_UI_SUCCESS: production slot drag handlers, hotbar/backpack transfer, outside cancel and Esc retain items.");
            }
            catch(Exception error) { Fail(error); }
            finally { Event.current=previous; }
        }
        Call(hud,"Draw");
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Inventory UI timeout: "+step);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.5f || GameSession.Busy)return;
            farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(farm==null || !farm.IsInitialized)return;
            farm.enabled=false;
            if(step==0)
            {
                InventoryCheckAccess.Clear(farm);farm.AddItem(AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset"));
                farm.AddItem(AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/WateringCan.asset"));farm.AddItem(Resources.Load<PlantDefinition>("Crops/ParsnipSeeds"));
                farm.ProcessMenuInput(true,false,false);farm.gameObject.AddComponent<InventoryLayoutGuiProbe>();step=1;return;
            }
            if(step==1 && guiDone) { imagePath=Path.Combine(Path.GetDirectoryName(Application.dataPath),"InventoryLayout.png");ScreenCapture.CaptureScreenshot(imagePath);step=2;return; }
            if(step==2 && File.Exists(imagePath))
            { SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log("INVENTORY_LAYOUT_VISUAL_SUCCESS: "+imagePath);EditorApplication.Exit(0); }
        }
        catch(Exception error) { Fail(error); }
    }
    private static void Fail(Exception error) { SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1); }
}
public sealed class InventoryLayoutGuiProbe:MonoBehaviour
{
    private void OnGUI()=>InventoryLayoutCheck.Probe();
}
