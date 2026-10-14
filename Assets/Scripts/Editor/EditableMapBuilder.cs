using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class EditableMapBuilder
{
    const string Folder="Assets/Prefabs/Map";
    static FarmArt art;
    static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        int separator=path.LastIndexOf('/'); EnsureFolder(path.Substring(0,separator)); AssetDatabase.CreateFolder(path.Substring(0,separator),path.Substring(separator+1));
    }
    static Sprite Icon(int index)
    {
        string path="Assets/Art/MapSprites/Icon"+index+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path); if(existing!=null) return existing;
        var sprite=art.Get(index);
        if(index>=16) AssetDatabase.CreateAsset(sprite.texture,"Assets/Art/MapSprites/Texture"+index+".asset");
        AssetDatabase.CreateAsset(sprite,path); return sprite;
    }
    public static Sprite PreviewIcon(int index)
    {
        EnsureFolder("Assets/Art/MapSprites"); art ??= new FarmArt(); return Icon(index);
    }
    static GameObject Prefab(string name,int icon,int order,Color color,Action<GameObject> configure)
    {
        string path=Folder+"/"+name+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(existing!=null) return existing;
        var root=new GameObject(name);
        if(icon>=0) { var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=Icon(icon); renderer.sortingOrder=order; renderer.color=color; }
        configure?.Invoke(root); var prefab=PrefabUtility.SaveAsPrefabAsset(root,path); UnityEngine.Object.DestroyImmediate(root); return prefab;
    }
    static Transform Group(string name,Transform parent)
    {
        var root=new GameObject(name); root.transform.SetParent(parent,false); return root.transform;
    }
    static GameObject Place(GameObject prefab,Transform parent,Vector2 point,Vector2 size)
    {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent); root.transform.position=point; root.transform.localScale=new Vector3(size.x,size.y,1); return root;
    }
    [MenuItem("Tools/Everything Grows/Make current map editable")]
    public static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play before editing the map.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var farm=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmGame>(true)).FirstOrDefault();
        if(farm==null) throw new Exception("The active scene needs a FarmGame component.");
        if(farm.Layout!=null) { Selection.activeGameObject=farm.Layout.gameObject; Debug.Log("EDITABLE_MAP_ALREADY_EXISTS"); return; }
        art=new FarmArt(); if(!art.Available) throw new Exception("FarmAtlas missing.");
        EnsureFolder(Folder); EnsureFolder("Assets/Art/MapSprites");
        var ground=Prefab("GroundTile",9,-100,Color.white,root=>root.AddComponent<FarmGround>());
        var path=Prefab("PathTile",10,-94,new Color(.8f,.75f,.6f),null);
        var flower=Prefab("Flower",14,-50,Color.white,null);
        Prefab("TreeDecoration",13,0,Color.white,null);
        var area=Prefab("Area",-1,0,Color.white,root=>root.AddComponent<MapArea>());
        var player=Prefab("PlayerSpawn",0,20,Color.white,null);
        var chaser=Prefab("ChaserSpawn",20,20,Color.white,root=>root.AddComponent<EnemyPlacement>());
        var spitter=Prefab("SpitterSpawn",21,20,Color.white,root=>root.AddComponent<EnemyPlacement>().ranged=true);
        string[] names={"Shovel","WateringCan","Stone","Cloud","Sword","Bow"};
        var itemPrefabs=new GameObject[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var item=AssetDatabase.LoadAssetAtPath<PlantDefinition>((i<4?"Assets/Settings/":"Assets/Resources/Equipment/")+names[i]+".asset");
            if(item==null) throw new Exception("Missing item: "+names[i]);
            itemPrefabs[i]=Prefab((i==0?"Hoe":names[i])+"Pickup",art.ItemIndex(item),10,Color.white,root=>root.AddComponent<FarmItemPlacement>().item=item);
        }
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Make farm map editable");
        var mapRoot=new GameObject("EditableMap"); mapRoot.transform.SetParent(farm.transform,false); var layout=mapRoot.AddComponent<FarmMapLayout>();
        var groundGroup=Group("Ground",mapRoot.transform); var paths=Group("Paths",mapRoot.transform); var decor=Group("Decoration",mapRoot.transform);
        var items=Group("Items",mapRoot.transform); var enemies=Group("EnemySpawns",mapRoot.transform); var regions=Group("Regions",mapRoot.transform);
        for(int x=-24;x<=24;x++) for(int y=-16;y<=16;y++)
        {
            var tile=Place(ground,groundGroup,new Vector2(x,y),Vector2.one); tile.name="Ground_"+x+"_"+y;
            if(!OpenFieldCombat.Safe(new Vector2(x,y))) tile.GetComponent<SpriteRenderer>().color=new Color(.67f,.74f,.68f);
            if((Mathf.Abs(y)<=1 && Mathf.Abs(x)>6) || (Mathf.Abs(x)<=1 && Mathf.Abs(y)>4)) Place(path,paths,new Vector2(x,y),Vector2.one);
        }
        for(int x=-7;x<=7;x++) if(Mathf.Abs(x)>5) for(int y=-4;y<=4;y+=2) Place(flower,decor,new Vector2(x,y),Vector2.one*.7f);
        Vector2[] positions={new Vector2(-3,0),new Vector2(-2,0),new Vector2(2,-1),new Vector2(4,3),new Vector2(-1,1),new Vector2(1,1)};
        for(int i=0;i<positions.Length;i++) Place(itemPrefabs[i],items,positions[i],Vector2.one*.8f);
        Vector2[] camps={new Vector2(10,2),new Vector2(12,-3),new Vector2(-11,-2),new Vector2(-13,4),new Vector2(3,9),new Vector2(-4,-10)};
        for(int i=0;i<camps.Length;i++) Place(i%2==0?chaser:spitter,enemies,camps[i],Vector2.one*1.2f);
        layout.worldBounds=Place(area,regions,Vector2.zero,new Vector2(49,33)).GetComponent<MapArea>(); layout.worldBounds.name="WorldBounds"; layout.worldBounds.outline=Color.cyan;
        layout.safeArea=Place(area,regions,Vector2.zero,new Vector2(13,9)).GetComponent<MapArea>(); layout.safeArea.name="SafeArea";
        layout.playerSpawn=Place(player,regions,Vector2.zero,Vector2.one*1.1f).transform;
        Undo.RegisterCreatedObjectUndo(mapRoot,"Create editable map"); Undo.RecordObject(farm,"Connect map"); farm.ConfigureMap(layout); EditorUtility.SetDirty(farm);
        EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets();
        if(!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undo); Selection.activeGameObject=mapRoot;
        Debug.Log("EDITABLE_MAP_SETUP_SUCCESS");
    }
    public static void Validate()
    {
        if(!Application.isBatchMode) throw new Exception("Use an isolated project.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity"); Setup();
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); var map=farm.Layout;
        var item=map.GetComponentsInChildren<FarmItemPlacement>().First(); item.transform.position=new Vector3(3,2,0);
        map.playerSpawn.position=new Vector3(2,1,0); map.safeArea.transform.position=new Vector3(3,1,0);
        var enemy=map.GetComponentsInChildren<EnemyPlacement>().First(); UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        farm.Initialize();
        void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
        Check(farm.PlayerPosition==new Vector2(2,1),"Player spawn ignored");
        Check(farm.WorldPieceCount==6 && farm.DetachAt(new Vector2(3,2)),"Authored items ignored or duplicated");
        Check(farm.Combat.EnemyCount==5,"Deleted spawn regenerated");
        Check(farm.IsSafe(new Vector2(8,1)) && !farm.IsSafe(new Vector2(-5,1)),"Moved safe zone ignored");
        Check(!farm.Till(new Vector2Int(40,40)),"World bounds ignored");
        var ground=map.GetComponentsInChildren<FarmGround>().First(tile=>tile.transform.position==new Vector3(0,0,0));
        UnityEngine.Object.DestroyImmediate(ground.gameObject); Check(!farm.Till(Vector2Int.zero),"Deleted ground still farmable");
        Check(farm.Till(new Vector2Int(2,2)),"Authored ground not farmable");
        Debug.Log("EDITABLE_MAP_VALIDATION_SUCCESS");
    }
}
