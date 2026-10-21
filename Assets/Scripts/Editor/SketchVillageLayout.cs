using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// The user's sketch controls positions; the Pelican Town pass supplies the visual vocabulary.
public static class SketchVillageLayout
{
    const string Marker="User sketch with village square 2026-10-21";
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static Vector2[] Line(params float[] points)=>Enumerable.Range(0,points.Length/2).Select(i=>V(points[i*2],points[i*2+1])).ToArray();
    static readonly Vector2[][] Roads={
        Line(-32,0,0,0,0,29),
        Line(0,0,0,-17,-32,-17),
        Line(-17,0,-17,12),
        Line(0,5,29,5),
        Line(18,5,18,12),
        Line(12,5,12,-20),
        Line(0,-20,29,-20,29,-16,24,-16),
        Line(12,-1,24,-1),
        Line(-24,-23,0,-23,0,-17),
        Line(-25,15,-25,19,-23,21),
        Line(-25,15,-25,13,-17,13),
        Line(-23,-13,-23,-17),
        Line(-14,-13,-14,-17),
        Line(-7,-12.5f,-7,-17),
        Line(-8,6,-8,5,0,5),
        Line(7,-16.5f,7,-20)
    };
    static readonly (string name,Vector2 point,Vector2 scale)[] Additional={
        ("Clinic exterior",V(-7,-11),V(.6f,.9f)),
        ("Saloon exterior",V(9,8),V(1,1.15f)),
        ("River Road house",V(-8,8),V(.8f,.85f)),
        ("Willow Lane east house",V(-24,-20),V(.85f,1)),
        ("Mayor house exterior",V(7,-14),V(1.1f,1.15f)),
        ("Riverside trailer",V(18,-5),V(.9f,.5f)),
        ("East market exterior",V(20,3),V(1.5f,.9f)),
        ("Museum exterior",V(19,-17),V(1.35f,.8f))
    };
    static readonly Rect[] Bridges={new Rect(24,3.8f,6,2.4f),new Rect(24,-17.2f,6,2.4f)};
    static readonly Vector2[] Entries={V(-30,0),V(-30,-17),V(-1.5f,27),V(1.5f,27),V(-14,-14)};
    static readonly Vector2[] Landmarks={V(-17,15),V(18,15),V(-23,-11),V(-14,-11)};
    static float RoadDistance(Vector2 point)=>Roads.Min(road=>Distance(point,road));
    static float Shore(float x)=>-25.5f+Mathf.Sin(x*.14f)*.5f;
    static float RiverX(float y)=>27+Mathf.Sin(y*.14f)*.8f;
    static bool Water(Vector2 p)=>p.y<Shore(p.x)||Mathf.Abs(p.x-RiverX(p.y))<1.25f;
    static bool Bridge(Vector2 p)=>Bridges.Any(r=>r.Contains(p));
    static bool Square(Vector2 p)=>Mathf.Abs(p.x)<=4.8f&&Mathf.Abs(p.y)<=5.2f;
    static bool StoneStreet(Vector2 p)=>Square(p)||Mathf.Abs(p.y)<.8f&&p.x<0||Mathf.Abs(p.x)<.8f||Mathf.Abs(p.y+17)<.8f&&p.x<0;
    static void Flower(Transform parent,Vector2 p)=>Draw("Flowers",parent,p,Vector2.one*.65f,new Color(1,.9f,.8f),-970,EditableMapBuilder.PreviewIcon(14));
    static void ApplyTown()
    {
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var map=farm.Layout;
        if(map.transform.Find(Marker)!=null)return;
        var ground=Group(map.transform,"Ground and paths");Clear(ground);var scenery=Group(map.transform,"Plaza and decorations");Clear(scenery);
        map.worldBounds.transform.localScale=new Vector3(64,60,1);map.safeArea.transform.localScale=new Vector3(64,60,1);map.playerSpawn.position=V(-27,0);
        var waterGroup=Group(scenery,"East river and south coast");var square=Group(scenery,"Central village square");var park=Group(scenery,"Northwest guild park");var greenery=Group(scenery,"Gardens and trees");
        for(int x=-32;x<32;x++)
        {
            int start=-30;
            for(int y=-30;y<=30;y++)
            {
                Vector2 p=V(x+.5f,y+.5f);bool blocked=y<30&&Water(p)&&!Bridge(p);
                if(!blocked&&y>start)Block(waterGroup,"Water boundary",V(x+.5f,(start+y)*.5f),V(1,y-start));
                if(!blocked)start=y+1;
                if(y==30)continue;
                bool water=Water(p),bridge=Bridge(p),stone=!water&&StoneStreet(p);
                bool dirt=!water&&(RoadDistance(p)<.85f||p.y<Shore(p.x)+1.4f||Mathf.Abs(p.x-RiverX(p.y))<2);
                Color color=bridge?new Color(.57f,.4f,.24f):water?new Color(.24f,.58f,.75f):stone?new Color(.58f,.56f,.47f):dirt?new Color(.86f,.73f,.45f):Color.white;
                Draw(bridge?"Bridge deck":water?"Water":stone?"Stone paving":dirt?"Dirt path":"Grass",ground,p,Vector2.one,color,-1000,bridge||water||stone||dirt?Tile:EditableMapBuilder.PreviewIcon(9)).gameObject.AddComponent<FarmGround>().canFarm=false;
                if(water&&!bridge&&(x+y)%5==0)Draw("Water ripple",waterGroup,p,V(.6f,.035f),new Color(.56f,.8f,.88f),-998);
            }
        }
        foreach(var bridge in Bridges)foreach(int side in new[]{-1,1})Solid(waterGroup,"Bridge railing",bridge.center+Vector2.up*side*(bridge.height*.5f+.1f),V(bridge.width,.15f),new Color(.65f,.46f,.25f));
        var buildings=Group(map.transform,"Buildings");
        Building(buildings,"TownGuild",Landmarks[0],Vector2.one,true,new Color(.9f,.88f,.79f));
        Building(buildings,"TownSmith",Landmarks[1],V(.9f,.9f),true,new Color(.78f,.83f,.88f));
        Building(buildings,"TownHouse",Landmarks[2],Vector2.one,true,new Color(.83f,.9f,1));
        Building(buildings,"TownGeneralStore",Landmarks[3],V(1.1f,1.05f),true,new Color(1,.9f,.77f));
        foreach(var label in buildings.GetComponentsInChildren<WorldLabel>())
        {
            if(label.text=="TOWN HALL"||label.text=="GUILD"){label.text="GUILD";label.transform.position=Landmarks[0]+Vector2.up*3;}
            else if(label.text=="SMITH")label.transform.position=Landmarks[1]+Vector2.up*2.5f;
            else if(label.text=="VILLAGE HOUSE")label.transform.position=Landmarks[2]+Vector2.up*2.5f;
            else if(label.text=="GENERAL STORE")label.transform.position=Landmarks[3]+Vector2.up*2.5f;
        }
        var districts=Group(map.transform,"Town districts");Clear(districts);
        Color[] colors={new Color(.9f,1,1),new Color(.87f,.86f,1),new Color(1,.87f,.67f),new Color(.8f,.95f,1)};
        for(int i=0;i<Additional.Length;i++){var b=Additional[i];Building(districts,b.name,b.point,b.scale,false,colors[i%colors.Length]);}
        // The square stays open, with a central inset and benches along its edges.
        Draw("Square center inlay",square,Vector2.zero,V(1.8f,1.8f),new Color(.66f,.43f,.34f),-995);
        Draw("Square center stone",square,Vector2.zero,V(1.2f,1.2f),new Color(.63f,.59f,.48f),-994);
        foreach(var p in new[]{V(-5.8f,-3),V(5.8f,2.8f),V(5.8f,-3)})Solid(square,"Bench",p,V(1.7f,.45f),new Color(.5f,.33f,.18f));
        foreach(var p in new[]{V(-6.2f,3.5f),V(6.2f,-4.5f),V(-10,-17.9f),V(14,5.9f)})
        {
            Solid(square,"Lamp base",p,V(.2f,.22f),new Color(.3f,.27f,.2f));
            Draw("Lamp post",square,p+Vector2.up*.7f,V(.14f,1.4f),new Color(.34f,.27f,.19f),Depth(p.y)+1);
            Draw("Lamp light",square,p+Vector2.up*1.4f,Vector2.one*.4f,new Color(1,.86f,.45f),Depth(p.y)+2);
        }
        Solid(park,"Fountain basin",V(-25,17),V(1.8f,1.1f),new Color(.58f,.6f,.55f));
        Draw("Fountain water",park,V(-25,17.2f),V(1.4f,.7f),new Color(.3f,.66f,.86f),Depth(17)+1);
        Draw("Fountain spout",park,V(-25,17.7f),V(.24f,.9f),new Color(.77f,.78f,.71f),Depth(17)+2);
        Draw("Playground sand",park,V(-23,23),V(5,2.5f),new Color(.81f,.72f,.55f),-995);
        foreach(float x in new[]{-24.5f,-22.5f})Solid(park,"Swing post",V(x,23),V(.14f,1.5f),new Color(.55f,.34f,.16f));
        Draw("Swing beam",park,V(-23.5f,23.7f),V(2.2f,.15f),new Color(.55f,.34f,.16f),Depth(23)+1);
        foreach(float x in new[]{-24f,-23f})Draw("Swing chain",park,V(x,23),V(.06f,1.3f),new Color(.35f,.36f,.32f),Depth(23)+1);
        Draw("Swing seats",park,V(-23.5f,22.4f),V(1.4f,.15f),new Color(.7f,.4f,.2f),Depth(23)+2);
        foreach(var rect in new[]{new Rect(-32,9,13.5f,.7f),new Rect(-15.5f,9,8,.7f)})Solid(park,"Park terrace edge",rect.center,rect.size,new Color(.47f,.4f,.25f),-850);
        for(int i=0;i<5;i++)Draw("Guild park stairs",park,V(-17,9+i*.18f),V(2.4f,.1f),new Color(.69f,.64f,.48f),-840);
        var allBuildings=Additional.Select(b=>b.point).Concat(Landmarks).ToArray();
        var clearPoints=Entries.Concat(Additional.Select(b=>b.point+Vector2.down*(b.scale.y*1.55f+.75f))).Concat(new[]{V(-17,10.8f),V(18,11.2f),V(-23,-14.4f),V(-27,0)}).ToArray();
        var random=new System.Random(1022);
        for(int i=0;i<170;i++)
        {
            Vector2 p=V(-30+(float)random.NextDouble()*60,-23+(float)random.NextDouble()*51);
            if(Water(p)||Bridge(p)||RoadDistance(p)<1.7f||Square(p)||allBuildings.Any(b=>Mathf.Abs(p.x-b.x)<4&&Mathf.Abs(p.y-b.y)<4)||clearPoints.Any(c=>Vector2.Distance(p,c)<2)||Vector2.Distance(p,V(-25,17))<2.5f||Vector2.Distance(p,V(-23,23))<3.5f)continue;
            if(i%3==0)Tree(greenery,p);else Flower(greenery,p);
        }
        foreach(var npc in map.GetComponentsInChildren<ShopNpc>())npc.transform.position=npc.npcId=="town_guide"?V(-17,11.8f):npc.npcId=="town_smith"?V(18,12.2f):V(-23,-13.4f);
        farm.GetComponent<NpcManager>().npcs.Single(n=>n.id=="town_guide").greeting="Welcome! The guild is northwest, the smith northeast, and the store southwest of the central square. The northern road leads to the dungeon.";
        foreach(var exit in map.GetComponentsInChildren<MapExit>())
        {
            Vector2 p;float angle=0;
            if(exit.destinationScene==WorldFrameworkSetup.Store)p=Entries[4];
            else if(exit.destinationScene==CombatTestMapSetup.FarmScene){p=exit.requireInteract?Entries[1]:Entries[0];angle=exit.requireInteract?0:180;}
            else{p=exit.requireInteract?Entries[3]:Entries[2];angle=exit.requireInteract?0:90;}
            exit.transform.SetPositionAndRotation(p,Quaternion.Euler(0,0,angle));foreach(var label in exit.GetComponentsInChildren<WorldLabel>())label.transform.position=p+Vector2.up*1.6f;PrefabUtility.RecordPrefabInstancePropertyModifications(exit.transform);
        }
        foreach(var arrival in map.GetComponentsInChildren<MapArrivalPoint>())switch(arrival.arrivalId)
        {
            case "town_from_farm":arrival.transform.position=V(-27,0);break;
            case "town_from_farm_door":arrival.transform.position=V(-27,-17);break;
            case "town_from_dungeon":arrival.transform.position=V(1.5f,24);break;
            case "town_from_dungeon_path":arrival.transform.position=V(-1.5f,24);break;
            case "town_from_store":arrival.transform.position=V(-14,-15.2f);break;
        }
        Root(Marker,map.transform,Vector2.zero);EditorSceneManager.MarkSceneDirty(farm.gameObject.scene);EditorSceneManager.SaveScene(farm.gameObject.scene);
    }
    [MenuItem("Tools/Everything Grows/World/Apply user sketch village square")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before authoring.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save open scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try{ApplyTown();AssetDatabase.SaveAssets();Debug.Log("SKETCH_VILLAGE_APPLIED");}
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
    public static void Verify()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        Apply();WorldConnectionValidator.Validate();EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var collision=new MapCollision(farm);Capture(farm,"SketchTownOverview.png");
        var targets=farm.Layout.GetComponentsInChildren<MapArrivalPoint>().Select(a=>(Vector2)a.transform.position)
            .Concat(farm.Layout.GetComponentsInChildren<MapExit>().Select(e=>(Vector2)e.transform.position))
            .Concat(farm.Layout.GetComponentsInChildren<ShopNpc>().Select(n=>(Vector2)n.transform.position+Vector2.down))
            .Concat(Additional.Select(b=>b.point+Vector2.down*(b.scale.y*1.55f+.75f))).Concat(new[]{Vector2.zero,V(-25,15),V(-23,21),V(30,5),V(30,-16)}).ToArray();
        foreach(var target in targets)
        {
            var path=collision.FindPath(farm.SpawnPosition,target,farm.PlayerRadius);
            if(!collision.CanStand(target,farm.PlayerRadius)||(Vector2.Distance(farm.SpawnPosition,target)>.3f&&(path.Count==0||Vector2.Distance(path.Last(),target)>.3f)))throw new Exception("Sketch village route blocked: "+target);
        }
        var buildings=farm.Layout.transform.Find("Buildings");
        if((Vector2)buildings.Find("TownGuild").position!=Landmarks[0]||(Vector2)buildings.Find("TownSmith").position!=Landmarks[1]||(Vector2)buildings.Find("TownGeneralStore").position!=Landmarks[3])throw new Exception("Sketch landmark positions changed.");
        if(collision.Trace(V(23,12),V(31,12),farm.PlayerRadius)>=1)throw new Exception("River collision missing.");
        Debug.Log("SKETCH_VILLAGE_PATHS_SUCCESS: "+targets.Length+" destinations, central square, sketch landmarks and river blocking checked.");WorldFrameworkCheck.Run();
    }
}
