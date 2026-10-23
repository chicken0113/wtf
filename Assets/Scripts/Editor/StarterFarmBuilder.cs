using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class StarterFarmBuilder
{
    const string HousePath="Assets/Prefabs/Map/Farmhouse.prefab",MerchantPath="Assets/Prefabs/Map/Merchant.prefab";
    // One-time authoring entry retained for fixtures; no production menu.
    public static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play to arrange the farm.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var farm=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmGame>()).FirstOrDefault();
        if(farm==null || farm.Layout==null) throw new Exception("Open an editable farm scene.");
        var map=farm.Layout;
        if(map.transform.Find("StarterFarmSetup")!=null) { Debug.Log("STARTER_FARM_ALREADY_EXISTS"); return; }
        AssetDatabase.ImportAsset("Assets/Resources/FarmHouse.png",ImportAssetOptions.ForceUpdate);
        var housePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(HousePath);
        if(housePrefab==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/FarmHouse.png"); if(source==null) throw new Exception("House sprite missing.");
            var root=new GameObject("Farmhouse"); root.AddComponent<FarmBuilding>();
            var visual=new GameObject("Visual"); visual.transform.SetParent(root.transform,false); visual.transform.localScale=Vector3.one*(5/source.bounds.size.x);
            var renderer=visual.AddComponent<SpriteRenderer>(); renderer.sprite=source; renderer.sortingOrder=10;
            housePrefab=PrefabUtility.SaveAsPrefabAsset(root,HousePath); UnityEngine.Object.DestroyImmediate(root);
        }
        var merchantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(MerchantPath);
        if(merchantPrefab==null)
        {
            var root=new GameObject("Merchant"); root.AddComponent<ShopNpc>();
            var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=EditableMapBuilder.PreviewIcon(0); renderer.color=new Color(.7f,.85f,1); renderer.sortingOrder=10;
            merchantPrefab=PrefabUtility.SaveAsPrefabAsset(root,MerchantPath); UnityEngine.Object.DestroyImmediate(root);
        }
        Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Set up starter farm and merchant");
        var marker=new GameObject("StarterFarmSetup"); marker.transform.SetParent(map.transform,false); Undo.RegisterCreatedObjectUndo(marker,"Record starter farm setup");
        Undo.RecordObject(map,"Hide safe zone markers"); map.showSafeMarkers=false; EditorUtility.SetDirty(map);
        foreach(string name in new[]{"Paths","Items"})
        {
            var layer=map.transform.Find(name); if(layer==null) continue;
            foreach(Transform child in layer) { Undo.RecordObject(child.gameObject,"Hide initial test object"); child.gameObject.SetActive(false); }
        }
        MapPlacement.Place(map,housePrefab,new Vector2(-3.5f,2.5f),1,MapPlacement.Category.Decoration);
        MapPlacement.Place(map,merchantPrefab,new Vector2(.5f,2.5f),1.1f,MapPlacement.Category.Npcs);
        Undo.RecordObject(map.playerSpawn,"Move farm start"); map.playerSpawn.position=new Vector3(0,-.5f,0);
        var groundPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/GroundTile.prefab");
        var soil=EditableMapBuilder.PreviewIcon(10);
        var grounds=map.GetComponentsInChildren<FarmGround>();
        for(int x=-1;x<=3;x++) for(int y=-3;y<=-1;y++)
        {
            var tile=grounds.FirstOrDefault(item=>Vector2.Distance(item.transform.position,new Vector2(x,y))<.01f);
            if(tile==null) tile=MapPlacement.Place(map,groundPrefab,new Vector2(x,y),1,MapPlacement.Category.Ground).GetComponent<FarmGround>();
            if(tile.GetComponent<StartingPlot>()==null) Undo.AddComponent<StartingPlot>(tile.gameObject);
            var renderer=tile.GetComponent<SpriteRenderer>(); Undo.RecordObject(renderer,"Show starting field"); renderer.sprite=soil; renderer.color=Color.white;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        var manager=farm.GetComponent<NpcManager>(); if(manager==null) manager=Undo.AddComponent<NpcManager>(farm.gameObject);
        Undo.RecordObject(manager,"Configure merchant");
        if(!manager.npcs.Any(profile=>profile.id=="farm_merchant"))
        {
            var profile=new NpcProfile();
            string[] names={"Hoe","WateringCan","Stone","Cloud","InventorySlot","Sword","Bow"}; int[] prices={15,15,5,25,40,25,30};
            for(int i=0;i<names.Length;i++)
            {
                var item=AssetDatabase.LoadAssetAtPath<PlantDefinition>((i<5?"Assets/Settings/":"Assets/Resources/Equipment/")+names[i]+".asset");
                profile.offers.Add(new ShopOffer {item=item,buyPrice=prices[i],sellPrice=prices[i]/2});
            }
            manager.npcs.Add(profile);
        }
        EditorUtility.SetDirty(manager); EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(group); Selection.activeGameObject=manager.gameObject; Debug.Log("STARTER_FARM_SETUP_SUCCESS");
    }
    public static void Validate()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated batch project.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity"); Setup();
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); farm.Initialize(false); var shop=farm.Shop;
        var npc=farm.GetComponentInChildren<ShopNpc>();
        void Check(bool ok,string reason) { if(!ok) throw new Exception(reason); }
        Check(farm.GetComponentsInChildren<FarmBuilding>().Length==1 && npc!=null,"House / NPC missing");
        Check(farm.IsMature(new Vector2Int(0,-2))==false && !farm.Till(new Vector2Int(0,-2)),"Starting field not tilled");
        Check(farm.WorldPieceCount==0,"Hidden starter item markers spawned");
        Check(!shop.Open(npc),"Shop opened out of range"); farm.MovePlayer(new Vector2(.5f,1)); Check(shop.Open(npc),"NPC interaction failed");
        int gold=shop.Gold; Check(shop.Buy(0) && shop.Gold==gold-15 && farm.InventoryCount==1,"Purchase transaction");
        Check(shop.Sell(0) && shop.Gold==gold-8 && farm.InventoryCount==0,"Sale transaction");
        var weapon=Resources.Load<PlantDefinition>("Equipment/Gun"); weapon.sellPrice=70;
        farm.AddItem(weapon); gold=shop.Gold; Check(shop.Sell(0) && shop.Gold==gold+70,"Unlisted inventory item not sellable");
        var item=shop.npcs[0].offers[0].item;
        while(farm.AddItem(item)) {}
        gold=shop.Gold; Check(!shop.Buy(0) && shop.Gold==gold,"Full purchase charged money");
        InventoryCheckAccess.Clear(farm);
        shop.npcs[0].offers[0].buyPrice=shop.Gold+1;
        Check(!shop.Buy(0) && farm.InventoryCount==0 && shop.Gold==gold,"Unaffordable purchase succeeded");
        shop.npcs.Add(new NpcProfile {id="second",displayName="Second",offers=new(){new ShopOffer {item=item,buyPrice=3,sellPrice=2}}});
        npc.npcId="second"; Check(shop.Open(npc) && shop.Buy(0) && shop.Sell(0) && shop.Gold==gold-1,"Per-NPC prices ignored");
        Check(!farm.Till(new Vector2Int(-3,2)),"House footprint farmable");
        shop.Close(); farm.MovePlayer(new Vector2(0,-.5f));
        var camera=Camera.main; camera.transform.position=new Vector3(0,0,-10);
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes("StarterFarm.png",image.EncodeToPNG()); camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("STARTER_FARM_VALIDATION_SUCCESS: house, tilled field, NPC range, purchase, all-item sale, full inventory, insufficient funds, NPC-specific prices.");
    }
}

[InitializeOnLoad]
public static class StarterFarmRequest
{
    static StarterFarmRequest() { if(!Application.isBatchMode) EditorApplication.update+=Tick; }
    static void Tick()
    {
        const string request="Library/StarterFarm.apply";
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !System.IO.File.Exists(request)) return;
        System.IO.File.Delete(request);
        try { StarterFarmBuilder.Setup(); System.IO.File.WriteAllText("Library/StarterFarm.result","SUCCESS: "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path); }
        catch(Exception error) { Debug.LogException(error); System.IO.File.WriteAllText("Library/StarterFarm.result",error.ToString()); }
    }
}
