using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class WorldFrameworkSetup
{
    public const string Town="Assets/Scenes/Town.unity", Store="Assets/Scenes/GeneralStore.unity";
    const string Request="Library/WorldFramework.request",Result="Library/WorldFramework.result";
    static Sprite Tile => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MapSprites/TestBlock.png");
    static WorldFrameworkSetup() { if(!Application.isBatchMode) EditorApplication.update+=Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        File.Delete(Request); try { Connect(); File.WriteAllText(Result,"SUCCESS"); } catch(Exception error) { Debug.LogException(error); File.WriteAllText(Result,error.ToString()); }
    }
    static GameObject Root(string name,Transform parent,Vector2 position)
    { var root=new GameObject(name); if(parent!=null) root.transform.SetParent(parent,false); root.transform.position=position; return root; }
    static SpriteRenderer Draw(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order=-95,Sprite sprite=null)
    {
        var root=Root(name,parent,position); var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=sprite!=null?sprite:Tile;
        root.transform.localScale=new Vector3(size.x/renderer.sprite.bounds.size.x,size.y/renderer.sprite.bounds.size.y,1); renderer.color=color; renderer.sortingOrder=order; return renderer;
    }
    static void Label(Transform parent,string text,Vector2 position)
    { var label=Root("Label - "+text,parent,position).AddComponent<WorldLabel>(); label.text=text; }
    static MapArea Area(string name,Transform parent,Vector2 center,Vector2 size)
    { var root=Root(name,parent,center); root.transform.localScale=new Vector3(size.x,size.y,1); return root.AddComponent<MapArea>(); }
    static FarmGame NewMap(string name,Vector2 size,Vector2 spawn)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var farm=Root(name,null,Vector2.zero).AddComponent<FarmGame>(); farm.Configure(new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}.Select(n=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+n+".asset")).ToArray());
        var map=Root("EditableMap",farm.transform,Vector2.zero).AddComponent<FarmMapLayout>(); farm.ConfigureMap(map); map.combatSchedule=FarmMapLayout.CombatSchedule.Peaceful; map.showSafeMarkers=false;
        map.worldBounds=Area("World bounds",map.transform,Vector2.zero,size); map.safeArea=Area("Safe area",map.transform,Vector2.zero,size); map.playerSpawn=Root("Player spawn",map.transform,spawn).transform;
        var camera=Root("Main Camera",null,spawn).AddComponent<Camera>(); camera.tag="MainCamera"; camera.orthographic=true; camera.orthographicSize=5; camera.transform.position=new Vector3(spawn.x,spawn.y,-10); camera.backgroundColor=new Color(.14f,.22f,.19f);
        return farm;
    }
    static void Arrival(Transform parent,Vector2 position,string id)
    { var point=Root("Arrival - "+id,parent,position).AddComponent<MapArrivalPoint>(); point.arrivalId=id; }
    static MapExit Exit(Transform parent,Vector2 position,bool door,string target,string arrival,string label)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(door?MapExitTypesSetup.DoorPath:MapExitTypesSetup.TouchPath);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent); root.name=label; root.transform.position=position;
        var exit=root.GetComponent<MapExit>(); exit.destinationScene=target; exit.arrivalId=arrival; PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
        Label(root.transform,label,position+Vector2.up*1.6f); return exit;
    }
    static void Block(string name,Transform parent,Vector2 position,Vector2 size,Color color)
    { var part=Draw(name,parent,position,size,color,30-Mathf.RoundToInt((position.y-size.y*.5f)*10)); part.gameObject.AddComponent<MapObstacle>(); }
    static GameObject BuildingPrefab(string name,Color tint)
    {
        string path="Assets/Prefabs/Map/"+name+".prefab"; var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(saved!=null) return saved;
        var root=Root(name,null,Vector2.zero); var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/FarmHouse.png");
        Draw("Building art",root.transform,Vector2.zero,new Vector2(5,5*sprite.bounds.size.y/sprite.bounds.size.x),tint,-15,sprite);
        var obstacle=root.AddComponent<MapObstacle>(); obstacle.size=new Vector2(4.3f,1.6f); obstacle.offset=new Vector2(0,-.75f);
        saved=PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root); return saved;
    }
    static void House(Transform parent,string name,Vector2 point,Color tint)
    {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(BuildingPrefab(name,tint),parent); root.transform.position=point;
        foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>()) { renderer.sortingOrder=30-Mathf.RoundToInt((point.y-1.55f)*10); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
    }
    static ShopNpc Npc(Transform parent,Vector2 position,string id,string label,Color tint,bool dayOnly)
    {
        var renderer=Draw(label,parent,position,Vector2.one*1.1f,tint,30-Mathf.RoundToInt(position.y*10),EditableMapBuilder.PreviewIcon(0));
        var npc=renderer.gameObject.AddComponent<ShopNpc>(); npc.npcId=id; npc.daytimeOnly=dayOnly; Label(npc.transform,label,position+Vector2.up*.8f); return npc;
    }
    static void MakeTown()
    {
        var farm=NewMap("Town",new Vector2(32,22),new Vector2(-11,0)); var map=farm.Layout; var ground=Root("Ground and paths",map.transform,Vector2.zero).transform;
        for(int x=-16;x<16;x++) for(int y=-11;y<11;y++)
        {
            bool path=Mathf.Abs(y)<=1 || (Mathf.Abs(x)<=1 && y>=-3 && y<=7) || (Mathf.Abs(x+8)<=1 && y>=0 && y<=4) || (Mathf.Abs(x-8)<=1 && y>=0 && y<=4) || (Mathf.Abs(x)<=3 && Mathf.Abs(y)<=3);
            var sprite=path?Tile:EditableMapBuilder.PreviewIcon(9); Draw(path?"Path":"Grass",ground,new Vector2(x,y),Vector2.one,path?new Color(.72f,.63f,.43f):Color.white,-100,sprite).gameObject.AddComponent<FarmGround>().canFarm=false;
        }
        var buildings=Root("Buildings",map.transform,Vector2.zero).transform;
        House(buildings,"TownGeneralStore",new Vector2(0,7),Color.white); House(buildings,"TownSmith",new Vector2(-8,6),new Color(.8f,.85f,.95f)); House(buildings,"TownHouse",new Vector2(8,7),new Color(1,.9f,.8f));
        Label(buildings,"GENERAL STORE",new Vector2(0,8.9f)); Label(buildings,"SMITH",new Vector2(-8,7.9f)); Label(buildings,"VILLAGE HOUSE",new Vector2(8,8.9f));
        var plaza=Root("Plaza and decorations",map.transform,Vector2.zero).transform;
        Block("Fountain base",plaza,Vector2.zero,new Vector2(1.8f,1.4f),new Color(.48f,.55f,.6f)); Draw("Fountain water",plaza,new Vector2(0,.15f),new Vector2(1.4f,.8f),new Color(.3f,.7f,.85f),38); Draw("Fountain center",plaza,new Vector2(0,.4f),new Vector2(.4f,.8f),new Color(.7f,.75f,.8f),39);
        foreach(float x in new[]{-4.5f,4.5f}) { Block("Bench",plaza,new Vector2(x,-3),new Vector2(1.6f,.5f),new Color(.45f,.27f,.13f)); Block("Lamp base",plaza,new Vector2(x,3),new Vector2(.2f,.3f),new Color(.3f,.25f,.2f)); Draw("Lamp",plaza,new Vector2(x,3.65f),new Vector2(.2f,1.3f),new Color(.36f,.29f,.2f),5); Draw("Lamp glow",plaza,new Vector2(x,4.3f),Vector2.one*.5f,new Color(1,.85f,.38f),6); }
        foreach(float x in new[]{-13f,-10f,-6f,6f,10f,13f}) foreach(float y in new[]{-7f,9f})
        { var tree=Draw("Tree",plaza,new Vector2(x,y),new Vector2(1.8f,2.3f),Color.white,30-Mathf.RoundToInt((y-1)*10),EditableMapBuilder.PreviewIcon(13)); var obstacle=tree.gameObject.AddComponent<MapObstacle>(); obstacle.size=new Vector2(.35f,.25f); obstacle.offset=new Vector2(0,-.35f); }
        var npcs=Root("Residents",map.transform,Vector2.zero).transform; var manager=farm.gameObject.AddComponent<NpcManager>();
        manager.npcs.Add(new NpcProfile {id="town_guide",displayName="Mira - Village guide",shopEnabled=false,greeting="Welcome! Your farm is west of the square. The general store is north, and the dungeon entrance is to the east."});
        manager.npcs.Add(new NpcProfile {id="town_smith",displayName="Rowan - Smith",shopEnabled=false,greeting="Heading into the ruins? Bring something to defend yourself with. The entrance is a safe place to catch your breath."});
        manager.npcs.Add(new NpcProfile {id="town_resident",displayName="June - Resident",shopEnabled=false,greeting="I like seeing what people bring back from their farms. There is always something surprising growing out there."});
        Npc(npcs,new Vector2(-3,-2),"town_guide","Mira",new Color(.8f,.9f,1),false); Npc(npcs,new Vector2(-8,3),"town_smith","Rowan",new Color(.9f,.73f,.6f),false); Npc(npcs,new Vector2(8,3.5f),"town_resident","June",new Color(1,.8f,.87f),false);
        var travel=Root("Travel",map.transform,Vector2.zero).transform;
        Exit(travel,new Vector2(-14,0),false,CombatTestMapSetup.FarmScene,"farm_path_entry","Farm"); Exit(travel,new Vector2(-14,-3),true,CombatTestMapSetup.FarmScene,"farm_from_combat","Farm door [E]");
        Exit(travel,new Vector2(13,0),true,CombatTestMapSetup.CombatScene,"combat_entry","Dungeon [E]"); Exit(travel,new Vector2(13,-3),false,CombatTestMapSetup.CombatScene,"combat_path_entry","Dungeon path");
        Arrival(travel,new Vector2(-11,0),"town_from_farm"); Arrival(travel,new Vector2(-11,-2),"town_from_farm_door"); Arrival(travel,new Vector2(10,0),"town_from_dungeon"); Arrival(travel,new Vector2(10,-3),"town_from_dungeon_path");
        var door=Root("General store door - E",travel,new Vector2(0,4.6f)).AddComponent<MapExit>(); door.destinationScene=Store; door.arrivalId="store_entry"; door.areaSize=new Vector2(1.6f,1);
        Draw("Store doormat",door.transform,door.transform.position,new Vector2(1.4f,.65f),door.ModeColor,-30); Label(door.transform,"Store [E]",new Vector2(0,4.1f)); Arrival(travel,new Vector2(0,3.4f),"town_from_store");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Town);
    }
    static void MakeStore(ShopOffer[] offers)
    {
        var farm=NewMap("General Store",new Vector2(14,12),new Vector2(0,-3)); farm.Layout.dayNightLighting=false;
        var map=farm.Layout; var room=Root("Interior",map.transform,Vector2.zero).transform;
        for(int x=-6;x<=6;x++) for(int y=-5;y<=5;y++) Draw("Wooden floor",room,new Vector2(x,y),Vector2.one,new Color(.55f,.37f,.22f),-100).gameObject.AddComponent<FarmGround>().canFarm=false;
        Block("Back wall",room,new Vector2(0,5),new Vector2(14,1),new Color(.42f,.3f,.22f)); Block("Left wall",room,new Vector2(-6,0),new Vector2(1,10),new Color(.42f,.3f,.22f)); Block("Right wall",room,new Vector2(6,0),new Vector2(1,10),new Color(.42f,.3f,.22f)); Block("Front wall",room,new Vector2(0,-5),new Vector2(14,1),new Color(.42f,.3f,.22f));
        Block("Shop counter",room,new Vector2(0,1.5f),new Vector2(6,.7f),new Color(.36f,.21f,.1f));
        foreach(float x in new[]{-4.5f,4.5f}) { Block("Shelf",room,new Vector2(x,2.5f),new Vector2(1.2f,3),new Color(.4f,.25f,.12f)); for(int y=2;y<=3;y++) Draw("Goods",room,new Vector2(x,y),Vector2.one*.65f,Color.white,40,EditableMapBuilder.PreviewIcon(y==2?5:6)); }
        var manager=farm.gameObject.AddComponent<NpcManager>(); manager.npcs.Add(new NpcProfile {id="town_merchant",displayName="General Store",greeting="Welcome!",offers=offers.ToList()});
        Npc(map.transform,new Vector2(0,2.8f),"town_merchant","Merchant",new Color(.85f,.95f,1),true);
        var door=Root("Exit to town - E",map.transform,new Vector2(0,-4.1f)).AddComponent<MapExit>(); door.destinationScene=Town; door.arrivalId="town_from_store"; door.areaSize=new Vector2(2,1);
        Draw("Doormat",door.transform,door.transform.position,new Vector2(2,.6f),door.ModeColor,-30); Label(door.transform,"Town [E]",new Vector2(0,-3.8f)); Arrival(map.transform,new Vector2(0,-3),"store_entry");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Store);
    }
    static void EnableScene(string path)
    { var list=EditorBuildSettings.scenes.ToList(); var entry=list.Find(e=>e.path==path); if(entry!=null) entry.enabled=true; else list.Add(new EditorBuildSettingsScene(path,true)); EditorBuildSettings.scenes=list.ToArray(); }
    [MenuItem("Tools/Everything Grows/World/Connect farm, town and dungeon")]
    public static void Connect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before connecting the world.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Town)==null || AssetDatabase.LoadAssetAtPath<SceneAsset>(Store)==null) throw new Exception("Town and GeneralStore scenes are required.");
        var active=SceneManager.GetActiveScene();
        foreach(string path in new[]{CombatTestMapSetup.FarmScene,CombatTestMapSetup.CombatScene})
        {
            var scene=SceneManager.GetSceneByPath(path); bool opened=!scene.IsValid() || !scene.isLoaded; if(opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var exit in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MapExit>()))
                {
                    if(exit.GetComponentsInParent<Transform>().Any(parent=>parent.name=="Farm Dungeon Shortcut")) continue;
                    string previous=path==CombatTestMapSetup.FarmScene?CombatTestMapSetup.CombatScene:CombatTestMapSetup.FarmScene;
                    if(exit.destinationScene!=previous && exit.destinationScene!=Town) continue;
                    Undo.RecordObject(exit,"Route through town"); exit.destinationScene=Town;
                    exit.arrivalId=path==CombatTestMapSetup.FarmScene?(exit.requireInteract?"town_from_farm_door":"town_from_farm"):(exit.requireInteract?"town_from_dungeon":"town_from_dungeon_path");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(exit); EditorUtility.SetDirty(exit);
                    if(exit.transform.Find("Town route label")==null) { var label=Root("Town route label",exit.transform,(Vector2)exit.transform.position+Vector2.up*1.5f).AddComponent<WorldLabel>(); label.text=exit.requireInteract?"Town [E]":"Town"; Undo.RegisterCreatedObjectUndo(label.gameObject,"Label town route"); }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if(opened) EditorSceneManager.CloseScene(scene,true); }
        }
        EnableScene(Town); EnableScene(Store); SceneManager.SetActiveScene(active); AssetDatabase.SaveAssets(); Debug.Log("WORLD_FRAMEWORK_CONNECTED");
    }
    public static void Generate()
    {
        if(!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)) throw new Exception("Generate only in an isolated temporary project.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Town)!=null || AssetDatabase.LoadAssetAtPath<SceneAsset>(Store)!=null) throw new Exception("Preserve existing town scenes.");
        EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene); var manager=UnityEngine.Object.FindFirstObjectByType<NpcManager>();
        var offers=manager.npcs.First(p=>p.id=="farm_merchant").offers.Select(o=>new ShopOffer {item=o.item,buyPrice=o.buyPrice,sellPrice=o.sellPrice}).ToArray();
        MakeTown(); MakeStore(offers); EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene); Connect(); Debug.Log("WORLD_FRAMEWORK_GENERATED");
    }
}
