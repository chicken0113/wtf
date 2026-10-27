using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class FarmingProgressionCheck
{
    private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private static void Check(bool value,string text) { if(!value)throw new Exception(text); }
    private static object Call(object instance,string method,params object[] args) => instance.GetType().GetMethod(method,Members).Invoke(instance,args);
    private static int Total(FarmGame f)=>f.CapturePlayer(Resources.Load<SaveItemCatalog>("SaveItemCatalog")).farmingExperience;
    private static FarmGame Reload()
    {
        GameSession.ResetForTests();EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");
        var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();f.Initialize(false);GameSession.Bind(f);
        Check(f.IsInitialized && string.IsNullOrEmpty(GameSession.LastError),"Initialize/restore: "+GameSession.LastError);return f;
    }
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) ||
            !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))
            throw new Exception("Isolated project and saves only.");
        var levels=Resources.Load<FarmingLevelSettings>("FarmingLevels");levels.experienceToNextLevel=new[]{10,20,30};
        EditorUtility.SetDirty(levels);AssetDatabase.SaveAssetIfDirty(levels);
        var f=Reload();var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");
        Check(f.FarmingLevel==1 && Total(f)==0,"New save progression");
        InventoryCheckAccess.Clear(f);
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");seed.requiredFarmingLevel=2;seed.harvestExperience=10;
        f.AddItem(seed);var cell=Vector2Int.zero;bool found=false;
        for(int y=-8;y<=8 && !found;y++)for(int x=-8;x<=8;x++)if(f.Till(new Vector2Int(x,y))) { cell=new Vector2Int(x,y);found=true;break; }
        Check(found,"No free soil");f.TeleportPlayer(cell);
        object rules=typeof(FarmGame).GetProperty("Farming",Members).GetValue(f);
        var hint=Call(rules,"PlantHint",cell);
        Check(!(bool)hint.GetType().GetProperty("Allowed").GetValue(hint) && !f.PlantSelected(cell) && f.InventoryQuantity(0)==1,"Locked planting consumed item or preview disagreed");
        Check(Total(f)==0 && f.CanAddItem(seed),"Pickup restricted or awarded XP");
        seed.requiredFarmingLevel=1;Check(f.PlantSelected(cell),"Level 1 planting failed");
        Check(!f.Uproot(cell) && Total(f)==0,"Immature recovery/XP");
        f.Water(cell,.1f);f.Tick(seed.growthSeconds);while(f.AddItem(Resources.Load<PlantDefinition>("Equipment/Sword"))) {}
        int drops=f.WorldPieceCount;
        Check(f.InteractPlot(cell) && f.WorldPieceCount==drops+1 && Total(f)==10 && f.FarmingLevel==2,"Full-inventory harvest XP/level failed");
        Check(!f.Uproot(cell) && Total(f)==10,"Repeated empty harvest awarded XP");
        InventoryCheckAccess.Clear(f);seed.requiredFarmingLevel=2;f.AddItem(seed);
        Check(f.PlantSelected(cell),"Level unlock did not apply");
        Check((bool)Call(rules,"UseHoe",cell) && Total(f)==10,"Hoe removal awarded XP");
        f.AddItem(seed);Check(f.PlantSelected(cell),"Replant failed");f.Water(cell,.1f);f.Tick(seed.growthSeconds);f.InteractPlot(cell);
        Check(Total(f)==20 && f.FarmingLevel==2 && f.FarmingExperience==10,"Within-level XP wrong");
        InventoryCheckAccess.Clear(f);var tomato=Resources.Load<PlantDefinition>("Crops/TomatoSeeds");tomato.harvestExperience=10;
        f.AddItem(tomato);Check(f.PlantSelected(cell),"Tomato planting");f.Water(cell,.1f);f.Tick(tomato.growthSeconds);f.InteractPlot(cell);
        Check(Total(f)==30 && f.FarmingLevel==3,"First regrow harvest XP");
        Check(!f.InteractPlot(cell) && Total(f)==30,"Regrowth awarded XP before ready");
        f.Water(cell,.1f);f.Tick(tomato.regrowSeconds);f.InteractPlot(cell);Check(Total(f)==40,"Repeat harvest XP");
        Check(GameSession.Save(f),"Progress save failed");f=Reload();Check(Total(f)==40 && f.FarmingLevel==3 && f.FarmingExperience==10,"Reload lost XP/level");
        var state=f.CapturePlayer(catalog);state.farmingExperience=int.MaxValue;f.RestorePlayer(state,catalog);
        Check(f.FarmingLevel==4 && f.FarmingExperienceToNextLevel==0,"Max level handling");
        var progress=typeof(FarmGame).GetField("progression",Members).GetValue(f);Call(progress,"Add",int.MaxValue);
        Check(Total(f)==int.MaxValue,"XP overflow");
        state.farmingExperience=-1;bool rejected=false;try { f.RestorePlayer(state,catalog); }catch(InvalidOperationException) { rejected=true; }
        Check(rejected && Total(f)==int.MaxValue,"Negative XP mutated live player");
        var legacy=JsonUtility.FromJson<PlayerSave>("{\"inventory\":[],\"health\":1}");f.RestorePlayer(legacy,catalog);
        Check(f.FarmingLevel==1 && Total(f)==0,"Legacy missing XP default");
        var report=new SetupValidationReport();levels.experienceToNextLevel=new[]{0};var setup=GameSetupResources.Load();setup.FarmingLevels=levels;SetupDataValidator.CheckResources(setup,report,false,false);
        Check(report.HasErrors,"Invalid thresholds accepted");
        Debug.Log("FARMING_PROGRESSION_SUCCESS: shared level-gated planting, no pickup/removal/immature XP, full-inventory harvest, regrowth, threshold carry, save/reload, legacy migration, max/overflow and invalid settings.");
    }
}
