using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// Repaint roads in place so building instances, IDs and manual scene edits remain intact.
public static class VillageRoadRefinement
{
    const string Marker="Village road bends 2026-10-21";
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static Vector2[] Line(params float[] p)=>Enumerable.Range(0,p.Length/2).Select(i=>V(p[i*2],p[i*2+1])).ToArray();
    static readonly Vector2[][] StoneRoads={
        Line(-32,0,-26,0,-21,1.5f,-12,1.5f,-8,0,-4,0),
        Line(0,4,0,9,2,13,2,18,-.5f,22,-.5f,24,0,27,0,30),
        Line(0,-4,0,-8,-1.5f,-11,-1.5f,-15,0,-18,0,-24),
        Line(-32,-17,-26,-17,-23,-16.5f,-18,-17.5f,-11,-17.5f,-6,-16,-1.5f,-16)
    };
    static readonly Vector2[][] DirtRoads={
        Line(-17,1.5f,-18.5f,4.5f,-18.5f,7,-17,9.5f,-17,12),
        Line(4,5,9,5,12,5,14,7,16,7,19,5,24,5,30,5),
        Line(18,5.4f,16.5f,8,16.5f,10,18,12),
        Line(12,6.5f,12,1.5f,13.5f,-2,13.5f,-7,11.5f,-11,11.5f,-16,13,-20),
        Line(0,-20,5,-21,10,-21,14,-20,22,-20,23,-19,23,-16,30.8f,-16),
        Line(12,0,16,-.5f,21,-.5f,24,-1),
        Line(20,.9f,20,-.5f),
        Line(-24,-23,-17,-23,-13,-22,-6,-22,0,-24,10,-23.5f,18,-23,24,-24),
        Line(-25,15,-27,16,-27,19,-25.5f,20,-23,21),
        Line(-25,15,-23,13.5f,-21,12.3f,-20,11.5f,-17,11.8f),
        Line(-23,-13,-23,-14.5f,-24,-16.7f),
        Line(-14,-13,-14,-15,-15,-17.5f),
        Line(-7,-12.9f,-7.5f,-14.5f,-6,-16),
        Line(-8,6,-8,5,-5.5f,4.8f,-4,4.8f),
        Line(7,-16.5f,8,-18,8,-21),
        Line(19,-19,19,-20)
    };
    static readonly Vector2[][] AllRoads=StoneRoads.Concat(DirtRoads).ToArray();
    static float RoadDistance(Vector2 p,Vector2[][] roads)=>roads.Min(r=>Distance(p,r));
    static float RoadDistance(Vector2 p)=>RoadDistance(p,AllRoads);
    static bool Square(Vector2 p)=>Mathf.Abs(p.x)<=4.8f&&Mathf.Abs(p.y)<=5.2f;
    static bool Water(Vector2 p)=>p.y<-25.5f+Mathf.Sin(p.x*.14f)*.5f||Mathf.Abs(p.x-(27+Mathf.Sin(p.y*.14f)*.8f))<1.25f;
    static void MoveTrees(FarmMapLayout map)
    {
        var greenery=map.transform.Find("Plaza and decorations/Gardens and trees");
        if(greenery==null)return;
        foreach(var trunk in greenery.GetComponentsInChildren<MapObstacle>().Where(o=>o.name=="Tree trunk"))
        {
            Vector2 original=trunk.transform.position;
            if(RoadDistance(original)>1.6f)continue;
            var art=greenery.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.name=="Tree"&&Vector2.Distance(r.transform.position,original+Vector2.up*.75f)<.05f);
            if(art==null)throw new Exception("Tree art and trunk are not paired.");
            bool moved=false;
            foreach(var offset in new[]{3f,4.5f,6f,8f}.SelectMany(radius=>Enumerable.Range(0,16).Select(i=>V(Mathf.Cos(i*Mathf.PI/8),Mathf.Sin(i*Mathf.PI/8))*radius)))
            {
                var candidate=original+offset;
                if(Water(candidate)||Square(candidate)||RoadDistance(candidate)<2||candidate.x< -30||candidate.x>30||candidate.y< -23||candidate.y>28)continue;
                if(!new MapCollision(UnityEngine.Object.FindFirstObjectByType<FarmGame>()).CanStand(candidate,.9f))continue;
                trunk.transform.position=candidate;art.transform.position=(Vector2)art.transform.position+offset;art.sortingOrder=Depth(art.transform.position.y-1);moved=true;break;
            }
            if(!moved)throw new Exception("No clear roadside position for tree at "+original);
        }
    }
    [MenuItem("Tools/Everything Grows/World/Apply village road bends")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before authoring.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save open scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var map=farm.Layout;
            if(map.transform.Find(Marker)!=null)return;
            var ground=map.transform.Find("Ground and paths");if(ground==null)throw new Exception("Authored ground missing.");
            var grass=EditableMapBuilder.PreviewIcon(9);
            foreach(var r in ground.GetComponentsInChildren<SpriteRenderer>())
            {
                if(r.name=="Water"||r.name=="Bridge deck")continue;
                Vector2 p=r.transform.position;
                bool stone=Square(p)||RoadDistance(p,StoneRoads)<.9f;
                float width=.88f+.1f*Mathf.Sin(p.x*.3f+p.y*.2f);
                bool coast=p.y< -24.1f+Mathf.Sin(p.x*.14f)*.5f;
                bool bank=Mathf.Abs(p.x-(27+Mathf.Sin(p.y*.14f)*.8f))<2;
                bool dirt=RoadDistance(p,DirtRoads)<width||coast||bank;
                r.name=stone?"Stone paving":dirt?"Dirt path":"Grass";r.sprite=stone||dirt?Tile:grass;
                r.color=stone?new Color(.58f,.56f,.47f):dirt?new Color(.86f,.73f,.45f):Color.white;
                r.transform.localScale=new Vector3(1/r.sprite.bounds.size.x,1/r.sprite.bounds.size.y,1);
            }
            MoveTrees(map);Root(Marker,map.transform,Vector2.zero);EditorSceneManager.MarkSceneDirty(farm.gameObject.scene);EditorSceneManager.SaveScene(farm.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("VILLAGE_ROAD_BENDS_APPLIED");
        }
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
    public static void Verify()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        Apply();EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var collision=new MapCollision(farm);Capture(farm,"RoadBendsOverview.png");
        // Check the actual planned centerlines, not just routes across otherwise walkable grass.
        foreach(var road in AllRoads)for(int i=1;i<road.Length;i++)
        {
            var start=road[i-1];var end=road[i];
            // Road artwork may continue past the map boundary; check the walkable section only.
            if(!farm.WorldBounds.Contains(start)||!farm.WorldBounds.Contains(end))continue;
            if(start.x<=-31.5f||end.x<=-31.5f||end.y>=29.5f)continue;
            if(!collision.CanStand(start,farm.PlayerRadius)||!collision.CanStand(end,farm.PlayerRadius)||collision.Trace(start,end,farm.PlayerRadius)<1)throw new Exception("Bent road blocked: "+start+" -> "+end);
        }
        Debug.Log("VILLAGE_ROAD_CENTERLINES_SUCCESS");SketchVillageLayout.Verify();
    }
}
