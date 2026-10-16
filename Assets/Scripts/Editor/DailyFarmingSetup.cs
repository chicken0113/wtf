using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class DailyFarmingSetup
{
    [MenuItem("Tools/Everything Grows/Set up crop farming")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var farm=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmGame>()).FirstOrDefault();
        var manager=farm!=null?farm.GetComponent<NpcManager>():null; if(manager==null) throw new Exception("Open the farm with a merchant.");
        Undo.RecordObject(manager,"Add seeds to shop");
        string[] names={"ParsnipSeeds","PotatoSeeds","CauliflowerSeeds","TomatoSeeds","Scythe"}; int[] prices={20,50,80,50,25};
        var profile=manager.npcs.Find(entry=>entry.id=="farm_merchant"); if(profile==null) throw new Exception("Farm merchant profile missing.");
        for(int i=0;i<names.Length;i++)
        {
            var item=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Resources/Crops/"+names[i]+".asset");
            if(item==null) throw new Exception("Missing crop data: "+names[i]);
            if(!profile.offers.Any(offer=>offer.item==item)) profile.offers.Add(new ShopOffer {item=item,buyPrice=prices[i],sellPrice=-1});
        }
        var icon=EditableMapBuilder.PreviewIcon(22);
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Map/HoePickup.prefab");
        root.GetComponent<SpriteRenderer>().sprite=icon; PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Map/HoePickup.prefab"); PrefabUtility.UnloadPrefabContents(root);
        foreach(var placement in farm.GetComponentsInChildren<FarmItemPlacement>(true)) if(placement.item!=null && placement.item.effect==GrowthEffect.Dig)
        { var renderer=placement.GetComponent<SpriteRenderer>(); if(renderer==null) continue; Undo.RecordObject(renderer,"Show hoe"); renderer.sprite=icon; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
        EditorUtility.SetDirty(manager); EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
        Debug.Log("DAILY_FARM_SETUP_SUCCESS");
    }
    public static void Validate()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated project.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity"); Apply();
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); farm.Initialize(false);
        void Check(bool ok,string reason) { if(!ok) throw new Exception(reason); }
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds"); var tomato=Resources.Load<PlantDefinition>("Crops/TomatoSeeds");
        farm.MovePlayer(new Vector2(0,-2));
        Check(farm.AddItem(seed) && farm.InteractPlot(new Vector2Int(0,-2)),"Seed planting");
        farm.Tick(seed.growthSeconds*2); Check(farm.CropProgress(new Vector2Int(0,-2))==0,"Dry crop grew");
        farm.Water(new Vector2(0,-2),.1f); farm.Tick(seed.growthSeconds*.5f);
        Check(Mathf.Approximately(farm.CropProgress(new Vector2Int(0,-2)),.5f),"Timed crop progress incorrect");
        farm.Tick(seed.growthSeconds*.5f);
        Check(farm.IsMature(new Vector2Int(0,-2)) && farm.InteractPlot(new Vector2Int(0,-2)) && farm.SelectedItem==seed.harvestResult,"Timed harvest failed");
        Check(!farm.InteractPlot(new Vector2Int(0,-2)),"Vegetable replanted as seed");
        Check(farm.AddItem(tomato),"Tomato seed unavailable");
        farm.Water(new Vector2(0,-2),.1f); Check(farm.InteractPlot(new Vector2Int(0,-2)),"Pre-watered planting failed");
        farm.Tick(tomato.growthSeconds);
        Check(farm.InteractPlot(new Vector2Int(0,-2)) && !farm.IsMature(new Vector2Int(0,-2)),"Regrow harvest removed crop");
        farm.Tick(tomato.regrowSeconds*2); Check(farm.CropProgress(new Vector2Int(0,-2))==0,"Regrowth did not require water");
        farm.Water(new Vector2(0,-2),.1f); farm.Tick(tomato.regrowSeconds*.5f);
        Check(Mathf.Approximately(farm.CropProgress(new Vector2Int(0,-2)),.5f),"Regrowth seconds ignored");
        farm.Tick(tomato.regrowSeconds*.5f); Check(farm.IsMature(new Vector2Int(0,-2)),"Regrowth did not finish");
        farm.Tick(10000); Check(farm.IsMature(new Vector2Int(0,-2)),"Mature crop did not remain harvestable");
        Check(farm.Uproot(new Vector2Int(0,-2)),"Crop recovery failed");
        farm.AddItem(AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset"));
        Check(farm.UseTool(new Vector2Int(5,-2)),"Hoe failed");
        Check(farm.Shop.npcs[0].offers.Any(offer=>offer.item==seed),"Seeds missing from merchant");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        Check(!farm.UseTool(new Vector2Int(5,-1)),"Bare hands can till soil");
        Check(farm.AddItem(seed) && farm.AddItem(seed) && farm.InventoryCount==1 && farm.InventoryQuantity(0)==2,"Seeds do not stack");
        Check(farm.InteractPlot(new Vector2Int(1,-2)) && farm.InventoryQuantity(0)==1,"Planting consumed entire stack");
        var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset"); while(farm.AddItem(hoe)) {}
        farm.MovePlayer(new Vector2(.5f,1));
        var npc=farm.GetComponentInChildren<ShopNpc>(); Check(farm.Shop.Open(npc),"Stack trade shop unavailable");
        int seedIndex=farm.Shop.npcs[0].offers.FindIndex(offer=>offer.item==seed); int gold=farm.Shop.Gold;
        Check(farm.Shop.Buy(seedIndex) && farm.InventoryQuantity(0)==2 && farm.Shop.Gold==gold-20,"Full inventory cannot buy into an existing stack");
        Check(farm.Shop.Sell(0) && farm.InventoryQuantity(0)==1 && farm.Shop.Gold==gold-15,"Selling removed entire stack or paid incorrectly");
        Debug.Log("DAILY_FARM_VALIDATION_SUCCESS: seeds, water, timed growth, harvest, timed regrowth, hoe and stacks.");
    }
}
[InitializeOnLoad]
public static class DailyFarmingRequest
{
    static DailyFarmingRequest() { if(!Application.isBatchMode) EditorApplication.update+=Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !System.IO.File.Exists("Library/DailyFarm.apply")) return;
        System.IO.File.Delete("Library/DailyFarm.apply");
        try { DailyFarmingSetup.Apply(); System.IO.File.WriteAllText("Library/DailyFarm.result","SUCCESS"); }
        catch(Exception error) { Debug.LogException(error); System.IO.File.WriteAllText("Library/DailyFarm.result",error.ToString()); }
    }
}
