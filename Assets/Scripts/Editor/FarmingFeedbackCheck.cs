using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class FarmingFeedbackCheck
{
    private const string VisualKey="FarmingFeedbackVisual";
    private static double deadline;
    private static int frames;
    private static string imagePath;
    static FarmingFeedbackCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(VisualKey,false))
        { deadline=EditorApplication.timeSinceStartup+90; EditorApplication.update+=VisualTick; }
    }
    public static void RunVisual()
    {
        Guard();
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use isolated save directory.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        SessionState.SetString(VisualKey+"Error","");
        var gameView=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));gameView.Show();gameView.position=new Rect(100,100,1280,800);gameView.Focus();
        SessionState.SetBool(VisualKey,true); EditorApplication.isPlaying=true;
    }
    private static void VisualTick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Feedback rendering timeout: frames="+frames+", playing="+EditorApplication.isPlaying+", busy="+GameSession.Busy+", time="+Time.timeSinceLevelLoad+", image="+imagePath);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.4f || GameSession.Busy) return;
            var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
            if(farm==null || !farm.IsInitialized) return;
            if(frames++==0)
            {
                Debug.Log("FEEDBACK_VISUAL_SETUP");
                farm.enabled=false; // Freeze growth while rendering the actual HUD through the probe.
                while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
                var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");
                var dry=new Vector2Int(0,-2);var growing=new Vector2Int(1,-2);var ripe=new Vector2Int(2,-2);
                foreach(var cell in new[]{dry,growing,ripe}) { farm.Till(cell); farm.AddItem(seed); Check(farm.PlantSelected(cell),"Visual fixture planting failed"); }
                farm.Water(ripe,.1f); farm.Tick(seed.growthSeconds);
                farm.Water(growing,.1f); farm.Tick(seed.growthSeconds*.5f);
                farm.TeleportPlayer(new Vector2(0,-4));
                farm.AddItem(AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/WateringCan.asset"));
                Query(farm,"LateUpdate");
                var camera=Camera.main;camera.transform.position=new Vector3(1,-1.8f,-10);camera.orthographicSize=4.8f;
                CheckIcons(farm,seed);
                Query(farm,"UpdateFarmingIcon",new Vector2(1.1f,-2.1f),false);
                var probe=farm.gameObject.AddComponent<FarmingFeedbackGuiProbe>();probe.farm=farm;
                Application.logMessageReceived+=(message,trace,type)=> { if(type==LogType.Exception && trace.Contains("EverythingGrows.")) SessionState.SetString(VisualKey+"Error",message+trace); };
            }
            if(frames==12)
            {
                imagePath=Path.GetFullPath("FarmingIcons-"+Guid.NewGuid().ToString("N")+".png");
                ScreenCapture.CaptureScreenshot(imagePath);Debug.Log("FEEDBACK_VISUAL_CAPTURE: "+imagePath);
            }
            if(frames<24 || !File.Exists(imagePath)) return;
            Check(string.IsNullOrEmpty(SessionState.GetString(VisualKey+"Error","")),SessionState.GetString(VisualKey+"Error",""));
            var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
            var icon=(SpriteRenderer)typeof(FarmGame).GetField("plantingPreview",Members).GetValue(f);
            f.SetInventoryOpen(true);Query(f,"UpdateFarmingIcon",new Vector2(0,-2),false);
            Check(!icon.enabled && !Border(f).enabled,"Preview leaked through inventory");
            f.SetInventoryOpen(false);f.SetMapOpen(true);Query(f,"UpdateFarmingIcon",new Vector2(0,-2),false);
            Check(!icon.enabled && !Border(f).enabled,"Preview leaked through map");
            f.SetMapOpen(false);
            SessionState.SetBool(VisualKey,false);EditorApplication.update-=VisualTick;
            Debug.Log("FARMING_FEEDBACK_VISUAL_SUCCESS: valid cell has one icon and green border; invalid cells, empty hands, range and menus hide both. Image: "+imagePath);
            EditorApplication.Exit(0);
        }
        catch(Exception e) { SessionState.SetBool(VisualKey,false);Debug.LogException(e);EditorApplication.Exit(1); }
    }
    private static void CheckIcons(FarmGame farm,PlantDefinition seed)
    {
        var icon=(SpriteRenderer)typeof(FarmGame).GetField("plantingPreview",Members).GetValue(farm);
        var art=(FarmArt)Read(farm,"art");
        var water=farm.SelectedItem;
        Query(farm,"UpdateFarmingIcon",new Vector2(.15f,-2.1f),false);
        Check(icon.enabled && icon.sprite==art.ItemSprite(water),"Watering tool icon missing");
        Check(icon.transform.position==new Vector3(0,-2,0),"Icon not centered in cell");
        Check(Border(farm).enabled && Border(farm).transform.position==icon.transform.position && Border(farm).bounds.size.x==1 && Border(farm).color.g>Border(farm).color.r,"Green border does not surround selected cell");
        Query(farm,"UpdateFarmingIcon",new Vector2(-.2f,-1.9f),false);
        Check(icon.transform.position==new Vector3(0,-2,0),"Icon followed pointer inside a cell");
        Query(farm,"UpdateFarmingIcon",new Vector2(6,-2),false);
        Check(!icon.enabled && !Border(farm).enabled,"Out-of-range preview visible");
        Query(farm,"UpdateFarmingIcon",new Vector2(0,-2),true);
        Check(!icon.enabled && !Border(farm).enabled,"Preview visible over UI");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        Query(farm,"UpdateFarmingIcon",new Vector2(0,-2),false);
        Check(!icon.enabled && !Border(farm).enabled,"Empty hands displayed preview");
        farm.AddItem(seed);var empty=new Vector2Int(0,-3);farm.Till(empty);
        Query(farm,"UpdateFarmingIcon",(Vector2)empty,false);
        Check(icon.enabled && icon.sprite!=art.ItemSprite(seed) && icon.color.g>icon.color.r,"Planting icon missing or wrongly invalid");
        var plantingSprite=icon.sprite;
        Query(farm,"UpdateFarmingIcon",new Vector2(0,-2),false);
        Check(!icon.enabled && !Border(farm).enabled,"Occupied cell preview visible");
        foreach(var name in new[]{"Hoe","Scythe"})
        {
            var item=name=="Hoe"?AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset"):Resources.Load<PlantDefinition>("Crops/Scythe");
            Check(item!=null,"Missing tool "+name);farm.AddItem(item);
            var target=name=="Hoe"?new Vector2Int(0,-4):empty;
            if(name=="Scythe")
            {
                Query(farm,"UpdateFarmingIcon",(Vector2)empty,false);
                Check(icon.enabled && icon.sprite==plantingSprite && Border(farm).enabled,"Scythe did not fall back to planting on empty soil");
                farm.RemoveInventoryItem(farm.InventoryCount-1);farm.AddItem(seed);Check(farm.PlantSelected(empty),"Dead-crop fixture planting failed");farm.AddItem(item);
                foreach(var plot in (System.Collections.IEnumerable)Read(Read(farm,"Farming"),"Plots"))
                    if((Vector2Int)plot.GetType().GetField("cell").GetValue(plot)==empty) plot.GetType().GetField("wilted").SetValue(plot,true);
            }
            Query(farm,"UpdateFarmingIcon",(Vector2)target,false);
            if(name=="Scythe")Check(!icon.enabled && !Border(farm).enabled,"Scythe still removed planted crops");
            else Check(icon.enabled && Border(farm).enabled && icon.sprite==art.ItemSprite(item) && icon.sprite!=plantingSprite,"Tool did not replace planting icon: "+name);
            if(name=="Hoe")
            {
                Check(farm.UseTool(target),"Hoe fixture failed");
                Query(farm,"UpdateFarmingIcon",(Vector2)target,false);
                Check(icon.enabled && icon.sprite==plantingSprite && Border(farm).enabled,"Hoe did not fall back to planting after tilling");
                Check(farm.FarmAction(target) && farm.InventoryItem(farm.InventoryCount-1)!=item,"E could not plant the indicated hoe");
                Query(farm,"UpdateFarmingIcon",(Vector2)target,false);
                Check(!icon.enabled && !Border(farm).enabled,"Occupied cell retained invalid preview");
                farm.AddItem(item);Check(farm.UseTool(target),"Could not remove planted tool with hoe");
            }
            farm.RemoveInventoryItem(farm.InventoryCount-1);
        }
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);farm.AddItem(water);
        Query(farm,"UpdateFarmingIcon",new Vector2(0,-4),false);
        Check(icon.enabled && icon.sprite==art.ItemSprite(water) && Border(farm).enabled,"Water action did not take priority over possible planting");
    }
    private static SpriteRenderer Border(FarmGame farm) => (SpriteRenderer)typeof(FarmGame).GetField("farmingBorder",Members).GetValue(farm);
    private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static object Query(object owner,string method,params object[] args) => owner.GetType().GetMethod(method,Members).Invoke(owner,args);
    private static object Read(object owner,string property) => owner.GetType().GetProperty(property,Members).GetValue(owner);
    private static bool Allowed(object hint) => (bool)Read(hint,"Allowed");
    private static string Text(object hint) => (string)Read(hint,"Text");
    private static void Check(bool ok,string error) { if(!ok) throw new Exception(error); }
    private static void Guard()
    {
        if(!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Run in an isolated temporary project.");
    }
    public static void Run()
    {
        Guard();
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); farm.Initialize(false);
        Check(farm.IsInitialized,farm.InitializationError);
        var rules=Read(farm,"Farming");
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");
        var tomato=Resources.Load<PlantDefinition>("Crops/TomatoSeeds");
        var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
        var water=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/WateringCan.asset");
        Vector2Int cell=default; bool found=false;
        for(int y=-12;y<=12 && !found;y++) for(int x=-20;x<=20 && !found;x++)
        {
            var candidate=new Vector2Int(x,y);
            if(Allowed(Query(rules,"TillHint",candidate))) { cell=candidate; found=true; }
        }
        Check(found,"No empty farmable test cell");
        farm.TeleportPlayer(cell);
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        farm.AddItem(hoe);
        Check(!Allowed(Query(rules,"PlantHint",cell)) && !farm.PlantSelected(cell),"Untilled soil accepted planting");
        Check(Allowed(Query(rules,"ToolHint",cell,GrowthEffect.Dig)) && farm.UseTool(cell),"Hoe preview and execution disagree");
        Check(!Allowed(Query(rules,"ToolHint",cell,GrowthEffect.Dig)) && !farm.UseTool(cell),"Already tilled preview and execution disagree");
        farm.RemoveInventoryItem(0);
        Check(!Allowed(Query(rules,"PlantHint",cell)) && !farm.PlantSelected(cell),"Empty hands accepted planting");
        var transformed=UnityEngine.Object.Instantiate(hoe);transformed.transformed=true;
        var broken=UnityEngine.Object.Instantiate(seed);broken.harvestResult=null;
        try
        {
            farm.AddItem(transformed); int before=farm.InventoryCount;
            Check(!Allowed(Query(rules,"PlantHint",cell)) && !farm.PlantSelected(cell) && farm.InventoryCount==before,"Transformed item preview or consumption incorrect");
            farm.RemoveInventoryItem(0); farm.AddItem(broken);
            Check(!Allowed(Query(rules,"PlantHint",cell)) && !farm.PlantSelected(cell),"Unconfigured seed accepted");
            farm.RemoveInventoryItem(0); farm.AddItem(seed);
            Check(Allowed(Query(rules,"PlantHint",cell)) && farm.PlantSelected(cell),"Seed preview and planting disagree");
            Check(!Allowed(Query(rules,"PlantHint",cell)),"Occupied plot accepted another plant");
            Check(Text(Query(rules,"InteractionHint",cell)).Contains("Water") && !farm.InteractPlot(cell),"Dry crop state incorrect");
            farm.AddItem(water);
            Check(Allowed(Query(rules,"ToolHint",cell,GrowthEffect.Water)) && farm.UseTool(cell),"Water preview and execution disagree");
            farm.Tick(seed.growthSeconds*.5f);
            Check(Text(Query(rules,"InteractionHint",cell)).Contains("growing") && !farm.InteractPlot(cell),"Growing crop state incorrect");
            farm.Tick(seed.growthSeconds);
            Check(Allowed(Query(rules,"InteractionHint",cell)),"Ripe crop not harvestable");
            // Full inventory still harvests successfully, dropping instead of rejecting.
            while(farm.AddItem(hoe)) {}
            int pieces=farm.WorldPieceCount;
            Check(farm.InteractPlot(cell) && farm.WorldPieceCount==pieces+1,"Full inventory harvest did not drop");
            while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
            farm.AddItem(seed.harvestResult);
            Check(!Allowed(Query(rules,"PlantHint",cell)) && !farm.PlantSelected(cell),"Harvested vegetable showed plantable");
            farm.RemoveInventoryItem(0);farm.AddItem(tomato);farm.PlantSelected(cell);farm.Water(cell,.1f);farm.Tick(tomato.growthSeconds);
            Check(farm.InteractPlot(cell) && Text(Query(rules,"InteractionHint",cell)).Contains("Water"),"Regrowth did not return to needs-water state");
            farm.Water(cell,.1f);farm.Tick(tomato.regrowSeconds);
            Check(Allowed(Query(rules,"InteractionHint",cell)),"Regrown crop not harvestable");
            farm.TeleportPlayer((Vector2)cell+Vector2.right*4);
            Check(!Allowed(Query(rules,"InteractionHint",cell)) && !farm.InteractPlot(cell),"Out-of-reach harvest allowed");
            Check(!Allowed(Query(farm,"PrimaryFarmHint",(Vector2)cell)),"Out-of-reach tool preview allowed");
            farm.TeleportPlayer(cell);farm.Uproot(cell);
            // Mature utility plants are harvestable; producers wait for their output.
            while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
            var stone=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Stone.asset");
            farm.AddItem(stone);farm.PlantSelected(cell);farm.Water(cell,.1f);farm.Tick(stone.growthSeconds);
            Check(!Allowed(Query(rules,"InteractionHint",cell)) && !farm.InteractPlot(cell),"Unready producer showed harvestable");
            farm.Tick(stone.productionSeconds);
            Check(Allowed(Query(rules,"InteractionHint",cell)) && farm.InteractPlot(cell),"Ready producer not collectible");
            Check(!Allowed(Query(rules,"InteractionHint",cell)),"Collected producer remained ready");
            farm.Uproot(cell);farm.AddItem(hoe);farm.PlantSelected(cell);farm.Water(cell,.1f);farm.Tick(hoe.growthSeconds);
            Check(Allowed(Query(rules,"InteractionHint",cell)) && farm.InteractPlot(cell),"Mature utility plant cannot be harvested");
        }
        finally { UnityEngine.Object.DestroyImmediate(transformed); UnityEngine.Object.DestroyImmediate(broken); }
        CheckPlantedRemoval(farm,cell+Vector2Int.right,hoe,seed);
        Debug.Log("FARMING_FEEDBACK_SUCCESS: shared planting/tool/harvest decisions, invalid items, dry/growing/ready/regrowth/producer states, distance and full-inventory harvest.");
    }
    private static void CheckPlantedRemoval(FarmGame farm,Vector2Int cell,PlantDefinition hoe,PlantDefinition seed)
    {
        farm.TeleportPlayer(cell);farm.Till(cell);
        foreach(var item in new[]{hoe,seed}) foreach(bool mature in new[]{false,true})
        {
            while(farm.InventoryCount>0)farm.RemoveInventoryItem(0);
            Check(farm.AddItem(item) && farm.PlantSelected(cell),"Removal fixture planting failed");
            if(mature) { farm.Water(cell,.1f);farm.Tick(item.growthSeconds);Check(farm.IsMature(cell),"Removal fixture did not mature"); }
            else Check(!farm.FarmAction(cell) && !farm.Uproot(cell) && farm.InventoryCount==0,"Immature planted object was recovered");
            farm.AddItem(hoe);int count=farm.InventoryCount,pieces=farm.WorldPieceCount;
            Check(farm.UsePrimaryTool(cell,true),"Hoe could not remove planted item/crop");
            Check(farm.InventoryCount==count && farm.WorldPieceCount==pieces && !farm.IsMature(cell) && farm.CropProgress(cell)==0,"Hoe refunded or failed to reset plant");
            Check(!farm.Till(cell) && farm.PlantSelected(cell),"Removal lost tilled soil or prevented replanting");
            farm.AddItem(hoe);Check(farm.UsePrimaryTool(cell,true),"Cleanup hoe failed");
        }
        while(farm.InventoryCount>0)farm.RemoveInventoryItem(0);
        var storage=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/InventorySlot.asset");
        farm.AddItem(storage);farm.PlantSelected(cell);farm.Water(cell,.1f);farm.Tick(storage.growthSeconds);
        while(farm.AddItem(hoe)) {}
        int before=farm.WorldPieceCount,excess=farm.InventoryCount-5;
        Check(excess>0 && farm.UsePrimaryTool(cell,true),"Storage plant removal failed");
        Check(farm.Capacity==5 && farm.InventoryCount==5 && farm.WorldPieceCount==before+excess,"Storage removal lost carried items or refunded planted slot");
        Debug.Log("PLANTED_REMOVAL_SUCCESS: E cannot recover immature items/seeds; hoe removes immature/mature items/crops without refunds, preserves soil/replanting and safely drops storage overflow.");
    }

}

// Uses the production HUD unchanged while the test freezes input/growth.
public sealed class FarmingFeedbackGuiProbe : MonoBehaviour
{
    public FarmGame farm;
    private void OnGUI()
    {
        if(farm==null) return;
        var hud=typeof(FarmGame).GetField("hud",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(farm);
        hud.GetType().GetMethod("Draw").Invoke(hud,null);
    }
}
