using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

// One-time authoring pass. Generated objects remain ordinary editable Scene objects.
public static class SpaciousTownLayout
{
    const string Marker="Spacious town 2026-10-21";
    static Sprite Tile => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MapSprites/TestBlock.png");
    static readonly Vector2[] Homes={new Vector2(-26,16),new Vector2(-8,14),new Vector2(8,14),new Vector2(26,8),new Vector2(12,-1),new Vector2(24,-3),new Vector2(15,-10),new Vector2(-4,-10),new Vector2(-25,-10)};
    static readonly Vector2[][] Roads={
        new[]{new Vector2(-31,6),new Vector2(-24,5),new Vector2(-13,4),new Vector2(-2,3),new Vector2(9,5),new Vector2(18,9),new Vector2(27,5)},
        new[]{new Vector2(-31,-7),new Vector2(-21,-6),new Vector2(-11,-6),new Vector2(-7,-2),new Vector2(-2,3)},
        new[]{new Vector2(-24,5),new Vector2(-26,10),new Vector2(-26,13.5f)},
        new[]{new Vector2(-18,9.5f),new Vector2(-18,4.5f)},
        new[]{new Vector2(-8,11.5f),new Vector2(-8,7),new Vector2(-2,3)},
        new[]{new Vector2(8,11.5f),new Vector2(8,7),new Vector2(-2,3)},
        new[]{new Vector2(-2,3),new Vector2(-1,10),new Vector2(0,16),new Vector2(0,21)},
        new[]{new Vector2(27,5),new Vector2(27,-6),new Vector2(20,-13),new Vector2(8,-13),new Vector2(-4,-13),new Vector2(-16,-13),new Vector2(-27,-13),new Vector2(-27,-6)},
        new[]{new Vector2(12,-3.5f),new Vector2(15,-6),new Vector2(25,-6)},
        new[]{new Vector2(9,5),new Vector2(7,0),new Vector2(9,-6),new Vector2(15,-6),new Vector2(15,-12.5f)},
        new[]{new Vector2(-4,-12.5f),new Vector2(-4,-13)},
        new[]{new Vector2(-25,-12.5f),new Vector2(-25,-13)},
        new[]{new Vector2(-21,-6),new Vector2(-21,0),new Vector2(-24,5)},
        new[]{new Vector2(-4,-13),new Vector2(1,-7),new Vector2(2,0)}
    };
    static GameObject Root(string name,Transform parent,Vector2 position)
    { var root=new GameObject(name); root.transform.SetParent(parent,false); root.transform.position=position; return root; }
    static Transform Group(Transform parent,string name) => parent.Find(name)??Root(name,parent,Vector2.zero).transform;
    static void Clear(Transform parent) { foreach(Transform child in parent.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject); }
    static SpriteRenderer Draw(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order=-1000,Sprite sprite=null)
    {
        var root=Root(name,parent,position); var r=root.AddComponent<SpriteRenderer>(); r.sprite=sprite!=null?sprite:Tile;
        root.transform.localScale=new Vector3(size.x/r.sprite.bounds.size.x,size.y/r.sprite.bounds.size.y,1); r.color=color; r.sortingOrder=order; return r;
    }
    static float RoadDistance(Vector2 point)
    {
        float distance=float.PositiveInfinity;
        foreach(var road in Roads) for(int i=1;i<road.Length;i++) distance=Mathf.Min(distance,OpenFieldCombat.SegmentDistance(road[i-1],road[i],point));
        return distance;
    }
    static int Depth(float y) => 30-Mathf.RoundToInt(y*10);
    static void Tree(Transform parent,Vector2 point,float scale=1)
    {
        var r=Draw("Tree",parent,point,new Vector2(1.8f,2.3f)*scale,Color.white,Depth(point.y-scale),EditableMapBuilder.PreviewIcon(13));
        var obstacle=r.gameObject.AddComponent<MapObstacle>(); obstacle.size=new Vector2(.35f,.25f); obstacle.offset=new Vector2(0,-.35f);
    }
    static void Solid(string name,Transform parent,Vector2 point,Vector2 size,Color color)
    { var r=Draw(name,parent,point,size,color,Depth(point.y)); var obstacle=r.gameObject.AddComponent<MapObstacle>(); obstacle.size=Vector2.one; }
    static void Fence(Transform parent,Vector2 point,float width)
    {
        var root=Root("Fence section",parent,point).transform;
        Draw("Upper rail",root,point+Vector2.up*.15f,new Vector2(width,.1f),new Color(.57f,.38f,.2f),Depth(point.y));
        Draw("Lower rail",root,point-Vector2.up*.12f,new Vector2(width,.1f),new Color(.57f,.38f,.2f),Depth(point.y));
        for(float x=-width*.5f;x<=width*.5f+.01f;x+=width*.5f) Draw("Post",root,point+new Vector2(x,.05f),new Vector2(.15f,.65f),new Color(.68f,.47f,.25f),Depth(point.y)+1);
        var obstacle=root.gameObject.AddComponent<MapObstacle>(); obstacle.size=new Vector2(width,.24f);
    }
    static void Flower(Transform parent,Vector2 point,Color color)
    { Draw("Flowers",parent,point,Vector2.one*.65f,color,-972,EditableMapBuilder.PreviewIcon(14)); }
    static void MoveBuilding(Transform buildings,string name,Vector2 point)
    {
        var root=buildings.Find(name); if(root==null) throw new Exception("Missing landmark "+name); root.position=point;
        foreach(var r in root.GetComponentsInChildren<SpriteRenderer>()) { r.sortingOrder=Depth(point.y-1.55f); PrefabUtility.RecordPrefabInstancePropertyModifications(r); }
        PrefabUtility.RecordPrefabInstancePropertyModifications(root);
    }
    static void Label(FarmMapLayout map,string text,Vector2 point)
    { var label=map.GetComponentsInChildren<WorldLabel>().First(l=>l.text==text); label.transform.position=point; }
    static void Yard(Transform parent,Vector2 point,int index)
    {
        var yard=Root("House yard "+(index+1).ToString("00"),parent,point).transform;
        var house=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/TownHouse.prefab"),yard);
        house.name="Village house "+(index+1).ToString("00"); house.transform.position=point;
        Color[] colors={new Color(.85f,.95f,1),new Color(1,.82f,.72f),new Color(.9f,1,.8f),new Color(1,.91f,.68f)};
        foreach(var r in house.GetComponentsInChildren<SpriteRenderer>()) { r.color=colors[index%colors.Length]; r.sortingOrder=Depth(point.y-1.55f); PrefabUtility.RecordPrefabInstancePropertyModifications(r); }
        Draw("Front walk",yard,point+Vector2.down*2.4f,new Vector2(1.2f,1.6f),new Color(.76f,.69f,.52f),-991);
        Fence(yard,point+new Vector2(-2.65f,-3),2.6f); Fence(yard,point+new Vector2(2.65f,-3),2.6f);
        Fence(yard,point+new Vector2(-2.5f,2.6f),2.6f); Fence(yard,point+new Vector2(2.5f,2.6f),2.6f);
        for(int j=0;j<3;j++) Flower(yard,point+new Vector2(3.2f,-1+j*.8f),colors[(index+1)%colors.Length]);
        Tree(yard,point+new Vector2(-3.4f,1),.8f);
        Draw("Small garden bed",yard,point+new Vector2(-3,-1.4f),new Vector2(1.4f,.8f),new Color(.45f,.32f,.2f),-990);
        Flower(yard,point+new Vector2(-3,-1.4f),new Color(.85f,1,.7f));
        // Entry point is editor-only, so every yard can be checked without adding gameplay.
        Root("Walkable front gate",yard,point+new Vector2(0,-3.6f));
    }
    static void Build()
    {
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town); var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); var map=farm.Layout;
        if(map.transform.Find(Marker)!=null) return;
        var ground=Group(map.transform,"Ground and paths"); Clear(ground);
        var scenery=Group(map.transform,"Plaza and decorations"); Clear(scenery);
        var oldReserve=map.transform.Find("Future building space - east"); if(oldReserve!=null) UnityEngine.Object.DestroyImmediate(oldReserve.gameObject);
        map.worldBounds.transform.localScale=new Vector3(64,44,1); map.safeArea.transform.localScale=new Vector3(64,44,1); map.playerSpawn.position=new Vector2(-27,5.3f);
        var coast=Group(scenery,"Coast and promenade"); var greenery=Group(scenery,"Trees and flower beds"); var square=Group(scenery,"Town square");
        for(int x=-32;x<32;x++)
        {
            int shore=-17+Mathf.RoundToInt(Mathf.Sin(x*.14f)*1.3f);
            for(int y=-22;y<22;y++)
            {
                Vector2 p=new Vector2(x+.5f,y+.5f); bool water=y<shore, sand=y==shore || y==shore+1;
                bool plaza=Vector2.Distance(p,new Vector2(-2,3))<4; bool path=!water&&!sand&&(plaza||RoadDistance(p)<1.15f);
                Color c=water?Color.Lerp(new Color(.17f,.48f,.64f),new Color(.3f,.66f,.76f),(y+22)/7f):sand?new Color(.87f,.8f,.58f):path?new Color(.76f,.68f,.5f):Color.white;
                Draw(water?"Sea":sand?"Beach":path?"Village path":"Grass",ground,p,Vector2.one,c,-1000,water||sand||path?Tile:EditableMapBuilder.PreviewIcon(9)).gameObject.AddComponent<FarmGround>().canFarm=false;
                if(water && (x+y)%5==0) Draw("Wave",coast,p,new Vector2(.7f,.035f),new Color(.6f,.83f,.87f),-998);
            }
            var boundary=Root("Sea boundary",coast,new Vector2(x+.5f,-22+(shore+22)*.5f)).AddComponent<MapObstacle>(); boundary.size=new Vector2(1,shore+22);
        }
        var buildings=Group(map.transform,"Buildings");
        MoveBuilding(buildings,"TownGuild",new Vector2(-18,12)); MoveBuilding(buildings,"TownSmith",new Vector2(18,12));
        MoveBuilding(buildings,"TownHouse",new Vector2(-22,-3)); MoveBuilding(buildings,"TownGeneralStore",new Vector2(-13,-3));
        Label(map,"GUILD",new Vector2(-18,15)); Label(map,"SMITH",new Vector2(18,14.3f)); Label(map,"VILLAGE HOUSE",new Vector2(-22,-.7f)); Label(map,"GENERAL STORE",new Vector2(-13,-.7f));
        var residential=Group(map.transform,"Residential neighborhoods"); Clear(residential);
        for(int i=0;i<Homes.Length;i++) Yard(residential,Homes[i],i);
        Solid("Fountain basin",square,new Vector2(-2,3),new Vector2(2,1.4f),new Color(.49f,.55f,.59f));
        Draw("Fountain water",square,new Vector2(-2,3.15f),new Vector2(1.6f,.8f),new Color(.3f,.7f,.85f),Depth(3)+1);
        Draw("Fountain pillar",square,new Vector2(-2,3.6f),new Vector2(.4f,1),new Color(.76f,.78f,.75f),Depth(3)+2);
        foreach(var p in new[]{new Vector2(-5,1),new Vector2(1,5.5f),new Vector2(3,-9),new Vector2(20,-14.5f),new Vector2(-15,-14.5f)}) Solid("Bench",square,p,new Vector2(1.8f,.5f),new Color(.49f,.3f,.15f));
        foreach(var p in new[]{new Vector2(-7,3),new Vector2(3,4),new Vector2(-16,-6),new Vector2(6,-5),new Vector2(28,0),new Vector2(-9,-13)})
        {
            var lamp=Root("Village lamp",scenery,p).transform;
            Solid("Lamp base",lamp,p,new Vector2(.22f,.25f),new Color(.32f,.26f,.2f));
            Draw("Lamp post",lamp,p+Vector2.up*.7f,new Vector2(.16f,1.4f),new Color(.34f,.27f,.19f),Depth(p.y)+1);
            Draw("Lamp light",lamp,p+Vector2.up*1.4f,Vector2.one*.4f,new Color(1,.86f,.45f),Depth(p.y)+2);
        }
        var random=new System.Random(1021);
        for(int i=0;i<250;i++)
        {
            Vector2 p=new Vector2((float)random.NextDouble()*60-30,(float)random.NextDouble()*36-15);
            var landmarks=new[]{new Vector2(-18,12),new Vector2(18,12),new Vector2(-22,-3),new Vector2(-13,-3)};
            if(RoadDistance(p)<2.3f || Vector2.Distance(p,new Vector2(-2,3))<5 || Homes.Concat(landmarks).Any(h=>Mathf.Abs(h.x-p.x)<4.8f&&Mathf.Abs(h.y-p.y)<4)) continue;
            if(i%3==0) Tree(greenery,p,.8f+(float)random.NextDouble()*.4f); else Flower(greenery,p,new Color(1,.9f,.75f));
        }
        for(int x=-30;x<=30;x+=3) if(Mathf.Abs(x)>4) { Tree(greenery,new Vector2(x,21),1.2f); Tree(greenery,new Vector2(x+1,19.5f)); }
        foreach(var npc in map.GetComponentsInChildren<ShopNpc>()) npc.transform.position=npc.npcId=="town_guide"?new Vector2(-18,9):npc.npcId=="town_smith"?new Vector2(18,9):new Vector2(-22,-5.3f);
        foreach(var exit in map.GetComponentsInChildren<MapExit>())
        {
            Vector2 p; float angle=0;
            if(exit.destinationScene==WorldFrameworkSetup.Store) p=new Vector2(-13,-5.4f);
            else if(exit.destinationScene==CombatTestMapSetup.FarmScene) { p=exit.requireInteract?new Vector2(-31,-7):new Vector2(-31,6); angle=exit.requireInteract?0:180; }
            else { p=exit.requireInteract?new Vector2(1.8f,20):new Vector2(-1.8f,20); angle=exit.requireInteract?0:90; }
            exit.transform.SetPositionAndRotation(p,Quaternion.Euler(0,0,angle));
            foreach(var label in exit.GetComponentsInChildren<WorldLabel>()) label.transform.position=p+Vector2.up*1.6f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(exit.transform);
        }
        foreach(var arrival in map.GetComponentsInChildren<MapArrivalPoint>())
        {
            switch(arrival.arrivalId)
            {
                case "town_from_farm": arrival.transform.position=new Vector2(-27,5.3f); break;
                case "town_from_farm_door": arrival.transform.position=new Vector2(-28,-7); break;
                case "town_from_dungeon": arrival.transform.position=new Vector2(1.8f,17.5f); break;
                case "town_from_dungeon_path": arrival.transform.position=new Vector2(-1.8f,17.5f); break;
                case "town_from_store": arrival.transform.position=new Vector2(-13,-6.6f); break;
            }
        }
        Root(Marker,map.transform,Vector2.zero);
        EditorSceneManager.MarkSceneDirty(farm.gameObject.scene); EditorSceneManager.SaveScene(farm.gameObject.scene);
    }
    // One-time authoring entry retained for fixtures; no production menu.
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before applying the town layout.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)) throw new Exception("Save open scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try { Build(); AssetDatabase.SaveAssets(); Debug.Log("SPACIOUS_TOWN_APPLIED"); }
        finally { if(setup.Any(scene=>scene.isLoaded&&scene.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene); }
    }
    public static void Verify()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use isolated project and saves.");
        Apply(); WorldConnectionValidator.Validate();
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town); var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); var collision=new MapCollision(farm);
        var targets=farm.Layout.GetComponentsInChildren<MapArrivalPoint>().Select(a=>(Vector2)a.transform.position)
            .Concat(farm.Layout.GetComponentsInChildren<MapExit>().Select(e=>(Vector2)e.transform.position))
            .Concat(farm.Layout.GetComponentsInChildren<ShopNpc>().Select(n=>(Vector2)n.transform.position+Vector2.down))
            .Concat(Homes.Select(p=>p+new Vector2(0,-3.6f))).ToArray();
        foreach(var target in targets)
        {
            var path=collision.FindPath(farm.SpawnPosition,target,farm.PlayerRadius);
            if(!collision.CanStand(target,farm.PlayerRadius)||(Vector2.Distance(farm.SpawnPosition,target)>.3f&&(path.Count==0||Vector2.Distance(path.Last(),target)>.3f))) throw new Exception("Spacious town path blocked: "+target);
        }
        Debug.Log("SPACIOUS_TOWN_PATHS_SUCCESS: "+targets.Length+" routes, nine additional house gates included.");
        WorldFrameworkCheck.Run();
    }
}
