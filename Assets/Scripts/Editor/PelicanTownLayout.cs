using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

// Scene authoring only. Coordinates follow the Pelican Town reference image (784 x 720).
public static class PelicanTownLayout
{
    const string Marker="Pelican town reference layout 2026-10-21";
    static Sprite Tile=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MapSprites/TestBlock.png");
    static Vector2 P(float x,float y)=>new Vector2((x-392)*.08f,(360-y)*.08f);
    static Vector2[] Line(params float[] coordinates)=>Enumerable.Range(0,coordinates.Length/2).Select(i=>P(coordinates[i*2],coordinates[i*2+1])).ToArray();
    static readonly Vector2[] River=Line(615,-30,615,50,605,100,587,155,597,190,563,228,528,250,486,278,474,312,485,348,472,380,491,410,522,442,526,472,510,502,497,530,500,563,512,595,509,625,511,652,531,679,556,700,570,750);
    static readonly Vector2[] SouthRiver=Line(-20,637,50,645,110,671,190,686,270,688,350,697,420,704,481,696,531,699);
    static readonly Vector2[][] DirtRoads={
        Line(529,-12,529,160,519,182,460,182,449,205,367,205,352,181),
        Line(352,141,352,180,360,217,307,250,267,263,261,290,261,310),
        Line(261,148,262,235,307,250),
        Line(100,104,70,125,55,162,67,194,105,232,137,273,137,294),
        Line(105,232,163,202,174,202),
        Line(174,202,192,189,192,164,174,153,151,164,151,193,174,202),
        Line(319,386,319,459,351,491,352,578,354,640,354,702),
        Line(315,464,278,489,266,518,318,518,351,540,421,583,460,588),
        Line(451,356,451,412,456,437,466,460,464,504,445,542,421,583),
        Line(535,357,579,357,641,357,683,365,689,416,689,456,664,489,627,491,621,524,672,550,672,590,642,615,545,615),
        Line(619,524,579,555,568,607,545,615),
        Line(0,421,44,421,45,476,72,491,72,514),
        Line(147,495,147,526),
        Line(195,600,229,620,229,655,277,659,296,641,353,641)
    };
    static readonly Rect[] StoneAreas={R(0,349,315,376),R(137,284,151,376),R(310,282,324,395),R(146,373,252,496),R(182,491,198,605),R(0,590,198,610),R(192,533,352,551)};
    static readonly Rect[] Bridges={R(455,343,507,368),R(484,602,538,629),R(341,667,367,740)};
    static Rect R(float left,float top,float right,float bottom)=>Rect.MinMaxRect(P(left,bottom).x,P(left,bottom).y,P(right,top).x,P(right,top).y);
    static readonly (string name,Vector2 point,Vector2 scale)[] ExtraBuildings={
        ("Clinic exterior",P(224,337),new Vector2(.6f,.9f)),
        ("Saloon exterior",P(377,393),new Vector2(1,1.15f)),
        ("River Road house",P(282,446),new Vector2(.8f,.85f)),
        ("Willow Lane east house",P(148,551),new Vector2(.85f,1)),
        ("Mayor house exterior",P(408,530),new Vector2(1.1f,1.15f)),
        ("Riverside trailer",P(475,443),new Vector2(.9f,.5f)),
        ("East market exterior",P(630,313),new Vector2(1.5f,.9f)),
        ("Museum exterior",P(674,578),new Vector2(1.35f,.8f))
    };
    static GameObject Root(string name,Transform parent,Vector2 p)
    {var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.position=p;return root;}
    static Transform Group(Transform parent,string name)=>parent.Find(name)??Root(name,parent,Vector2.zero).transform;
    static void Clear(Transform parent){foreach(Transform child in parent.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);}
    static SpriteRenderer Draw(string name,Transform parent,Vector2 p,Vector2 size,Color color,int order=-1000,Sprite sprite=null)
    {
        var r=Root(name,parent,p).AddComponent<SpriteRenderer>();r.sprite=sprite!=null?sprite:Tile;r.color=color;r.sortingOrder=order;
        r.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1);return r;
    }
    static int Depth(float y)=>30-Mathf.RoundToInt(y*10);
    static float Distance(Vector2 p,Vector2[] line)
    {float result=float.PositiveInfinity;for(int i=1;i<line.Length;i++)result=Mathf.Min(result,OpenFieldCombat.SegmentDistance(line[i-1],line[i],p));return result;}
    static float DirtDistance(Vector2 p)=>DirtRoads.Min(line=>Distance(p,line));
    static bool Water(Vector2 p)=>Distance(p,River)<1.45f||Distance(p,SouthRiver)<1.2f;
    static bool Bridge(Vector2 p)=>Bridges.Any(rect=>rect.Contains(p));
    static void Block(Transform parent,string name,Vector2 p,Vector2 size)
    {var obstacle=Root(name,parent,p).AddComponent<MapObstacle>();obstacle.size=size;}
    static void Solid(Transform parent,string name,Vector2 p,Vector2 size,Color color,int? order=null)
    {Draw(name,parent,p,size,color,order??Depth(p.y));Block(parent,name+" collision",p,size);}
    static void Tree(Transform parent,Vector2 p,float size=1)
    {Draw("Tree",parent,p,new Vector2(1.8f,2.3f)*size,Color.white,Depth(p.y-size),EditableMapBuilder.PreviewIcon(13));Block(parent,"Tree trunk",p+Vector2.down*.75f*size,new Vector2(.45f,.35f)*size);}
    static void Building(Transform parent,string name,Vector2 p,Vector2 scale,bool existing,Color tint)
    {
        Transform root;
        if(existing)root=parent.Find(name);
        else{var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/TownHouse.prefab"),parent);go.name=name;root=go.transform;}
        if(root==null)throw new Exception("Missing building: "+name);
        root.position=p;root.localScale=new Vector3(scale.x,scale.y,1);
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>()){r.color=tint;r.sortingOrder=Depth(p.y-1.55f*scale.y);PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        PrefabUtility.RecordPrefabInstancePropertyModifications(root);
    }
    static void Fence(Transform parent,float x1,float x2,float y)
    {
        var left=P(x1,y);var right=P(x2,y);float width=right.x-left.x;
        Draw("Fence rail",parent,(left+right)*.5f,new Vector2(width,.1f),new Color(.6f,.4f,.2f),Depth(left.y));
        for(float x=left.x;x<=right.x+.01f;x+=.65f)Draw("Fence post",parent,new Vector2(x,left.y+.1f),new Vector2(.12f,.6f),new Color(.7f,.48f,.25f),Depth(left.y)+1);
        Block(parent,"Fence collision",(left+right)*.5f,new Vector2(width,.2f));
    }
    static Vector2 BuildingEntry(Vector2 p,Vector2 scale)=>p+Vector2.down*(1.55f*scale.y+.75f);
    static void Build()
    {
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var map=farm.Layout;
        if(map.transform.Find(Marker)!=null)return;
        var ground=Group(map.transform,"Ground and paths");Clear(ground);var scenery=Group(map.transform,"Plaza and decorations");Clear(scenery);
        var old=map.transform.Find("Residential neighborhoods");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        map.worldBounds.transform.localScale=new Vector3(64,60,1);map.safeArea.transform.localScale=new Vector3(64,60,1);map.playerSpawn.position=P(48,363);
        var riverGroup=Group(scenery,"River and three bridges");var park=Group(scenery,"North park and playground");var greenery=Group(scenery,"Gardens and trees");
        for(int x=-32;x<32;x++)
        {
            int runStart=-30;
            for(int y=-30;y<=30;y++)
            {
                Vector2 p=new Vector2(x+.5f,y+.5f);bool solidWater=y<30&&Water(p)&&!Bridge(p);
                if(!solidWater&&y>runStart){Block(riverGroup,"River bank collision",new Vector2(x+.5f,(runStart+y)*.5f),new Vector2(1,y-runStart));}
                if(!solidWater)runStart=y+1;
                if(y==30)continue;
                bool water=Water(p),bridge=Bridge(p),stone=StoneAreas.Any(rect=>rect.Contains(p));
                float riverDistance=Mathf.Min(Distance(p,River)-1.45f,Distance(p,SouthRiver)-1.2f);
                bool path=!water&&(DirtDistance(p)<.65f||riverDistance<.75f);
                Color color=bridge?new Color(.57f,.4f,.24f):water?new Color(.24f,.58f,.75f):stone?new Color(.58f,.56f,.47f):path?new Color(.86f,.73f,.45f):Color.white;
                Draw(bridge?"Bridge deck":water?"River":stone?"Stone street":path?"Dirt path":"Grass",ground,p,Vector2.one,color,-1000,water||bridge||stone||path?Tile:EditableMapBuilder.PreviewIcon(9)).gameObject.AddComponent<FarmGround>().canFarm=false;
                if(water&&!bridge&&(x+y)%5==0)Draw("Water ripple",riverGroup,p,new Vector2(.6f,.035f),new Color(.56f,.8f,.88f),-998);
            }
        }
        foreach(var rect in Bridges)
        {
            bool vertical=rect.height>rect.width;
            for(int side=-1;side<=1;side+=2)
            {
                var point=rect.center+(vertical?Vector2.right*(rect.width*.5f+.1f):Vector2.up*(rect.height*.5f+.1f))*side;
                Solid(riverGroup,"Bridge railing",point,vertical?new Vector2(.15f,rect.height):new Vector2(rect.width,.15f),new Color(.65f,.46f,.25f));
            }
        }
        var buildings=Group(map.transform,"Buildings");
        Building(buildings,"TownGuild",P(345,106),new Vector2(1.1f,1.1f),true,new Color(.9f,.88f,.79f));
        Building(buildings,"TownGeneralStore",P(277,338),new Vector2(1.1f,1.05f),true,new Color(1,.9f,.77f));
        Building(buildings,"TownSmith",P(615,518),new Vector2(.9f,.9f),true,new Color(.78f,.83f,.88f));
        Building(buildings,"TownHouse",P(75,539),Vector2.one,true,new Color(.83f,.9f,1));
        foreach(var label in buildings.GetComponentsInChildren<WorldLabel>())
        {
            if(label.text=="GUILD"){label.text="TOWN HALL";label.transform.position=P(345,67);}
            else if(label.text=="SMITH")label.transform.position=P(615,480);
            else if(label.text=="GENERAL STORE")label.transform.position=P(277,298);
            else if(label.text=="VILLAGE HOUSE")label.transform.position=P(75,500);
        }
        var districts=Group(map.transform,"Town districts");Clear(districts);
        Color[] colors={new Color(.9f,1,1),new Color(.87f,.86f,1),new Color(1,.87f,.67f),new Color(.8f,.95f,1)};
        for(int i=0;i<ExtraBuildings.Length;i++){var b=ExtraBuildings[i];Building(districts,b.name,b.point,b.scale,false,colors[i%colors.Length]);}
        Solid(park,"Fountain basin",P(174,178),new Vector2(1.8f,1.1f),new Color(.58f,.6f,.55f));
        Draw("Fountain water",park,P(174,175),new Vector2(1.4f,.7f),new Color(.3f,.66f,.86f),Depth(P(174,178).y)+1);
        Draw("Fountain spout",park,P(174,169),new Vector2(.24f,.9f),new Color(.77f,.78f,.71f),Depth(P(174,178).y)+2);
        // Flat park paving, with a swing frame and sandbox as editable placeholder geometry.
        Draw("Playground sand",park,P(130,82),new Vector2(6.5f,2.7f),new Color(.81f,.72f,.55f),-995);
        Solid(park,"Swing left post",P(94,83),new Vector2(.14f,1.5f),new Color(.55f,.34f,.16f));
        Solid(park,"Swing right post",P(121,83),new Vector2(.14f,1.5f),new Color(.55f,.34f,.16f));
        Draw("Swing beam",park,P(107.5f,74),new Vector2(2.4f,.15f),new Color(.55f,.34f,.16f),Depth(P(107,83).y)+1);
        foreach(float x in new[]{101f,114f})Draw("Swing chain",park,P(x,84),new Vector2(.06f,1.3f),new Color(.35f,.36f,.32f),Depth(P(x,83).y)+1);
        Draw("Swing seats",park,P(107.5f,91),new Vector2(1.4f,.15f),new Color(.7f,.4f,.2f),Depth(P(107,83).y)+2);
        Draw("Square center inlay",scenery,P(192,447),new Vector2(1.5f,1.5f),new Color(.66f,.43f,.34f),-995);
        Draw("Square center stone",scenery,P(192,447),Vector2.one,new Color(.63f,.59f,.48f),-994);
        // Northern raised park boundary. Leave both stair routes open.
        foreach(var section in new[]{R(0,287,128,302),R(153,287,302,302),R(325,287,421,302)})
            Solid(park,"Park terrace edge",section.center,section.size,new Color(.47f,.4f,.25f),-850);
        foreach(float x in new[]{137f,313f})for(int i=0;i<5;i++)Draw("Park stairs",park,P(x,286+i*4),new Vector2(1.4f,.13f),new Color(.69f,.64f,.48f),-840);
        Fence(greenery,222,290,76);Fence(greenery,382,443,147);Fence(greenery,554,612,470);Fence(greenery,674,726,502);
        var treePoints=Line(22,40,50,61,35,130,21,208,60,251,104,271,193,104,229,70,290,41,398,49,440,81,480,109,485,155,319,188,403,235,339,272,402,301,423,333,418,387,352,455,265,490,229,515,212,563,261,585,302,572,312,623,388,615,417,643,31,472,19,541,15,587,577,418,600,453,720,409,742,443,739,493,733,546,744,608,683,660,625,660,584,654,554,280,705,266,711,210,738,172,750,105,734,55,727,23);
        var allBuildings=ExtraBuildings.Select(b=>b.point).Concat(new[]{P(345,106),P(277,338),P(615,518),P(75,539)}).ToArray();
        foreach(var p in treePoints)if(!Water(p)&&!Bridge(p)&&!StoneAreas.Any(r=>r.Contains(p))&&DirtDistance(p)>.85f&&allBuildings.All(b=>Vector2.Distance(b,p)>2.7f)&&new[]{P(12,600),P(12,363),P(549,15),P(512,15),P(277,369)}.All(entry=>Vector2.Distance(entry,p)>2))Tree(greenery,p);
        for(int i=0;i<45;i++)
        {
            Vector2 p=new Vector2(-29+(i*13%58),-24+(i*17%48));
            if(Water(p)||Bridge(p)||StoneAreas.Any(r=>r.Contains(p))||DirtDistance(p)<1.3f||allBuildings.Any(b=>Vector2.Distance(b,p)<3))continue;
            Draw("Shrub and flowers",greenery,p,Vector2.one*.7f,new Color(.84f,1,.82f),-970,EditableMapBuilder.PreviewIcon(14));
        }
        foreach(var npc in map.GetComponentsInChildren<ShopNpc>())npc.transform.position=npc.npcId=="town_guide"?P(345,141):npc.npcId=="town_smith"?P(615,549):P(75,571);
        farm.GetComponent<NpcManager>().npcs.Single(n=>n.id=="town_guide").greeting="Welcome! The general store is beside the central square. Cross the eastern bridge for the smith; the northern road leads to the dungeon.";
        foreach(var exit in map.GetComponentsInChildren<MapExit>())
        {
            Vector2 p;float angle=0;
            if(exit.destinationScene==WorldFrameworkSetup.Store)p=P(277,369);
            else if(exit.destinationScene==CombatTestMapSetup.FarmScene){p=exit.requireInteract?P(12,600):P(12,363);angle=exit.requireInteract?0:180;}
            else{p=exit.requireInteract?P(549,15):P(512,15);angle=exit.requireInteract?0:90;}
            exit.transform.SetPositionAndRotation(p,Quaternion.Euler(0,0,angle));foreach(var label in exit.GetComponentsInChildren<WorldLabel>())label.transform.position=p+Vector2.up*1.6f;PrefabUtility.RecordPrefabInstancePropertyModifications(exit.transform);
        }
        foreach(var arrival in map.GetComponentsInChildren<MapArrivalPoint>())switch(arrival.arrivalId)
        {
            case "town_from_farm":arrival.transform.position=P(48,363);break;
            case "town_from_farm_door":arrival.transform.position=P(48,600);break;
            case "town_from_dungeon":arrival.transform.position=P(549,49);break;
            case "town_from_dungeon_path":arrival.transform.position=P(512,49);break;
            case "town_from_store":arrival.transform.position=P(277,386);break;
        }
        Root(Marker,map.transform,Vector2.zero);EditorSceneManager.MarkSceneDirty(farm.gameObject.scene);EditorSceneManager.SaveScene(farm.gameObject.scene);
    }
    [MenuItem("Tools/Everything Grows/World/Apply Pelican Town reference layout")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before authoring.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save open scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try{Build();AssetDatabase.SaveAssets();Debug.Log("PELICAN_LAYOUT_APPLIED");}
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
    static void Capture(FarmGame farm)
    {
        var camera=Camera.main; camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=32;
        var target=new RenderTexture(960,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(960,900,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,960,900),0,0);image.Apply();System.IO.File.WriteAllBytes("PelicanOverview.png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
    }
    public static void VerifyOldSavePosition()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();farm.Initialize(false);
        var blocked=new Vector2(-27,5.3f);var valid=P(192,447);var spawn=farm.SpawnPosition;var collision=new MapCollision(farm);
        if(collision.CanStand(blocked,farm.PlayerRadius))throw new Exception("Old spawn no longer reproduces the blocked-save case.");
        foreach(var position in new[]{blocked,valid})
        {
            GameSession.ResetForTests();var data=new GameSaveData{currentScene=WorldFrameworkSetup.Town};data.player.position=position;data.player.gold=77;data.player.health=2;
            System.IO.Directory.CreateDirectory(GameSession.SaveDirectory);System.IO.File.WriteAllText(GameSession.SavePath,JsonUtility.ToJson(data));GameSession.Bind(farm);
            if(!string.IsNullOrEmpty(GameSession.LastError)||Vector2.Distance(farm.PlayerPosition,position==blocked?spawn:valid)>.01f||farm.Gold!=77||!collision.CanStand(farm.PlayerPosition,farm.PlayerRadius))throw new Exception("Save position restore failed.");
        }
        Debug.Log("PELICAN_SAVE_RELOCATION_SUCCESS: blocked old spawn moved to clear entry; valid save position and gold preserved.");
    }
    public static void Verify()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        Apply();WorldConnectionValidator.Validate();EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var collision=new MapCollision(farm); Capture(farm);
        var targets=farm.Layout.GetComponentsInChildren<MapArrivalPoint>().Select(a=>(Vector2)a.transform.position)
            .Concat(farm.Layout.GetComponentsInChildren<MapExit>().Select(e=>(Vector2)e.transform.position))
            .Concat(farm.Layout.GetComponentsInChildren<ShopNpc>().Select(n=>(Vector2)n.transform.position+Vector2.down))
            .Concat(ExtraBuildings.Select(b=>BuildingEntry(b.point,b.scale))).Concat(new[]{P(174,205),P(130,110),P(192,447),P(354,652)}).ToArray();
        foreach(var target in targets)
        {
            var path=collision.FindPath(farm.SpawnPosition,target,farm.PlayerRadius);
            if(!collision.CanStand(target,farm.PlayerRadius)||(Vector2.Distance(farm.SpawnPosition,target)>.3f&&(path.Count==0||Vector2.Distance(path.Last(),target)>.3f)))throw new Exception("Reference layout route blocked to "+target+" stand="+collision.CanStand(target,farm.PlayerRadius)+" count="+path.Count+" last="+(path.Count>0?path.Last().ToString():"none"));
        }
        if(collision.Trace(P(429,600),P(582,600),farm.PlayerRadius)>=1)throw new Exception("River can be crossed away from bridge.");
        Debug.Log("PELICAN_PATHS_SUCCESS: "+targets.Length+" destinations and river collision checked.");WorldFrameworkCheck.Run();
    }
}
