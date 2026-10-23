using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

[InitializeOnLoad]
public static class InteriorWorldSetup
{
    public const string Home="Assets/Scenes/FarmHouseInterior.unity",Guild="Assets/Scenes/GuildInterior.unity",Smith="Assets/Scenes/SmithInterior.unity";
    public static readonly string[] Rooms={Home,Guild,Smith};
    public static bool Installed=>File.Exists(Home);
    const string Marker="Building interiors 2026-10-22",DoorPrefab="Assets/Resources/Buildings/HouseDoor.prefab",Request="Library/InteriorWorld.request",Result="Library/InteriorWorld.result";
    static InteriorWorldSetup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText(Result,"WAITING_FOR_PLAY_STOP");return;}
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)){File.WriteAllText(Result,"WAITING_FOR_SCENE_SAVE");return;}
        File.Delete(Request);try{Apply();File.WriteAllText(Result,"SUCCESS");}catch(Exception e){Debug.LogException(e);File.WriteAllText(Result,e.ToString());}
    }
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static void Label(Transform parent,string text,Vector2 p)=>Root("Label - "+text,parent,p).AddComponent<WorldLabel>().text=text;
    static void Save()=>EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    internal static MapArrivalPoint Arrival(Transform parent,string id,Vector2 p)
    {var a=Root("Arrival - "+id,parent,p).AddComponent<MapArrivalPoint>();a.arrivalId=id;return a;}
    internal static MapExit Door(Transform parent,string name,Vector2 p,string target,string arrival)
    {
        var go=Root(name,parent,p);var exit=go.AddComponent<MapExit>();exit.destinationScene=target;exit.arrivalId=arrival;exit.requireInteract=true;exit.areaSize=V(1.8f,1.2f);
        Draw("Blue doormat",go.transform,p,V(1.6f,.7f),exit.ModeColor,-900);Label(go.transform,name+" [RMB]",p+Vector2.down*.8f);return exit;
    }
    static void MakeDoorPrefab()
    {
        if(File.Exists(DoorPrefab))return;
        var exit=Door(null,"House entrance",Vector2.zero,Home,"home_entry");exit.gameObject.AddComponent<HouseInteriorLink>();
        PrefabUtility.SaveAsPrefabAsset(exit.gameObject,DoorPrefab);UnityEngine.Object.DestroyImmediate(exit.gameObject);
    }
    internal static FarmGame Room(string path,Vector2 size,Color floor)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var f=Root(Path.GetFileNameWithoutExtension(path),null,Vector2.zero).AddComponent<FarmGame>();
        f.Configure(new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}.Select(n=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+n+".asset")).ToArray());
        var map=Root("EditableMap",f.transform,Vector2.zero).AddComponent<FarmMapLayout>();f.ConfigureMap(map);
        map.combatSchedule=FarmMapLayout.CombatSchedule.Peaceful;map.dayNightLighting=false;map.showSafeMarkers=false;
        foreach(string name in new[]{"World bounds","Safe area"})
        {var go=Root(name,map.transform,Vector2.zero);go.transform.localScale=new Vector3(size.x,size.y,1);var a=go.AddComponent<MapArea>();if(name=="World bounds")map.worldBounds=a;else map.safeArea=a;}
        map.playerSpawn=Root("Player spawn",map.transform,V(0,-size.y/2+3)).transform;
        var ground=Group(map.transform,"Floor and walls");
        for(int x=-(int)size.x/2;x<size.x/2;x++)for(int y=-(int)size.y/2;y<size.y/2;y++)
            Draw("Floor",ground,V(x+.5f,y+.5f),Vector2.one,floor,-1000).gameObject.AddComponent<FarmGround>().canFarm=false;
        var wall=new Color(.34f,.25f,.2f);
        Solid(ground,"North wall",V(0,size.y/2-1),V(size.x,1.3f),wall);
        Solid(ground,"South wall",V(0,-size.y/2+.5f),V(size.x,.6f),wall);
        foreach(int sign in new[]{-1,1})Solid(ground,"Side wall",V(sign*(size.x/2-.5f),0),V(.6f,size.y),wall);
        var camera=Root("Main Camera",null,V(0,-2)).AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5;
        camera.transform.position=new Vector3(0,-2,-10);
        Root(Marker,map.transform,Vector2.zero);EditorSceneManager.SaveScene(scene,path);return f;
    }
    static void Furniture(Transform p,string name,Vector2 at,Vector2 size,Color color)=>Solid(p,name,at,size,color);
    static ShopNpc Npc(FarmGame f,string id,string display,Vector2 p,Color tint,NpcProfile profile)
    {
        var manager=f.GetComponent<NpcManager>()??f.gameObject.AddComponent<NpcManager>();manager.npcs.Add(profile);
        var r=Draw(display,Group(f.Layout.transform,"Residents"),p,Vector2.one*1.1f,tint,Depth(p.y),EditableMapBuilder.PreviewIcon(0));
        var npc=r.gameObject.AddComponent<ShopNpc>();npc.npcId=id;npc.daytimeOnly=false;npc.interactionRange=2.6f;Label(npc.transform,display,p+Vector2.up*.9f);return npc;
    }
    static void MakeHome()
    {
        if(File.Exists(Home))return;
        var f=Room(Home,V(18,16),new Color(.57f,.39f,.25f));var p=Group(f.Layout.transform,"Home furniture");
        Draw("Woven rug",p,V(0,-1),V(5,4),new Color(.6f,.26f,.22f),-998);
        Furniture(p,"Bed frame",V(-5,3),V(2.5f,3),new Color(.33f,.2f,.12f));
        Draw("Bed blanket",p,V(-5,2.7f),V(2.1f,2.1f),new Color(.36f,.57f,.7f),Depth(3)+1);Draw("Pillow",p,V(-5,4),V(1.8f,.6f),new Color(.92f,.89f,.76f),Depth(3)+2);
        Furniture(p,"Dining table",V(2,1),V(3,1.4f),new Color(.42f,.26f,.12f));
        Furniture(p,"Chair",V(2,-.4f),V(.8f,.6f),new Color(.55f,.35f,.17f));
        Furniture(p,"Bookshelf",V(-5.6f,5.4f),V(3,1),new Color(.35f,.22f,.12f));
        Furniture(p,"Fireplace",V(5,5.2f),V(2.6f,1.5f),new Color(.38f,.38f,.4f));Draw("Hearth embers",p,V(5,5),V(1.4f,.6f),new Color(.95f,.45f,.15f),Depth(5.2f)+1);
        foreach(int sign in new[]{-1,1})Draw("Window",p,V(sign*3,6.7f),V(1.5f,.7f),new Color(.5f,.76f,.87f),Depth(7)+2);
        var travel=Group(f.Layout.transform,"Travel");var exit=Door(travel,"Outside",V(0,-6.2f),CombatTestMapSetup.FarmScene,"farm_from_house");exit.gameObject.AddComponent<HouseInteriorLink>().returnToHouse=true;
        Arrival(travel,"home_entry",V(0,-4.7f));Label(p,"Farmhouse",V(0,5.5f));Save();
    }
    static void MakeGuild()
    {
        if(File.Exists(Guild))return;
        var f=Room(Guild,V(22,18),new Color(.48f,.43f,.35f));var p=Group(f.Layout.transform,"Guild furniture");
        Draw("Entrance runner",p,V(0,-3),V(3,6),new Color(.25f,.38f,.52f),-998);
        Furniture(p,"Reception counter",V(0,2),V(8,.8f),new Color(.38f,.24f,.13f));
        Furniture(p,"Notice board - decoration",V(-7,4),V(3,1.5f),new Color(.52f,.34f,.18f));
        for(int i=0;i<3;i++)Draw("Notice paper",p,V(-7.8f+i*.8f,4),V(.5f,.8f),new Color(.91f,.85f,.63f),Depth(4)+1);
        foreach(int sign in new[]{-1,1}){Furniture(p,"Waiting bench",V(sign*7,-2),V(3,.8f),new Color(.45f,.28f,.14f));Draw("Guild banner",p,V(sign*5,6.8f),V(1.3f,2.2f),new Color(.22f,.36f,.6f),Depth(7)+2);}
        Npc(f,"guild_reception","Guild reception",V(0,3.4f),new Color(.72f,.82f,1),new NpcProfile{id="guild_reception",displayName="Guild reception",shopEnabled=false,greeting="Welcome to the guild. The dungeon entrance is just north of the village. Prepare your equipment before heading in."});
        var travel=Group(f.Layout.transform,"Travel");Door(travel,"Town",V(0,-7.2f),WorldFrameworkSetup.Town,"town_from_guild");Arrival(travel,"guild_entry",V(0,-5.5f));
        Label(p,"Adventurers guild",V(0,6));Save();
    }
    static void MakeSmith(ShopOffer[] stock)
    {
        if(File.Exists(Smith))return;
        var f=Room(Smith,V(20,16),new Color(.37f,.38f,.39f));var p=Group(f.Layout.transform,"Smith furniture");
        Furniture(p,"Smith counter",V(0,2),V(6,.8f),new Color(.32f,.24f,.18f));
        Furniture(p,"Forge brickwork",V(-5,4.5f),V(3,2),new Color(.39f,.25f,.22f));Draw("Forge fire",p,V(-5,4.4f),V(1.8f,.8f),new Color(1,.49f,.13f),Depth(4.5f)+1);
        Furniture(p,"Anvil base",V(5,0),V(1.3f,1),new Color(.23f,.25f,.28f));Draw("Anvil top",p,V(5,.3f),V(2.2f,.45f),new Color(.61f,.65f,.69f),Depth(0)+1);
        Furniture(p,"Weapon rack",V(6,4.5f),V(2.8f,1),new Color(.32f,.22f,.14f));
        Draw("Display sword",p,V(5.4f,4.6f),V(.8f,1.4f),Color.white,Depth(4.5f)+2,EditableMapBuilder.PreviewIcon(16));
        Draw("Display bow",p,V(6.6f,4.6f),V(.8f,1.4f),Color.white,Depth(4.5f)+2,EditableMapBuilder.PreviewIcon(18));
        Npc(f,"town_smith","Rowan - Smith",V(0,3.4f),new Color(.9f,.73f,.6f),new NpcProfile{id="town_smith",displayName="Rowan - Smith",shopEnabled=true,greeting="Need a weapon? Take a look. You can also sell the items you brought.",offers=stock.ToList()});
        var travel=Group(f.Layout.transform,"Travel");Door(travel,"Town",V(0,-6.2f),WorldFrameworkSetup.Town,"town_from_smith");Arrival(travel,"smith_entry",V(0,-4.7f));Label(p,"Smithy",V(0,6));Save();
    }
    static void ConnectFarm()
    {
        var scene=EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f.Layout.transform.Find(Marker)!=null)return;
        foreach(var building in f.GetComponentsInChildren<FarmBuilding>())
        {
            if(building.GetComponentInChildren<HouseInteriorLink>()!=null)continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefab),building.transform);
            go.name="House entrance - E";var b=building.WorldFootprint;go.transform.position=V(b.center.x,b.yMin-.7f);
            var s=building.transform.lossyScale;go.transform.localScale=new Vector3(1/s.x,1/s.y,1);
        }
        Arrival(Group(f.Layout.transform,"Travel"),"farm_from_house",f.SpawnPosition);Root(Marker,f.Layout.transform,Vector2.zero);EditorSceneManager.MarkSceneDirty(scene);Save();
    }
    static void ConnectTown()
    {
        var scene=EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f.Layout.transform.Find(Marker)!=null)return;
        var travel=Group(f.Layout.transform,"Travel");
        foreach(var entry in new[]{(name:"TownGuild",target:Guild,id:"guild"),(name:"TownSmith",target:Smith,id:"smith")})
        {
            var building=f.Layout.transform.Find("Buildings/"+entry.name);var bounds=building.GetComponent<MapObstacle>().Bounds;
            var p=V(bounds.center.x,bounds.yMin-.7f);var exit=Door(building,entry.name,p,entry.target,entry.id+"_entry");
            Arrival(travel,"town_from_"+entry.id,p+Vector2.down*1.3f);
        }
        // Rowan now works indoors; preserve the exterior profile so old authored configuration is retained.
        foreach(var npc in f.GetComponentsInChildren<ShopNpc>().Where(n=>n.npcId=="town_smith").ToArray())UnityEngine.Object.DestroyImmediate(npc.gameObject);
        Root(Marker,f.Layout.transform,Vector2.zero);EditorSceneManager.MarkSceneDirty(scene);Save();
    }
    // One-time authoring entry retained for fixtures; no production menu.
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Stop Play and save modified scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);var manager=UnityEngine.Object.FindFirstObjectByType<NpcManager>();
            var stock=manager.npcs.SelectMany(n=>n.offers).Where(o=>o.item!=null&&o.item.effect==GrowthEffect.Weapon).GroupBy(o=>o.item).Select(g=>new ShopOffer{item=g.Key,buyPrice=g.First().buyPrice,sellPrice=g.First().sellPrice}).ToArray();
            if(stock.Length==0)throw new Exception("No existing weapon offers to reuse.");
            MakeDoorPrefab();MakeHome();MakeGuild();MakeSmith(stock);ConnectFarm();ConnectTown();
            var list=EditorBuildSettings.scenes.ToList();foreach(var path in Rooms){var e=list.Find(s=>s.path==path);if(e!=null)e.enabled=true;else list.Add(new EditorBuildSettingsScene(path,true));}EditorBuildSettings.scenes=list.ToArray();
            AssetDatabase.SaveAssets();Debug.Log("BUILDING_INTERIORS_AUTHORED: home, guild, smith; existing trade and editable furnishings.");
        }
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
}
