using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// Explicit one-time scene authoring. Never rebuilds the user's layout at startup.
[InitializeOnLoad]
public static class SingleWorldSetup
{
    public const string World="Assets/Scenes/OpenWorld/ForestClearing.unity";
    const string Marker="Single open world 2026-10-28",Request="Library/SingleWorld.request",Result="Library/SingleWorld.result";
    public const string Trader="world_camp_trader";
    static SingleWorldSetup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        try
        {
            if(!Apply()){File.WriteAllText(Result,"WAITING_FOR_WORLD_SCENE_SAVE");return;}
            File.Delete(Request);File.WriteAllText(Result,"SUCCESS");
        }
        catch(Exception e){File.Delete(Request);File.WriteAllText(Result,e.ToString());Debug.LogException(e);}
    }
    public static bool Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
        var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(World);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(!opened && scene.isDirty)return false;
        if(opened)scene=EditorSceneManager.OpenScene(World,OpenSceneMode.Additive);
        try
        {
            var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();var map=farm.Layout;
            if(map.transform.Find(Marker)!=null)return true;
            foreach(var exit in map.GetComponentsInChildren<MapExit>())exit.gameObject.SetActive(false);
            map.playerSpawn.position=new Vector2(-12,-10);
            map.passiveHealthRecovery=false; // Free camp has no legacy safe-area regeneration.
            var camp=Group(map.transform,"Camp - start and night trade");
            var manager=farm.GetComponent<NpcManager>();
            manager.npcs.Add(new NpcProfile {id=Trader,displayName="야영지 상인",greeting="밤에 들러. 씨앗과 장비를 사고 수확물을 팔 수 있어.",offers=new System.Collections.Generic.List<ShopOffer> {
                Offer("Crops/ParsnipSeeds",10),Offer("Equipment/Sword",25),Offer("Equipment/Bow",40),Offer("Equipment/CampingBed",20),Offer("Equipment/RespawnToken",20) }});
            var trader=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Merchant.prefab"),camp);
            trader.name="Fixed night merchant";trader.transform.position=new Vector2(-10,-8);trader.GetComponent<ShopNpc>().npcId=Trader;
            PrefabUtility.RecordPrefabInstancePropertyModifications(trader.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(trader.GetComponent<ShopNpc>());
            var label=Root("Night merchant sign",camp,new Vector2(-10,-6.5f)).AddComponent<WorldLabel>();label.text="밤에 상인이 찾아오는 야영지";
            Supply(farm,"Equipment/Sword",new Vector2(-13,-11));Supply(farm,"Equipment/CampingBed",new Vector2(-11,-11));
            ConfigureRegions();Root(Marker,map.transform,Vector2.zero);
            EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("단일 월드 씬 저장 실패");
            Debug.Log("SINGLE_WORLD_AUTHORED");return true;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);}
    }
    static ShopOffer Offer(string resource,int price)
    {
        var item=Resources.Load<PlantDefinition>(resource);if(item==null)throw new InvalidOperationException("Missing item: "+resource);
        return new ShopOffer {item=item,buyPrice=price};
    }
    static void Supply(FarmGame farm,string resource,Vector2 position)
    {
        var item=Resources.Load<PlantDefinition>(resource);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/"+item.name+"Pickup.prefab")??AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/StonePickup.prefab");
        var root=MapPlacement.Place(farm.Layout,prefab,position,.8f,MapPlacement.Category.Items);root.name="Starting supply - "+item.name;
        root.GetComponent<FarmItemPlacement>().item=item;PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponent<FarmItemPlacement>());
        var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=item.itemSprite!=null?item.itemSprite:EditableMapBuilder.PreviewIcon(new FarmArt().ItemIndex(item));PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
    static void ConfigureRegions()
    {
        var south=Resources.Load<SpawnRegion>("SpawnRegions/ForestClearing");
        south.label="월드 남쪽 들판";south.useMapBounds=false;south.area=new Rect(-26,-22,52,18);south.spawnInterval=10;south.maxAlive=2;EditorUtility.SetDirty(south);AssetDatabase.SaveAssetIfDirty(south);
        const string path="Assets/Resources/SpawnRegions/WorldNorth.asset";
        var north=AssetDatabase.LoadAssetAtPath<SpawnRegion>(path);
        if(north==null){north=ScriptableObject.CreateInstance<SpawnRegion>();north.id="world_north_woodland";AssetDatabase.CreateAsset(north,path);}
        north.label="월드 북쪽 숲";north.scenePath=World;north.useMapBounds=false;north.area=new Rect(-26,-4,52,26);north.spawnInterval=8;north.maxAlive=4;EditorUtility.SetDirty(north);AssetDatabase.SaveAssetIfDirty(north);
        foreach(string name in new[]{"Chaser","Spitter"})
        {
            var type=Resources.Load<MonsterType>("Monsters/"+name);if(!type.spawnRegions.Contains(north))type.spawnRegions=type.spawnRegions.Concat(new[]{north}).ToArray();
            EditorUtility.SetDirty(type);AssetDatabase.SaveAssetIfDirty(type);
        }
    }
    public static void Check()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Isolated checks only.");
        if(!Apply())throw new Exception("Authoring blocked");
        var scene=EditorSceneManager.OpenScene(World);var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();var collision=new MapCollision(farm);
        if(farm.GetComponentsInChildren<MapExit>().Length!=0)throw new Exception("New game must start in a world without active exits");
        if(!collision.CanStand(farm.SpawnPosition,farm.PlayerRadius)||!farm.Layout.CanFarm(farm.SpawnPosition)||farm.Layout.passiveHealthRecovery)throw new Exception("Invalid free camp");
        foreach(var marker in farm.GetComponentsInChildren<FarmItemPlacement>())if(!collision.CanStand(marker.transform.position,.3f))throw new Exception("Blocked item: "+marker.name);
        if(!farm.GetComponentsInChildren<FarmItemPlacement>().Any(p=>p.item==Resources.Load<PlantDefinition>("Equipment/Sword")))throw new Exception("Weapon supply missing");
        if(!farm.GetComponentsInChildren<ShopNpc>().Any(n=>n.npcId==Trader&&collision.CanStand(n.transform.position,.3f)))throw new Exception("Fixed trader missing/blocked");
        var regions=SpawnRegion.ForScene(World);if(regions.Length!=2||regions.Any(r=>r.useMapBounds)||!regions.Any(r=>r.area.Contains(farm.SpawnPosition)))throw new Exception("Invalid regional split");
        string before=File.ReadAllText(World);if(!Apply()||before!=File.ReadAllText(World))throw new Exception("Repeated authoring changed scene");
        ForestFieldLayout.Check();WorldConnectionValidator.Validate();Debug.Log("SINGLE_WORLD_CHECK_PASSED");
    }
}
