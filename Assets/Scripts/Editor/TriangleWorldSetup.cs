using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// One-time Scene authoring. Subsequent manual placement is never regenerated at runtime.
[InitializeOnLoad]
public static class TriangleWorldSetup
{
    public const string Passage="Assets/Scenes/FarmTownPassage.unity", Field="Assets/Scenes/DungeonEntrance.unity",
        Forest="Assets/Scenes/ForestClearing.unity", Training="Assets/Scenes/TrainingGround.unity";
    const string Marker="Triangle world 2026-10-22", Request="Library/TriangleWorld.request", Result="Library/TriangleWorld.result";
    public static string[] Paths => new[]{CombatTestMapSetup.FarmScene,WorldFrameworkSetup.Town,WorldFrameworkSetup.Store,CombatTestMapSetup.CombatScene,Passage,Field,Forest,Training};
    public static bool Installed => File.Exists(Field);
    static TriangleWorldSetup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText(Result,"WAITING_FOR_PLAY_STOP");return;}
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)){File.WriteAllText(Result,"WAITING_FOR_SCENE_SAVE");return;}
        File.Delete(Request);try{Apply();File.WriteAllText(Result,"SUCCESS");}catch(Exception e){Debug.LogException(e);File.WriteAllText(Result,e.ToString());}
    }
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static Vector2[] Line(params float[] xy)=>Enumerable.Range(0,xy.Length/2).Select(i=>V(xy[i*2],xy[i*2+1])).ToArray();
    static FarmGame Farm(Scene s)=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FarmGame>()).Single();
    static void Label(Transform parent,string text,Vector2 p){Root("Sign - "+text,parent,p).AddComponent<WorldLabel>().text=text;}
    static void Save(FarmGame farm){EditorSceneManager.MarkSceneDirty(farm.gameObject.scene);EditorSceneManager.SaveScene(farm.gameObject.scene);}
    static MapArea Area(Transform parent,string name,Vector2 size)
    {var r=Root(name,parent,Vector2.zero);r.transform.localScale=new Vector3(size.x,size.y,1);return r.AddComponent<MapArea>();}
    static void Arrival(FarmGame farm,string id,Vector2 p)
    {
        var a=farm.GetComponentsInChildren<MapArrivalPoint>().SingleOrDefault(x=>x.arrivalId==id);
        if(a==null)a=Root("Arrival - "+id,Group(farm.Layout.transform,"Travel"),p).AddComponent<MapArrivalPoint>();
        a.arrivalId=id;a.transform.position=p;
    }
    static MapExit Exit(FarmGame farm,Vector2 p,string target,string arrival,string label,float angle=0)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapExitTypesSetup.TouchPath),Group(farm.Layout.transform,"Travel"));
        go.name="To "+label;go.transform.SetPositionAndRotation(p,Quaternion.Euler(0,0,angle));
        var exit=go.GetComponent<MapExit>();Route(exit,target,arrival,label);return exit;
    }
    static void Route(MapExit exit,string target,string arrival,string label)
    {
        exit.destinationScene=target;exit.arrivalId=arrival;exit.requireInteract=false;
        foreach(var l in exit.GetComponentsInChildren<WorldLabel>())UnityEngine.Object.DestroyImmediate(l.gameObject);
        Label(exit.transform,label,(Vector2)exit.transform.position+Vector2.up*1.3f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit);PrefabUtility.RecordPrefabInstancePropertyModifications(exit.transform);
    }
    static void Road(FarmGame farm,Vector2[] line,float width=1.2f)
    {
        foreach(var r in farm.Layout.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.GetComponent<FarmGround>()!=null))
        {
            if(Distance(r.transform.position,line)>width)continue;
            r.sprite=Tile;r.color=new Color(.84f,.71f,.46f);r.sortingOrder=-999;
            r.transform.localScale=new Vector3(1/Tile.bounds.size.x,1/Tile.bounds.size.y,1);
        }
    }
    static FarmGame NewMap(string path,Vector2 size,Vector2 spawn,Vector2[][] roads)
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var farm=Root(Path.GetFileNameWithoutExtension(path),null,Vector2.zero).AddComponent<FarmGame>();
        farm.Configure(new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}.Select(n=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+n+".asset")).ToArray());
        var map=Root("EditableMap",farm.transform,Vector2.zero).AddComponent<FarmMapLayout>();farm.ConfigureMap(map);
        map.combatSchedule=FarmMapLayout.CombatSchedule.Peaceful;map.showSafeMarkers=false;
        map.worldBounds=Area(map.transform,"World bounds",size);map.safeArea=Area(map.transform,"Safe area",size);
        map.playerSpawn=Root("Player spawn",map.transform,spawn).transform;
        var ground=Group(map.transform,"Ground and paths");
        for(int x=-(int)size.x/2;x<size.x/2;x++)for(int y=-(int)size.y/2;y<size.y/2;y++)
            Draw("Grass",ground,V(x+.5f,y+.5f),Vector2.one,Color.white,-1000,EditableMapBuilder.PreviewIcon(9)).gameObject.AddComponent<FarmGround>().canFarm=false;
        foreach(var road in roads)Road(farm,road);
        var scenery=Group(map.transform,"Trees and rocks");
        for(float x=-size.x/2+1;x<size.x/2;x+=2.5f)for(float y=-size.y/2+1;y<size.y/2;y+=2.5f)
        {
            var p=V(x+.25f*Mathf.Sin(y*2),y+.3f*Mathf.Cos(x));
            bool edge=Mathf.Abs(x)>size.x/2-3||Mathf.Abs(y)>size.y/2-3;
            if(!edge||roads.Any(r=>Distance(p+Vector2.down*.75f,r)<2.6f))continue;
            Tree(scenery,p);
        }
        var camera=Root("Main Camera",null,spawn).AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5.5f;
        camera.transform.position=new Vector3(spawn.x,spawn.y,-10);camera.backgroundColor=new Color(.15f,.25f,.18f);
        Root(Marker,map.transform,Vector2.zero);EditorSceneManager.SaveScene(scene,path);return farm;
    }
    static void MakePassage()
    {
        if(File.Exists(Passage))return;
        var f=NewMap(Passage,V(24,14),V(-8,0),new[]{Line(-11,0,-5,0,0,-1,5,0,11,0)});
        Exit(f,V(-10,0),CombatTestMapSetup.FarmScene,"farm_path_entry","Farm",180);Arrival(f,"passage_from_farm",V(-7,0));
        Exit(f,V(10,0),WorldFrameworkSetup.Town,"town_from_farm","Town");Arrival(f,"passage_from_town",V(7,0));
        Label(f.Layout.transform,"Farm - Town passage",V(0,2));Save(f);
    }
    static void MakeField()
    {
        if(File.Exists(Field))return;
        var f=NewMap(Field,V(40,28),V(-14,-9),new[]{Line(-17,-11,-10,-8,-5,-4,0,0,0,7),Line(17,-11,10,-8,5,-4,0,0),Line(-18,2,-11,2,-5,-1,0,0,8,2,18,2)});
        Exit(f,V(-17,-11),CombatTestMapSetup.FarmScene,"farm_from_dungeon_field","Farm",225);Arrival(f,"field_from_farm",V(-14,-9));
        Exit(f,V(17,-11),WorldFrameworkSetup.Town,"town_from_dungeon_path","Town",315);Arrival(f,"field_from_town",V(14,-9));
        Exit(f,V(0,7),CombatTestMapSetup.CombatScene,"combat_path_entry","Dungeon",90);Arrival(f,"field_from_dungeon",V(0,3.5f));
        Exit(f,V(-18,2),Forest,"forest_from_field","Forest",180);Arrival(f,"field_from_forest",V(-15,2));
        Exit(f,V(18,2),Training,"training_from_field","Training ground");Arrival(f,"field_from_training",V(15,2));
        var cave=Group(f.Layout.transform,"Dungeon cave exterior");
        Solid(cave,"Left rock",V(-2.6f,7.7f),V(2,4),new Color(.35f,.38f,.38f));
        Solid(cave,"Right rock",V(2.6f,7.7f),V(2,4),new Color(.35f,.38f,.38f));
        Solid(cave,"Cliff above cave",V(0,10.5f),V(7.2f,1.6f),new Color(.42f,.44f,.41f));
        Draw("Cave shadow",cave,V(0,8),V(3.1f,3),new Color(.09f,.1f,.13f),-995);
        Label(cave,"Dungeon entrance",V(0,11.7f));Save(f);
    }
    static void MakeForest()
    {
        if(File.Exists(Forest))return;
        var f=NewMap(Forest,V(28,24),V(10,-5),new[]{Line(12,-5,6,-5,2,-2,-4,0,-8,4),Line(-4,0,-5,5,-1,8)});
        Exit(f,V(12,-5),Field,"field_from_forest","Entrance field");Arrival(f,"forest_from_field",V(9,-5));
        var scenery=Group(f.Layout.transform,"Abandoned clearing");
        Tree(scenery,V(-8,-5),1.4f);Tree(scenery,V(6,6),1.3f);Tree(scenery,V(-9,8));
        Solid(scenery,"Fallen log",V(-7,2),V(3,.7f),new Color(.48f,.29f,.13f));
        Solid(scenery,"Empty abandoned crate",V(5,1),V(1.2f,1),new Color(.58f,.39f,.2f));
        var items=Group(f.Layout.transform,"Items - existing pickup system");
        var positions=new[]{V(-3,1),V(-1,6),V(4,-2),V(-6,-2)};
        var names=new[]{"Stone","Cloud","Stone","Cloud"};
        for(int i=0;i<positions.Length;i++)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/"+names[i]+"Pickup.prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,items);go.transform.position=positions[i];go.transform.localScale=Vector3.one*.8f;
        }
        Label(scenery,"Forest clearing - pick up with E",V(0,4));Save(f);
    }
    static void MakeTraining()
    {
        if(File.Exists(Training))return;
        var f=NewMap(Training,V(26,22),V(-9,-5),new[]{Line(-11,-5,-6,-5,-3,-2,1,0,8,0)});
        Exit(f,V(-11,-5),Field,"field_from_training","Entrance field",180);Arrival(f,"training_from_field",V(-8,-5));
        var targets=Group(f.Layout.transform,"Practice targets - existing combat hits");
        Draw("Sand practice patch",targets,V(2,2),V(12,9),new Color(.77f,.66f,.43f),-998);
        for(int i=0;i<3;i++)
        {
            var go=Root("Practice dummy "+(i+1),targets,V(-2+i*4,3));go.AddComponent<EnemyPlacement>().trainingDummy=true;
            var r=go.AddComponent<SpriteRenderer>();r.sprite=EditableMapBuilder.PreviewIcon(0);r.color=new Color(.85f,.65f,.32f);r.sortingOrder=Depth(3);go.transform.localScale=Vector3.one*1.2f;
            Draw("Wooden crossbar",targets,V(-2+i*4,3),V(1.7f,.14f),new Color(.4f,.25f,.12f),Depth(3)-1);
        }
        // Loan weapons reuse the existing pickup, inventory and attack systems.
        var equipment=Group(f.Layout.transform,"Practice weapons");
        foreach(var name in new[]{"Sword","Bow"})
        {var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/"+name+"Pickup.prefab"),equipment);go.transform.position=V(name=="Sword"?-3:0,-4);go.transform.localScale=Vector3.one*.8f;}
        Label(targets,"Practice targets - no attacks or loot",V(1,7));Save(f);
    }
    static void ConnectExisting(string path)
    {
        var scene=EditorSceneManager.OpenScene(path);var f=Farm(scene);var map=f.Layout;if(map.transform.Find(Marker)!=null)return;
        if(path==CombatTestMapSetup.FarmScene)
        {
            foreach(var exit in f.GetComponentsInChildren<MapExit>())
            {
                if(exit.destinationScene==WorldFrameworkSetup.Town)Route(exit,Passage,"passage_from_farm","Town passage");
                else if(exit.destinationScene==CombatTestMapSetup.CombatScene)Route(exit,Field,"field_from_farm","Dungeon entrance field");
            }
            var legacy=f.GetComponentsInChildren<MapArrivalPoint>().First(a=>a.arrivalId=="farm_from_dungeon");
            Arrival(f,"farm_from_dungeon_field",legacy.transform.position);
        }
        else if(path==WorldFrameworkSetup.Town)
        {
            foreach(var exit in f.GetComponentsInChildren<MapExit>())
            {
                if(exit.destinationScene==CombatTestMapSetup.FarmScene)Route(exit,Passage,"passage_from_town","Farm passage");
                else if(exit.destinationScene==CombatTestMapSetup.CombatScene)
                {exit.transform.SetPositionAndRotation(V(-12,27),Quaternion.Euler(0,0,90));Route(exit,Field,"field_from_town","Dungeon entrance field");}
            }
            var road=Line(-17,10,-12,10,-12,18,-14,22,-12,25,-12,29);
            // Clear roadside decoration only; all building instances, roof identities and NPCs stay intact.
            foreach(var obstacle in map.GetComponentsInChildren<MapObstacle>().Where(o=>Distance(o.transform.position,road)<2.1f).ToArray())
            {
                if(obstacle.GetComponentsInParent<Transform>().Any(t=>t.name=="Buildings"||t.name=="Town districts"))throw new Exception("New guild approach intersects building: "+obstacle.name);
                if(obstacle.name=="Tree trunk")
                {
                    var art=obstacle.transform.parent.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.name=="Tree"&&Vector2.Distance(r.transform.position,obstacle.transform.position+Vector3.up*.75f)<.1f);
                    if(art!=null)UnityEngine.Object.DestroyImmediate(art.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(obstacle.gameObject);
            }
            Road(f,road);Arrival(f,"town_from_dungeon_path",V(-12,24));
            f.GetComponent<NpcManager>().npcs.Single(p=>p.id=="town_guide").greeting="The dungeon entrance field is north of the guild. The western passage leads to your farm.";
        }
        else
        {
            var exits=f.GetComponentsInChildren<MapExit>();var keep=exits.First();
            foreach(var exit in exits.Skip(1))UnityEngine.Object.DestroyImmediate(exit.gameObject);
            Route(keep,Field,"field_from_dungeon","Entrance field");
        }
        Root(Marker,map.transform,Vector2.zero);Save(f);
    }
    [MenuItem("Tools/Everything Grows/World/Apply triangle world layout")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Stop Play and save modified scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            MapExitTypesSetup.GeneratePrefabs();MakePassage();MakeField();MakeForest();MakeTraining();
            foreach(var path in new[]{CombatTestMapSetup.FarmScene,WorldFrameworkSetup.Town,CombatTestMapSetup.CombatScene})ConnectExisting(path);
            var list=EditorBuildSettings.scenes.ToList();foreach(var path in Paths){var entry=list.Find(s=>s.path==path);if(entry!=null)entry.enabled=true;else list.Add(new EditorBuildSettingsScene(path,true));}
            EditorBuildSettings.scenes=list.ToArray();AssetDatabase.SaveAssets();Debug.Log("TRIANGLE_WORLD_AUTHORED");
        }
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
}
