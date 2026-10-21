using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public static class TownSketchLayout
{
    const string Marker="Sketch layout 2026-10-21";
    static Sprite Tile => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/MapSprites/TestBlock.png");
    static GameObject Root(string name,Transform parent,Vector2 point)
    { var root=new GameObject(name); root.transform.SetParent(parent,false); root.transform.position=point; return root; }
    static SpriteRenderer Draw(string name,Transform parent,Vector2 point,Vector2 size,Color color,int order=-100,Sprite sprite=null)
    {
        var root=Root(name,parent,point); var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=sprite!=null?sprite:Tile;
        root.transform.localScale=new Vector3(size.x/renderer.sprite.bounds.size.x,size.y/renderer.sprite.bounds.size.y,1); renderer.color=color; renderer.sortingOrder=order; return renderer;
    }
    static void Clear(Transform root)
    { if(root!=null) foreach(Transform child in root.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject); }
    static Transform Group(FarmMapLayout map,string name) => map.transform.Find(name)??Root(name,map.transform,Vector2.zero).transform;
    static void Label(Transform parent,string text,Vector2 point)
    { var label=Root(text,parent,point).AddComponent<WorldLabel>(); label.text=text; }
    static void Block(string name,Transform parent,Vector2 point,Vector2 size,Color color,int order)
    { Draw(name,parent,point,size,color,order).gameObject.AddComponent<MapObstacle>(); }
    static GameObject GuildPrefab()
    {
        const string path="Assets/Prefabs/Map/TownGuild.prefab"; var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(prefab!=null) return prefab;
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/TownHouse.prefab"));
        PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction); root.name="TownGuild";
        foreach(var model in root.GetComponentsInChildren<SpriteRenderer>()) { model.transform.localScale*=1.4f; model.color=new Color(.85f,.9f,1); }
        var obstacle=root.GetComponent<MapObstacle>(); obstacle.size=new Vector2(6.2f,2.1f); obstacle.offset=new Vector2(0,-1);
        prefab=PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root); return prefab;
    }
    static void MoveBuilding(Transform buildings,string name,Vector2 point)
    {
        var root=buildings.Find(name); if(root==null) throw new Exception("Building missing: "+name); root.position=point;
        foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>()) { renderer.sortingOrder=30-Mathf.RoundToInt((point.y-1.55f)*10); PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
    }
    static void MoveLabel(FarmMapLayout map,string text,Vector2 point)
    { foreach(var label in map.GetComponentsInChildren<WorldLabel>()) if(label.text==text) label.transform.position=point; }
    static void MoveArrival(FarmMapLayout map,string id,Vector2 point)
    { var arrival=map.GetComponentsInChildren<MapArrivalPoint>().Single(item=>item.arrivalId==id); arrival.transform.position=point; }
    static void MoveExit(MapExit exit,Vector2 point,float angle=0)
    {
        exit.transform.SetPositionAndRotation(point,Quaternion.Euler(0,0,angle));
        foreach(var label in exit.GetComponentsInChildren<WorldLabel>()) label.transform.position=point+Vector2.up*1.6f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(exit.transform);
    }
    static void Tree(Transform parent,Vector2 point,float size=1)
    {
        var renderer=Draw("Tree",parent,point,new Vector2(1.8f,2.3f)*size,Color.white,30-Mathf.RoundToInt((point.y-size)*10),EditableMapBuilder.PreviewIcon(13));
        var obstacle=renderer.gameObject.AddComponent<MapObstacle>(); obstacle.size=new Vector2(.35f,.25f); obstacle.offset=new Vector2(0,-.35f);
    }
    static void Bench(Transform parent,Vector2 point)
    { Block("Bench",parent,point,new Vector2(1.8f,.5f),new Color(.45f,.27f,.13f),30-Mathf.RoundToInt(point.y*10)); }
    static void Lamp(Transform parent,Vector2 point)
    {
        Block("Lamp base",parent,point,new Vector2(.22f,.25f),new Color(.32f,.26f,.2f),20);
        Draw("Lamp post",parent,point+Vector2.up*.65f,new Vector2(.18f,1.3f),new Color(.34f,.27f,.19f),21);
        Draw("Lamp light",parent,point+Vector2.up*1.35f,Vector2.one*.45f,new Color(1,.85f,.4f),22);
    }
    static float Segment(Vector2 point,Vector2 start,Vector2 end) => OpenFieldCombat.SegmentDistance(start,end,point);
    static bool PathAt(Vector2 point)
    {
        Vector2[][] roads={
            new[]{new Vector2(-19,6),new Vector2(-17,2)},new[]{new Vector2(-17,2),new Vector2(-3,2)},
            new[]{new Vector2(-1.5f,3),new Vector2(-1.5f,12.5f)},new[]{new Vector2(-1,2),new Vector2(10,3.7f)},
            new[]{new Vector2(-19,-5.8f),new Vector2(-5,-5.8f)},new[]{new Vector2(-5,-5.8f),new Vector2(-5,1)},
            new[]{new Vector2(-17,2),new Vector2(-17,-5.8f)},new[]{new Vector2(-11,3.7f),new Vector2(-11,2)} };
        return roads.Any(road=>Segment(point,road[0],road[1])<1.15f);
    }
    public static void ApplyTown()
    {
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town); var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); var map=farm.Layout;
        if(map.transform.Find(Marker)!=null) return;
        Root(Marker,map.transform,Vector2.zero);
        map.worldBounds.transform.localScale=new Vector3(40,28,1); map.safeArea.transform.localScale=new Vector3(40,28,1); map.playerSpawn.position=new Vector2(-16,5.3f);
        var ground=Group(map,"Ground and paths"); Clear(ground); var landscape=Group(map,"Plaza and decorations"); Clear(landscape);
        for(int x=-20;x<20;x++)
        {
            int shore=-9+Mathf.RoundToInt(Mathf.Sin(x*.2f)*.6f);
            for(int y=-14;y<14;y++)
            {
                Vector2 point=new Vector2(x+.5f,y+.5f); bool water=y<shore, sand=y==shore;
                bool plaza=Vector2.Distance(point,new Vector2(-1.5f,1))<3.3f; bool path=!water && !sand && (plaza || PathAt(point));
                Color color=water?Color.Lerp(new Color(.19f,.52f,.66f),new Color(.3f,.68f,.78f),(y+14)/6f):sand?new Color(.86f,.79f,.55f):path?new Color(.73f,.65f,.47f):Color.white;
                Draw(water?"Sea":sand?"Shore":path?"Path":"Grass",ground,point,Vector2.one,color,-100,water || sand || path?Tile:EditableMapBuilder.PreviewIcon(9)).gameObject.AddComponent<FarmGround>().canFarm=false;
                if(water && (x+y)%4==0) Draw("Wave",landscape,point,new Vector2(.65f,.035f),new Color(.57f,.82f,.86f),-98);
            }
            float waterHeight=shore+14; if(waterHeight>0) { var water=Root("Sea boundary",landscape,new Vector2(x+.5f,-14+waterHeight*.5f)).AddComponent<MapObstacle>(); water.size=new Vector2(1,waterHeight); }
        }
        for(int x=-19;x<=19;x+=2) if(Mathf.Abs(x)>4)
        { Block("Northern rock boundary",landscape,new Vector2(x,13.25f),new Vector2(2,1.5f),new Color(.43f,.47f,.46f),-70); Draw("Rock face",landscape,new Vector2(x,13.55f),new Vector2(1.4f,.65f),new Color(.58f,.61f,.57f),-69); }
        var buildings=Group(map,"Buildings"); MoveBuilding(buildings,"TownGeneralStore",new Vector2(-7,-3)); MoveBuilding(buildings,"TownSmith",new Vector2(10,7)); MoveBuilding(buildings,"TownHouse",new Vector2(-14,-3));
        var guild=(GameObject)PrefabUtility.InstantiatePrefab(GuildPrefab(),buildings); guild.transform.position=new Vector2(-11,6.5f);
        foreach(var renderer in guild.GetComponentsInChildren<SpriteRenderer>()) { renderer.sortingOrder=-14; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
        MoveLabel(map,"GENERAL STORE",new Vector2(-7,-.7f)); MoveLabel(map,"SMITH",new Vector2(10,9.3f)); MoveLabel(map,"VILLAGE HOUSE",new Vector2(-14,-.7f)); Label(buildings,"GUILD",new Vector2(-11,9.4f));
        Vector2 center=new Vector2(-1.5f,1); Block("Fountain base",landscape,center,new Vector2(1.8f,1.4f),new Color(.48f,.55f,.6f),27);
        Draw("Fountain water",landscape,center+Vector2.up*.15f,new Vector2(1.4f,.8f),new Color(.3f,.7f,.85f),28); Draw("Fountain center",landscape,center+Vector2.up*.4f,new Vector2(.4f,.8f),new Color(.7f,.75f,.8f),29);
        Bench(landscape,new Vector2(2,-1.5f)); Bench(landscape,new Vector2(3,3.8f)); Bench(landscape,new Vector2(12,-4));
        foreach(var point in new[]{new Vector2(-6,3),new Vector2(-4,-4),new Vector2(4,2.5f),new Vector2(-16,-6.5f)}) Lamp(landscape,point);
        foreach(var point in new[]{new Vector2(-17,10),new Vector2(-13,11),new Vector2(-8,11),new Vector2(6,11),new Vector2(14,10),new Vector2(18,8),new Vector2(18,4),new Vector2(17,-1),new Vector2(15,-5),new Vector2(6,-6.5f)}) Tree(landscape,point);
        foreach(var point in new[]{new Vector2(8,-3),new Vector2(10,-5),new Vector2(14,2),new Vector2(17,6),new Vector2(-18,-1),new Vector2(-10,-7)}) Draw("Wildflowers",landscape,point,Vector2.one*.55f,new Color(.95f,.85f,.6f),-75,EditableMapBuilder.PreviewIcon(14));
        var reserve=Root("Future building space - east",map.transform,new Vector2(10,-1)); var area=reserve.AddComponent<MapArea>(); area.outline=new Color(.6f,.8f,.6f,.5f); reserve.transform.localScale=new Vector3(8,5,1);
        foreach(var npc in map.GetComponentsInChildren<ShopNpc>())
        {
            npc.transform.position=npc.npcId=="town_guide"?new Vector2(-11,3.5f):npc.npcId=="town_smith"?new Vector2(10,4):new Vector2(-14,-5.3f);
        }
        var manager=farm.GetComponent<NpcManager>(); manager.npcs.Single(profile=>profile.id=="town_guide").greeting="The farm is to the west, and the dungeon road heads north. The general store and houses are southwest of the square. Welcome to the guild!";
        foreach(var exit in map.GetComponentsInChildren<MapExit>())
        {
            if(exit.destinationScene==WorldFrameworkSetup.Store)
            {
                MoveExit(exit,new Vector2(-7,-5.4f)); var mat=exit.transform.Find("Store doormat"); if(mat!=null) mat.position=exit.transform.position;
            }
            else if(exit.destinationScene==CombatTestMapSetup.FarmScene) MoveExit(exit,exit.requireInteract?new Vector2(-19,-5):new Vector2(-19,6),exit.requireInteract?0:180);
            else if(exit.destinationScene==CombatTestMapSetup.CombatScene) MoveExit(exit,exit.requireInteract?new Vector2(1.8f,12):new Vector2(-1.8f,12),exit.requireInteract?0:90);
        }
        MoveArrival(map,"town_from_farm",new Vector2(-16,5.3f)); MoveArrival(map,"town_from_farm_door",new Vector2(-17,-6));
        MoveArrival(map,"town_from_dungeon",new Vector2(1.8f,9.7f)); MoveArrival(map,"town_from_dungeon_path",new Vector2(-1.8f,9.7f)); MoveArrival(map,"town_from_store",new Vector2(-7,-6.6f));
        EditorSceneManager.MarkSceneDirty(farm.gameObject.scene); EditorSceneManager.SaveScene(farm.gameObject.scene);
    }
    static void Shortcut(string scenePath,Vector2 exitPoint,Vector2 arrivalPoint,string target,string targetId,string localId,float rotation)
    {
        EditorSceneManager.OpenScene(scenePath); var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); if(farm.Layout.transform.Find("Farm Dungeon Shortcut")!=null) return;
        var group=Root("Farm Dungeon Shortcut",farm.Layout.transform,Vector2.zero);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapExitTypesSetup.TouchPath),group.transform); root.transform.SetPositionAndRotation(exitPoint,Quaternion.Euler(0,0,rotation));
        var exit=root.GetComponent<MapExit>(); exit.destinationScene=target; exit.arrivalId=targetId; PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
        var arrival=Root("Shortcut arrival",group.transform,arrivalPoint).AddComponent<MapArrivalPoint>(); arrival.arrivalId=localId;
        Label(root.transform,target==CombatTestMapSetup.FarmScene?"Farm path":"Dungeon path",exitPoint+Vector2.up*1.5f);
        EditorSceneManager.MarkSceneDirty(farm.gameObject.scene); EditorSceneManager.SaveScene(farm.gameObject.scene);
    }
    [MenuItem("Tools/Everything Grows/World/Apply approved town sketch")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before applying the sketch.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(index=>SceneManager.GetSceneAt(index).isDirty)) throw new Exception("Save open scenes before applying the sketch.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            ApplyTown();
            Shortcut(CombatTestMapSetup.FarmScene,new Vector2(-3,12),new Vector2(-3,10),CombatTestMapSetup.CombatScene,"combat_from_farm","farm_from_dungeon",90);
            Shortcut(CombatTestMapSetup.CombatScene,new Vector2(-10,-2.5f),new Vector2(-8,-2.5f),CombatTestMapSetup.FarmScene,"farm_from_dungeon","combat_from_farm",180);
            AssetDatabase.SaveAssets(); Debug.Log("TOWN_SKETCH_APPLIED");
        }
        finally { if(setup.Any(scene=>scene.isLoaded && scene.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup); else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene); }
    }
}
