using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class HousePlantingCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated validation.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); farm.Initialize(false);
        void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
        var original=farm.GetComponentInChildren<FarmBuilding>();
        var originalVisual=original.GetComponentInChildren<SpriteRenderer>(); float originalWidth=originalVisual.bounds.size.x;
        var point=new Vector2(originalVisual.bounds.center.x,originalVisual.bounds.min.y+.1f);
        farm.MovePlayer(point-Vector2.up*.6f);
        var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
        while(farm.AddItem(hoe)) {}
        int pieces=farm.WorldPieceCount;
        Check(farm.HarvestBuildingAt(point) && (original==null || !original.gameObject.activeInHierarchy) && farm.WorldPieceCount==pieces+1,"Full inventory house harvest did not drop");
        Check(farm.GetComponentsInChildren<FarmBuilding>().Length==0,"Harvest auto-installed a house");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        Check(farm.DetachAt(farm.PlayerPosition+Vector2.down*.65f),"Dropped house cannot be picked up");
        var house=Resources.Load<PlantDefinition>("Buildings/House"); var upgraded=house.harvestResult;
        Check(farm.SelectedItem==house && upgraded!=null,"House upgrade data missing");
        var cell=new Vector2Int(1,-2); farm.MovePlayer(new Vector2(1,-4));
        Check(farm.PlantSelected(cell),"House cannot be planted");
        var visual=farm.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Growing House");
        Check(visual.sprite.texture==house.itemSprite.texture && visual.sprite.rect.height<house.itemSprite.rect.height && visual.bounds.size.x<=1,"Buried house appearance incorrect");
        Snapshot("HousePlanted.png");
        farm.Tick(house.growthSeconds); Check(!farm.IsMature(cell),"Dry house grew");
        farm.Water(cell,.1f); farm.Tick(house.growthSeconds);
        Check(farm.IsMature(cell) && visual.bounds.size.x<=1 && farm.Capacity==5 && visual.GetComponent<FarmBuilding>()==null,"Growth automatically installed house");
        Check(farm.InteractPlot(cell) && farm.SelectedItem==upgraded && farm.GetComponentsInChildren<FarmBuilding>().Length==0,"Upgraded house did not enter inventory");
        Check(farm.PlaceHouse(cell) && farm.InventoryCount==0 && farm.Capacity==5,"Manual house installation failed");
        var building=farm.GetComponentInChildren<FarmBuilding>();
        Check(building!=null && building.HarvestItem==upgraded && building.GetComponent<SpriteRenderer>().bounds.size.x>originalWidth,"Installed upgrade missing");
        Snapshot("HouseUpgraded.png");
        while(farm.AddItem(hoe)) {}
        pieces=farm.WorldPieceCount;
        Check(farm.HarvestBuildingAt(cell) && farm.Capacity==5 && farm.InventoryCount==5 && farm.WorldPieceCount==pieces+1,"Full inventory house harvest lost items or changed capacity");
        Check(farm.GetComponentsInChildren<FarmBuilding>().Length==0,"Harvested house remained installed");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        farm.AddItem(upgraded); farm.MovePlayer(new Vector2(7,-5));
        Check(farm.PlaceHouse(new Vector2(7,-3)),"House cannot be installed elsewhere");
        farm.AddItem(house); int carried=farm.InventoryCount;
        Check(!farm.PlaceHouse(new Vector2(7,-3)) && farm.InventoryCount==carried,"Overlapping placement consumed house");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        farm.MovePlayer(new Vector2(1,-4));
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds"); farm.AddItem(seed); farm.PlantSelected(cell);
        while(farm.AddItem(hoe)) {}
        farm.Water(cell,.1f); farm.Tick(seed.growthSeconds); pieces=farm.WorldPieceCount;
        Check(farm.InteractPlot(cell) && !farm.IsMature(cell) && farm.WorldPieceCount==pieces+1,"Full inventory crop did not drop");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        var tomato=Resources.Load<PlantDefinition>("Crops/TomatoSeeds"); farm.AddItem(tomato); farm.PlantSelected(cell);
        while(farm.AddItem(hoe)) {}
        farm.Water(cell,.1f); farm.Tick(tomato.growthSeconds); pieces=farm.WorldPieceCount;
        Check(farm.InteractPlot(cell) && !farm.IsMature(cell) && farm.WorldPieceCount==pieces+1,"Full inventory regrowing crop did not drop");
        farm.Water(cell,.1f); farm.Tick(tomato.regrowSeconds); Check(farm.IsMature(cell),"Dropped harvest broke regrowth");
        Debug.Log("HOUSE_PLANTING_SUCCESS: inventory/drop harvests, compact buried house, no automatic installation, manual placement, overlap protection, unchanged inventory capacity and crop regrowth.");
    }
    public static void Regression()
    {
        DailyFarmingSetup.Validate();
        HeartHarvestCheck.Run();
    }
    static void usingArt(SpriteRenderer visual,PlantDefinition item)
    {
        var art=new FarmArt();
        if(visual.sprite.texture!=art.ItemSprite(item).texture)
        {
            // Procedural textures are owned per FarmArt; compare the source dimensions and crop instead.
            if(visual.sprite.rect.width!=art.ItemSprite(item).rect.width || visual.sprite.rect.height>=art.ItemSprite(item).rect.height) throw new Exception("Common sprout still used");
        }
        art.Dispose();
    }
    static void Snapshot(string path)
    {
        var camera=Camera.main; camera.transform.position=new Vector3(0,0,-10);
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes(path,image.EncodeToPNG()); camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}
