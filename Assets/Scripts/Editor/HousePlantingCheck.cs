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
        Check(!farm.HarvestBuildingAt(point) && original.gameObject.activeInHierarchy,"Full inventory destroyed house");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        Check(farm.HarvestBuildingAt(point) && (original==null || !original.gameObject.activeInHierarchy),"Original house did not detach");
        var house=Resources.Load<PlantDefinition>("Buildings/House"); var upgraded=house.harvestResult;
        Check(farm.SelectedItem==house && upgraded!=null,"House upgrade data missing");
        var cell=new Vector2Int(1,-2); farm.MovePlayer(new Vector2(1,-4));
        Check(farm.PlantSelected(cell),"House cannot be planted");
        var visual=farm.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Growing House");
        Check(visual.sprite.texture==house.itemSprite.texture && visual.sprite.rect.height<house.itemSprite.rect.height,"Buried house appearance is wrong");
        Check(Mathf.Max(visual.bounds.size.x,visual.bounds.size.y)<=1,"Planted house too large");
        Snapshot("HousePlanted.png");
        farm.Tick(house.growthSeconds); Check(!farm.IsMature(cell),"Dry house grew");
        farm.Water(cell,.1f); farm.Tick(house.growthSeconds);
        Check(farm.IsMature(cell) && visual.bounds.size.x>originalWidth && farm.Capacity==10,"Larger house or storage upgrade missing");
        var building=visual.GetComponent<FarmBuilding>();
        Check(building!=null && building.HarvestItem==upgraded && building.Blocks(building.transform.TransformPoint(building.offset)),"Grown house has no footprint");
        Snapshot("HouseUpgraded.png");
        while(farm.AddItem(hoe)) {}
        Check(!farm.Uproot(cell) && farm.Capacity==10,"Harvest silently removed occupied storage slots");
        while(farm.InventoryCount>4) farm.RemoveInventoryItem(0);
        Check(farm.Uproot(cell) && farm.SelectedItem==upgraded && farm.Capacity==5,"House harvesting did not remove its storage function");
        Check(farm.PlantSelected(cell),"Upgraded house cannot be relocated"); farm.Tick(upgraded.growthSeconds);
        Check(farm.IsMature(cell) && farm.Capacity==10,"Relocated house lost upgrade");
        var hoeCell=new Vector2Int(-1,-3); farm.AddItem(hoe); Check(farm.PlantSelected(hoeCell),"Hoe planting failed");
        var hoeVisual=farm.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Growing Hoe");
        usingArt(hoeVisual,hoe);
        Debug.Log("HOUSE_PLANTING_SUCCESS: detach, original removal, compact buried items, timed upgrade, storage, capacity protection and relocation.");
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
