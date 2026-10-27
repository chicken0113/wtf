using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class CombatTestMapSetup
{
    public const string FarmScene="Assets/Scenes/LegacyVillage/FirstGarden.unity", CombatScene="Assets/Scenes/LegacyVillage/CombatTest.unity";
    const string SpritePath="Assets/Art/MapSprites/TestBlock.png", Request="Library/CombatTestMap.request", Result="Library/CombatTestMap.result";
    static CombatTestMapSetup() { if(!Application.isBatchMode) EditorApplication.update+=Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        File.Delete(Request);
        try { ConnectFarm(); File.WriteAllText(Result,"SUCCESS"); }
        catch(Exception error) { Debug.LogException(error); File.WriteAllText(Result,error.ToString()); }
    }
    static GameObject Root(string name,Transform parent,Vector2 point)
    {
        var root=new GameObject(name); if(parent!=null) root.transform.SetParent(parent,false); root.transform.position=point; return root;
    }
    static Sprite BlockSprite()
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); if(sprite!=null) return sprite;
        var texture=new Texture2D(16,16,TextureFormat.RGBA32,false);
        for(int y=0;y<16;y++) for(int x=0;x<16;x++) texture.SetPixel(x,y,x==0 || y==0?new Color(.8f,.8f,.8f):Color.white);
        texture.Apply(); File.WriteAllBytes(SpritePath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(SpritePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(SpritePath); importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=16; importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }
    static GameObject Visual(string name,Transform parent,Vector2 point,Vector2 size,Color color,int order)
    {
        var root=Root(name,parent,point); root.transform.localScale=new Vector3(size.x,size.y,1);
        var model=root.AddComponent<SpriteRenderer>(); model.sprite=BlockSprite(); model.color=color; model.sortingOrder=order; return root;
    }
    static MapArea Area(string name,Transform parent,Vector2 point,Vector2 size,Color color)
    {
        var root=Root(name,parent,point); root.transform.localScale=new Vector3(size.x,size.y,1); var area=root.AddComponent<MapArea>(); area.outline=color; return area;
    }
    static void Obstacle(string name,Transform parent,Vector2 point,Vector2 size)
    {
        var root=Visual(name,parent,point,size,new Color(.34f,.39f,.49f),15); root.AddComponent<MapObstacle>();
        Visual("Top face",root.transform,point+Vector2.up*(size.y*.15f),size*new Vector2(.9f,.65f),new Color(.52f,.59f,.68f),16).transform.localScale=new Vector3(.9f,.65f,1);
    }
    static void Gateway(Transform parent,Vector2 point,string destination,string arrival,string name)
    {
        var root=Root(name,parent,point); var exit=root.AddComponent<MapExit>(); exit.destinationScene=destination; exit.arrivalId=arrival; exit.areaSize=new Vector2(1.8f,2.4f);
        Visual("Gate floor",root.transform,point,new Vector2(1.8f,2.4f),new Color(.4f,.75f,.68f),-70);
        Visual("Gate left",root.transform,point+Vector2.left,new Vector2(.25f,2.6f),new Color(.35f,.6f,.57f),20);
        Visual("Gate right",root.transform,point+Vector2.right,new Vector2(.25f,2.6f),new Color(.35f,.6f,.57f),20);
        Visual("Gate lintel",root.transform,point+Vector2.up*1.3f,new Vector2(2.25f,.25f),new Color(.65f,.85f,.7f),21);
    }
    static void Arrival(Transform parent,Vector2 point,string id)
    { var arrival=Root("Arrival - "+id,parent,point).AddComponent<MapArrivalPoint>(); arrival.arrivalId=id; }
    static void EnableScene(string path)
    {
        var entries=EditorBuildSettings.scenes.ToList(); var existing=entries.Find(entry=>entry.path==path);
        if(existing!=null) existing.enabled=true; else entries.Add(new EditorBuildSettingsScene(path,true)); EditorBuildSettings.scenes=entries.ToArray();
    }
    // One-time authoring entry retained for fixtures; no production menu.
    public static void ConnectFarm()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before connecting the farm.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=FarmScene) throw new Exception("Open FirstGarden before connecting the combat test.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(CombatScene)==null) throw new Exception("CombatTest scene is missing.");
        var farm=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmGame>()).FirstOrDefault();
        if(farm==null || farm.Layout==null) throw new Exception("Farm map is missing.");
        Undo.RecordObject(farm.Layout,"Make farm peaceful"); farm.Layout.combatSchedule=FarmMapLayout.CombatSchedule.Peaceful; EditorUtility.SetDirty(farm.Layout);
        if(farm.Layout.transform.Find("Combat Test Connection")==null)
        {
            Vector2 point=new Vector2(6,0);
            var collision=new MapCollision(farm);
            bool Free(Vector2 p) => collision.CanStand(p,1.4f) && collision.CanStand(p+Vector2.left*1.7f,.4f)
                && !farm.Layout.GetComponentsInChildren<StartingPlot>().Any(plot=>Vector2.Distance(plot.transform.position,p)<2);
            if(!Free(point))
            {
                bool found=false;
                for(float x=farm.WorldBounds.xMin+3;x<farm.WorldBounds.xMax-2 && !found;x+=2) for(float y=farm.WorldBounds.yMin+3;y<farm.WorldBounds.yMax-2;y+=2)
                    if(Free(new Vector2(x,y))) {point=new Vector2(x,y);found=true;break;}
                if(!found) throw new Exception("No clear position for the combat entrance.");
            }
            var group=Root("Combat Test Connection",farm.Layout.transform,Vector2.zero); Undo.RegisterCreatedObjectUndo(group,"Connect combat test");
            Gateway(group.transform,point,CombatScene,"combat_entry","Combat entrance - E"); Arrival(group.transform,point+Vector2.left*1.7f,"farm_from_combat");
        }
        MapSaveAuthoring.PrepareCurrentMap(); EnableScene(FarmScene); EnableScene(CombatScene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("COMBAT_FARM_CONNECTION_SUCCESS");
    }
    public static void Generate()
    {
        if(!Application.isBatchMode) throw new Exception("Generate in an isolated project copy.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(CombatScene)!=null) throw new Exception("CombatTest already exists; preserve manual map changes.");
        BlockSprite(); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Root("Combat Test",null,Vector2.zero); var farm=root.AddComponent<FarmGame>();
        farm.Configure(new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}.Select(name=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+name+".asset")).ToArray());
        var map=Root("EditableMap",root.transform,Vector2.zero).AddComponent<FarmMapLayout>(); farm.ConfigureMap(map);
        map.combatSchedule=FarmMapLayout.CombatSchedule.Always; map.dayNightLighting=false; map.showSafeMarkers=false;
        map.worldBounds=Area("World bounds",map.transform,Vector2.zero,new Vector2(26,18),Color.white);
        map.safeArea=Area("Entry safe area",map.transform,new Vector2(-9.5f,0),new Vector2(5,6),Color.green);
        map.playerSpawn=Root("Player spawn",map.transform,new Vector2(-8,0)).transform;
        var ground=Root("Ground",map.transform,Vector2.zero).transform;
        for(int x=-12;x<=12;x++) for(int y=-8;y<=8;y++)
        { var tile=Visual("Floor "+x+","+y,ground,new Vector2(x,y),Vector2.one,new Color(.24f,.28f,.32f),-100); tile.AddComponent<FarmGround>().canFarm=false; }
        Visual("Safe entry floor",ground,new Vector2(-9.5f,0),new Vector2(5,6),new Color(.23f,.42f,.36f),-90);
        var obstacles=Root("Obstacles",map.transform,Vector2.zero).transform;
        Obstacle("North wall",obstacles,new Vector2(0,8),new Vector2(26,1)); Obstacle("South wall",obstacles,new Vector2(0,-8),new Vector2(26,1));
        Obstacle("West wall",obstacles,new Vector2(-12,0),new Vector2(1,16)); Obstacle("East wall",obstacles,new Vector2(12,0),new Vector2(1,16));
        Obstacle("Pillar A",obstacles,new Vector2(-3,2),new Vector2(1.5f,2)); Obstacle("Pillar B",obstacles,new Vector2(3,-2),new Vector2(1.5f,2));
        Obstacle("Upper cover",obstacles,new Vector2(1,4.5f),new Vector2(4,1)); Obstacle("Lower cover",obstacles,new Vector2(-1,-3.5f),new Vector2(3,1));
        var enemies=Root("Enemies",map.transform,Vector2.zero).transform;
        foreach(bool ranged in new[]{false,true})
        {
            var enemy=Root(ranged?"Spitter spawn":"Chaser spawn",enemies,ranged?new Vector2(5,2):new Vector2(-1,0)); enemy.transform.localScale=Vector3.one*1.2f;
            enemy.AddComponent<EnemyPlacement>().ranged=ranged; var model=enemy.AddComponent<SpriteRenderer>(); model.sprite=EditableMapBuilder.PreviewIcon(ranged?21:20); model.sortingOrder=30;
        }
        var travel=Root("Travel",map.transform,Vector2.zero).transform;
        Gateway(travel,new Vector2(-10,0),FarmScene,"farm_from_combat","Return to farm - E"); Arrival(travel,new Vector2(-8,0),"combat_entry");
        var camera=Root("Main Camera",null,new Vector2(-6,0)).AddComponent<Camera>(); camera.tag="MainCamera"; camera.orthographic=true; camera.orthographicSize=4.8f; camera.transform.position=new Vector3(-6,0,-10); camera.backgroundColor=new Color(.1f,.13f,.18f);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),CombatScene); EnableScene(CombatScene);
        EditorSceneManager.OpenScene(FarmScene); ConnectFarm();
        Debug.Log("COMBAT_TEST_MAP_GENERATED");
    }
}
