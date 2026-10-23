using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// One-time authoring only. No runtime regeneration of manually edited floors.
public static class TowerDungeonSetup
{
    public const string Lobby="Assets/Scenes/TowerFirstFloor.unity";
    public const string Battle=CombatTestMapSetup.CombatScene;
    public static bool Installed=>File.Exists(Lobby);
    private const string Marker="Tower floors 2026-10-23";
    private static Vector2 V(float x,float y)=>new Vector2(x,y);
    private static void Label(Transform parent,string text,Vector2 point)
    { var label=Root(text,parent,point).AddComponent<WorldLabel>();label.text=text;label.visibleDistance=8; }
    private static void Save(FarmGame farm)
    { EditorSceneManager.MarkSceneDirty(farm.gameObject.scene);EditorSceneManager.SaveScene(farm.gameObject.scene); }
    private static void Arrival(Transform parent,string id,Vector2 point)
    { var arrival=Root("Arrival - "+id,parent,point).AddComponent<MapArrivalPoint>();arrival.arrivalId=id; }
    private static MapArea Area(Transform parent,string name,Vector2 size)
    { var root=Root(name,parent,Vector2.zero);root.transform.localScale=new Vector3(size.x,size.y,1);return root.AddComponent<MapArea>(); }
    private static void Stairs(MapExit exit,string target,string arrival,string title,bool up)
    {
        if(PrefabUtility.IsPartOfPrefabInstance(exit.gameObject))
            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(exit.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var oldVisual=exit.GetComponent<MapExitVisual>();if(oldVisual!=null)UnityEngine.Object.DestroyImmediate(oldVisual);
        foreach(Transform child in exit.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        exit.name=title;exit.destinationScene=target;exit.arrivalId=arrival;exit.requireInteract=true;exit.requireClearFloor=true;exit.areaSize=V(2.4f,2.4f);
        exit.transform.rotation=Quaternion.identity;exit.transform.localScale=Vector3.one;
        Vector2 p=exit.transform.position;
        Draw("Stone stairwell",exit.transform,p,V(2.4f,2.4f),new Color(.16f,.22f,.25f),-80);
        for(int i=0;i<6;i++)
        {
            float shade=up?.44f+i*.055f:.72f-i*.055f;
            Draw("Step "+(i+1),exit.transform,p+V(0,-.9f+i*.35f),V(1.8f,.26f),new Color(shade,shade+.07f,shade+.09f),-78);
        }
        foreach(int side in new[]{-1,1})Draw("Teal stair rail",exit.transform,p+V(side*1.05f,0),V(.12f,2.3f),exit.ModeColor,-77);
        Label(exit.transform,title+" [F]",p+Vector2.down*1.65f);
    }
    private static void MakeLobby()
    {
        if(Installed) return;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var farm=Root("Tower 1F - Preparation",null,Vector2.zero).AddComponent<FarmGame>();
        farm.Configure(new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}.Select(n=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+n+".asset")).ToArray());
        var map=Root("EditableMap",farm.transform,Vector2.zero).AddComponent<FarmMapLayout>();farm.ConfigureMap(map);
        map.combatSchedule=FarmMapLayout.CombatSchedule.Peaceful;map.dayNightLighting=false;map.showSafeMarkers=false;
        map.worldBounds=Area(map.transform,"World bounds",V(24,20));map.safeArea=Area(map.transform,"Safe area - whole floor",V(24,20));
        map.playerSpawn=Root("Player spawn",map.transform,V(0,-5.5f)).transform;
        var floor=Group(map.transform,"Stone floor");
        for(int x=-12;x<12;x++)for(int y=-10;y<10;y++)
        {
            float shade=(x+y)%2==0?.36f:.39f;
            Draw("Stone tile",floor,V(x+.5f,y+.5f),Vector2.one,new Color(shade,shade+.035f,shade+.055f),-1000).gameObject.AddComponent<FarmGround>().canFarm=false;
        }
        var walls=Group(map.transform,"Tower walls and pillars");var stone=new Color(.26f,.31f,.35f);
        Solid(walls,"North wall",V(0,9),V(24,1.4f),stone);Solid(walls,"South wall",V(0,-9.5f),V(24,.7f),stone);
        foreach(int side in new[]{-1,1})
        {
            Solid(walls,"Side wall",V(side*11.5f,0),V(.7f,20),stone);
            foreach(int end in new[]{-1,1})Solid(walls,"Tower corner pier",V(side*10,end*8),V(2,2),stone);
            Solid(walls,"Pillar",V(side*4,2),V(1.2f,1.4f),new Color(.46f,.5f,.53f));
        }
        var furniture=Group(map.transform,"Preparation area - decoration only");
        Draw("Entrance runner",furniture,V(0,-2.3f),V(3,8),new Color(.23f,.39f,.48f),-995);
        foreach(int side in new[]{-1,1})
        {
            Solid(furniture,"Waiting bench",V(side*7,-2),V(3,.8f),new Color(.45f,.3f,.19f));
            Draw("Wall banner",furniture,V(side*6,7.2f),V(1.2f,1.8f),new Color(.28f,.52f,.58f),-70);
        }
        Label(furniture,"Tower 1F - Preparation",V(0,6.5f));
        var travel=Group(map.transform,"Travel");
        var outside=Root("Outside - automatic",travel,V(0,-7.8f)).AddComponent<MapExit>();outside.destinationScene=TriangleWorldSetup.Field;outside.arrivalId="field_from_dungeon";outside.requireInteract=false;outside.areaSize=V(2.8f,1.2f);
        Draw("Outside threshold",outside.transform,outside.transform.position,V(2.8f,1.2f),outside.ModeColor,-80);
        Label(outside.transform,"Outside",V(0,-8.7f));Arrival(travel,"tower_from_field",V(0,-5.5f));
        var up=Root("Upstairs to 2F",travel,V(6,4)).AddComponent<MapExit>();Stairs(up,Battle,"combat_path_entry","Up to 2F",true);
        Arrival(travel,"tower_from_second",V(3.5f,3.5f));
        var camera=Root("Main Camera",null,Vector2.zero).AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=4.8f;camera.transform.position=new Vector3(0,-3,-10);camera.backgroundColor=new Color(.1f,.13f,.17f);
        Root(Marker,map.transform,Vector2.zero);EditorSceneManager.SaveScene(scene,Lobby);
    }
    private static void ConnectBattle()
    {
        var scene=EditorSceneManager.OpenScene(Battle);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        if(farm.Layout.transform.Find(Marker)!=null) return;
        var down=farm.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==TriangleWorldSetup.Field);
        Stairs(down,Lobby,"tower_from_second","Down to 1F",false);
        farm.Layout.combatSchedule=FarmMapLayout.CombatSchedule.Always;farm.Layout.dayNightLighting=false;
        Label(farm.Layout.transform,"Tower 2F",V(-7,4.2f));Root(Marker,farm.Layout.transform,Vector2.zero);MapSaveAuthoring.PrepareCurrentMap();Save(farm);
    }
    private static void ConnectExterior()
    {
        var scene=EditorSceneManager.OpenScene(TriangleWorldSetup.Field);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        if(farm.Layout.transform.Find(Marker)!=null) return;
        var entrance=farm.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==Battle);
        entrance.destinationScene=Lobby;entrance.arrivalId="tower_from_field";entrance.name="To tower 1F";
        PrefabUtility.RecordPrefabInstancePropertyModifications(entrance);
        foreach(var text in entrance.GetComponentsInChildren<WorldLabel>())text.text="Tower entrance";
        var old=farm.Layout.transform.Find("Dungeon cave exterior");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var tower=Group(farm.Layout.transform,"Tower exterior");var stone=new Color(.43f,.46f,.51f);
        Draw("Tower face",tower,V(0,9.5f),V(8,5),stone,-993);
        Draw("Dark entrance",tower,V(0,8),V(3.2f,3),new Color(.08f,.11f,.15f),-990);
        foreach(int side in new[]{-1,1})
        {
            Solid(tower,"Tower side pier",V(side*2.9f,8.8f),V(1.5f,4.2f),new Color(.34f,.39f,.45f));
            Draw("Upper window",tower,V(side*2,10.8f),V(.65f,1),new Color(.16f,.25f,.31f),-988);
        }
        Solid(tower,"Tower crown",V(0,12),V(8.5f,1),new Color(.53f,.56f,.6f));
        for(int i=-3;i<=3;i+=2)Draw("Battlement",tower,V(i,12.9f),V(.8f,1),new Color(.53f,.56f,.6f),-980);
        Label(tower,"Tower",V(0,11));Root(Marker,farm.Layout.transform,Vector2.zero);Save(farm);
    }
    [MenuItem("Tools/Everything Grows/World/Create tower first and second floors")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Stop Play and save modified scenes before authoring tower floors.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            MakeLobby();ConnectBattle();ConnectExterior();
            var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==Lobby))scenes.Add(new EditorBuildSettingsScene(Lobby,true));else scenes.Single(s=>s.path==Lobby).enabled=true;
            EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
            Debug.Log("TOWER_FLOORS_AUTHORED: peaceful 1F, existing combat scene as 2F, F stairs, automatic exterior travel.");
        }
        finally { if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
