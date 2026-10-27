using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// Explicit authoring pass. Scene objects remain editable; never runs on startup or Play.
public static class ForestFieldLayout
{
    const string Marker="Forest field layout 2026-10-27";
    static Vector2 V(float x,float y)=>new(x,y);
    internal static readonly Vector2[][] Trails={
        new[]{V(24,-5),V(21,-5),V(17,-10),V(8,-12),V(-6,-11),V(-12,-10),V(-15,-4),V(-14,2),V(-12,8),V(-10,13),V(-4,16),V(4,15),V(12,12),V(15,8),V(10,6)},
        new[]{V(16,3),V(20,-1),V(19,-6),V(14,-10),V(8,-12)}
    };
    static float Road(Vector2 p)=>Trails.Min(t=>Distance(p,t));
    static bool Pond(Vector2 p)=>Mathf.Pow((p.x+20)/4,2)+Mathf.Pow((p.y+10)/6,2)<1;
    static bool Clearing(Vector2 p)=>Vector2.Distance(p,V(-12,-10))<5 || Vector2.Distance(p,V(-14,2))<4 || Vector2.Distance(p,V(-10,13))<4 || Vector2.Distance(p,V(10,3))<6;
    static void Sign(Transform parent,string text,Vector2 p)=>Root(text,parent,p).AddComponent<WorldLabel>().text=text;
    public static void Author()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch authoring only.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        if(!Apply())throw new InvalidOperationException("Forest has unsaved changes or Play is active.");
        Debug.Log("FOREST_FIELD_AUTHORED");
    }
    public static bool Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
        var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(TriangleWorldSetup.Forest);
        bool opened=!scene.IsValid() || !scene.isLoaded;
        if(!opened && scene.isDirty)return false;
        if(opened)scene=EditorSceneManager.OpenScene(TriangleWorldSetup.Forest,OpenSceneMode.Additive);
        try
        {
            var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();var map=farm.Layout;
            if(map.transform.Find(Marker)!=null)return true;
            foreach(string name in new[]{"Ground and paths","Trees and rocks","Abandoned clearing"})
            {var old=map.transform.Find(name);if(old!=null)old.gameObject.SetActive(false);}
            map.worldBounds.transform.localScale=new Vector3(52,44,1);
            map.safeArea.transform.localScale=new Vector3(52,44,1);map.showSafeMarkers=false;
            map.playerSpawn.position=V(21,-5);
            var arrival=farm.GetComponentsInChildren<MapArrivalPoint>().Single(a=>a.arrivalId=="forest_from_field");arrival.transform.position=V(21,-5);
            var exit=farm.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==TriangleWorldSetup.Field);
            var delta=V(24,-5)-(Vector2)exit.transform.position;exit.transform.position+= (Vector3)delta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(exit.transform);
            var ground=Group(map.transform,"Ground");var scenery=Group(map.transform,"Decoration");
            var water=Group(scenery,"Waterside - open camp space");var ruins=Group(scenery,"Abandoned campsite");
            var grove=Group(scenery,"Strange grove - one-time find");var boss=Group(scenery,"Boss clearing");var forest=Group(scenery,"Forest buffers");
            var random=new System.Random(271027);
            for(int x=-26;x<26;x++)for(int y=-22;y<22;y++)
            {
                Vector2 p=V(x+.5f,y+.5f);bool wet=Pond(p),path=Road(p)<1.05f;
                var cell=Root(wet?"Water":path?"Trail":"Grass",ground,p);cell.AddComponent<FarmGround>().canFarm=!wet;
                Draw("Surface",cell.transform,p,Vector2.one,wet?new Color(.24f,.54f,.67f):path?new Color(.71f,.62f,.4f):Color.white,-1000,wet||path?Tile:EditableMapBuilder.PreviewIcon(9));
                if(!wet && !path && !Clearing(p) && Road(p+Vector2.down*.75f)>2.6f && random.NextDouble()<.085)
                    Tree(forest,p,.9f+(float)random.NextDouble()*.4f);
            }
            // Merge water collision by column; shore is walkable/farmable, pond is not.
            for(int x=-24;x<-16;x++)
            {
                var ys=Enumerable.Range(-16,12).Where(y=>Pond(V(x+.5f,y+.5f))).ToArray();
                if(ys.Length>0)Block(water,"Pond collision",V(x+.5f,(ys.First()+ys.Last()+1)*.5f),V(1,ys.Length));
            }
            // Small landmarks reuse current project sprites and collision primitives.
            Solid(ruins,"Fallen log",V(-17,3),V(2.3f,.55f),new Color(.4f,.25f,.13f));
            Solid(ruins,"Broken supply crate",V(-12,4),V(1.1f,.9f),new Color(.57f,.36f,.18f));
            Draw("Cold firepit",ruins,V(-15,1),V(1.2f,.75f),new Color(.28f,.27f,.25f),-995);
            Draw("Tent canvas",ruins,V(-16,5),V(2.3f,1.5f),new Color(.7f,.51f,.32f),Depth(5));
            Block(ruins,"Tent",V(-16,5),V(2.3f,1.2f));
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;var p=V(-10,13)+V(Mathf.Cos(a)*3,Mathf.Sin(a)*3);
                if(Road(p)<1.9f)continue;
                Draw("Unusual pale stone",grove,p,V(.7f,.9f),new Color(.67f,.72f,.85f),Depth(p.y));
                Block(grove,"Stone",p,V(.55f,.55f));
            }
            Draw("Boss clearing ground",boss,V(10,3),V(10,9),new Color(.57f,.59f,.37f),-999);
            for(int i=0;i<5;i++){var p=V(5+i*2.3f,10.5f);if(Road(p+Vector2.down) > 2)Tree(boss,p,1.3f);}
            Sign(water,"물가 공터",V(-12,-7));Sign(ruins,"버려진 야영지",V(-14,6.5f));
            Sign(grove,"기묘한 돌무리",V(-10,17));Sign(boss,"숲의 안쪽",V(10,9));
            foreach(var placement in farm.GetComponentsInChildren<FarmItemPlacement>())
            {
                if(placement.forage==null)continue;
                placement.transform.position=placement.forage.name=="StoneFragments"?V(-13,2):V(-10,13);
                PrefabUtility.RecordPrefabInstancePropertyModifications(placement.transform);
            }
            Root(Marker,map.transform,Vector2.zero);
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Forest save failed.");
            return true;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);}
    }
    public static void ValidateAuthor(){Author();Check();WorldConnectionValidator.Validate();}
    public static void Check()
    {
        var scene=EditorSceneManager.OpenScene(TriangleWorldSetup.Forest);var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();
        if(farm.WorldBounds.size!=V(52,44))throw new Exception("Incorrect forest bounds");
        var collision=new MapCollision(farm);
        foreach(var trail in Trails)for(int i=1;i<trail.Length;i++)
            for(float t=0;t<=1;t+=.025f)if(!collision.CanStand(Vector2.Lerp(trail[i-1],trail[i],t),.35f))throw new Exception("Blocked trail: "+Vector2.Lerp(trail[i-1],trail[i],t));
        if(collision.CanStand(V(-20,-10),.35f)||farm.Layout.CanFarm(V(-20,-10)))throw new Exception("Pond must block farming and walking");
        if(!farm.Layout.CanFarm(V(-12,-10))||!collision.CanStand(V(-12,-10),.35f))throw new Exception("Camp must be usable");
        foreach(var p in farm.GetComponentsInChildren<FarmItemPlacement>().Where(p=>p.forage!=null))if(!collision.CanStand(p.transform.position,.35f))throw new Exception("Blocked forage");
        foreach(var p in farm.GetComponentsInChildren<FarmItemPlacement>())if(!collision.CanStand(p.transform.position,.25f))throw new Exception("Blocked pickup: "+p.name);
        foreach(var profile in farm.GetComponent<NpcManager>().npcs)foreach(var point in profile.visitLocations)if(!collision.CanStand(point.position,.35f))throw new Exception("Blocked merchant location");
        if(farm.GetComponentsInChildren<EnemyPlacement>().Count(e=>e.monsterType!=null && e.monsterType.isBoss)!=1)throw new Exception("Boss missing");
        if(farm.GetComponentsInChildren<MapExit>().Any(e=>e.requireInteract))throw new Exception("Exterior route must be automatic");
        Debug.Log("FOREST_FIELD_CHECK_PASSED");
    }
}


