using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class GrowthCategoriesCheck
{
    private static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    private static SaveItemCatalog Catalog=>Resources.Load<SaveItemCatalog>("SaveItemCatalog");
    private static FarmGame Reload()
    {
        GameSession.ResetForTests();EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");
        var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();f.Initialize(false);GameSession.Bind(f);
        Check(f.IsInitialized && string.IsNullOrEmpty(GameSession.LastError),f.InitializationError+GameSession.LastError);return f;
    }
    private static PlotSave Plot(FarmGame f,Vector2Int cell)=>f.CaptureMap(Catalog,0).plots.Single(p=>p.cell==cell);
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) ||
            !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new Exception("Isolated project/save only.");
        FarmingFeedbackCheck.Run(); // Existing tools, storage, crops, producer and regrowth remain unchanged.
        var f=Reload();InventoryCheckAccess.Clear(f);
        var sword=Resources.Load<PlantDefinition>("Equipment/Sword");var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
        var bow=Resources.Load<PlantDefinition>("Equipment/Bow");var crop=Resources.Load<PlantDefinition>("Crops/Parsnip");
        Vector2Int cell=default;bool found=false;
        for(int y=-8;y<=8 && !found;y++)for(int x=-8;x<=8 && !found;x++)
            if(f.Till(new Vector2Int(x,y)) && f.Till(new Vector2Int(x+1,y))) { cell=new Vector2Int(x,y);found=true; }
        Check(found,"No adjacent empty soil");f.TeleportPlayer(cell);
        sword.harvestQuantity=3;f.AddItem(sword);Check(f.PlantSelected(cell),"Harvest fixture failed");f.Water(cell,.1f);f.Tick(sword.growthSeconds);
        while(f.AddItem(hoe)) {} int drops=f.WorldPieceCount,xp=f.CapturePlayer(Catalog).farmingExperience;
        Check(f.InteractPlot(cell) && f.WorldPieceCount==drops+3 && string.IsNullOrEmpty(Plot(f,cell).item),"Harvest quantity/drop/removal failed");
        Check(f.CapturePlayer(Catalog).farmingExperience==xp+sword.harvestExperience && !f.Uproot(cell),"Quantity multiplied XP or duplicate harvest");
        InventoryCheckAccess.Clear(f);
        bow.growthType=PlantGrowthType.Producer;bow.harvestResult=crop;bow.harvestQuantity=2;bow.productionSeconds=4;bow.regrowSeconds=0;
        EditorUtility.SetDirty(bow);AssetDatabase.SaveAssetIfDirty(bow);
        f.AddItem(bow);Check(f.PlantSelected(cell),"Producer fixture failed");f.Water(cell,.1f);f.Tick(bow.growthSeconds);
        Check(!f.Collect(cell),"Producer ready before interval");f.Tick(1.5f);Check(GameSession.Save(f),"Producer save failed");f=Reload();
        Check(Mathf.Abs(Plot(f,cell).production-1.5f)<.001f,"Production timer lost on reload");f.Tick(2.5f);
        Check(f.Collect(cell) && f.InventoryQuantity(0)==2 && !string.IsNullOrEmpty(Plot(f,cell).item),"Producer output/retention failed");
        Check(!f.Collect(cell),"Producer duplicate harvest");f.Tick(4);Check(f.Collect(cell) && f.InventoryQuantity(0)==4,"Repeat production failed");
        Check(f.Uproot(cell),"Producer recovery failed");InventoryCheckAccess.Clear(f);
        sword=Resources.Load<PlantDefinition>("Equipment/Sword");sword.growthType=PlantGrowthType.Functional;sword.growthFunction=PlantFunction.Water;
        sword.harvestResult=null;sword.effectRadius=1.1f;sword.functionSeconds=2;
        EditorUtility.SetDirty(sword);AssetDatabase.SaveAssetIfDirty(sword);
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");var neighbour=cell+Vector2Int.right;
        f.AddItem(seed);Check(f.PlantSelected(neighbour),"Dry neighbour fixture failed");
        f.AddItem(sword);Check(f.PlantSelected(cell),"Functional fixture failed");f.Water(cell,.1f);f.Tick(sword.growthSeconds);
        Check(sword.effect==GrowthEffect.Weapon && !Plot(f,neighbour).wet,"Held weapon use coupled to planted water or interval fired early");
        f.Tick(1);Check(!Plot(f,neighbour).wet && GameSession.Save(f),"Function early tick/save failed");f=Reload();f.Tick(1);
        Check(Plot(f,neighbour).wet,"Functional timer/effect failed after restore");
        Check(f.InteractPlot(cell) && string.IsNullOrEmpty(Plot(f,cell).item),"Functional recovery failed");
        var invalid=UnityEngine.Object.Instantiate(sword);invalid.harvestQuantity=0;invalid.functionSeconds=-1;
        var report=new SetupValidationReport();SetupDataValidator.CheckItem(invalid,null,report,new System.Collections.Generic.HashSet<PlantDefinition>(),false);
        Check(report.HasErrors,"Invalid growth configuration accepted");UnityEngine.Object.DestroyImmediate(invalid);
        Debug.Log("GROWTH_CATEGORIES_SUCCESS: existing farming/storage/regrowth, harvest quantity/full inventory/single XP, independent held/plant roles, producer retention/interval/repeat/reload, function cadence/reload/recovery and invalid settings.");
    }
}
