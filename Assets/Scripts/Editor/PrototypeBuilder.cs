using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class PrototypeBuilder
{
    [MenuItem("Tools/Everything Grows/Create prototype")]
    public static void Create()
    {
        var definitions=new PlantDefinition[5];
        var names=new[]{"Hoe","Watering Can","Stone","Cloud","Inventory Slot"};
        var effects=new[]{GrowthEffect.Dig,GrowthEffect.Water,GrowthEffect.Produce,GrowthEffect.Water,GrowthEffect.Storage};
        var colors=new[]{new Color(.75f,.7f,.55f),new Color(.3f,.7f,.9f),new Color(.55f,.57f,.6f),Color.white,new Color(.85f,.6f,.9f)};
        for(int i=0;i<5;i++)
        {
            string path="Assets/Settings/"+names[i].Replace(" ","")+".asset";
            definitions[i]=AssetDatabase.LoadAssetAtPath<PlantDefinition>(path);
            if(definitions[i]!=null) continue;
            var data=ScriptableObject.CreateInstance<PlantDefinition>(); data.label=names[i]; data.effect=effects[i]; data.color=colors[i]; data.effectRadius=i==3?3:1.5f;
            AssetDatabase.CreateAsset(data,path); definitions[i]=data;
        }
        const string scenePath="Assets/Scenes/Garden.unity";
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)==null)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var camera=new GameObject("Main Camera"); camera.tag="MainCamera";
            camera.transform.position=new Vector3(0,0,-10); var lens=camera.AddComponent<Camera>(); lens.orthographic=true; lens.orthographicSize=6.7f; lens.backgroundColor=new Color(.12f,.22f,.22f);
            var game=new GameObject("Farm").AddComponent<FarmGame>(); game.Configure(definitions);
            EditorSceneManager.SaveScene(scene,scenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scenePath,true)};
        }
        EditorSettings.defaultBehaviorMode=EditorBehaviorMode.Mode2D;
        AssetDatabase.SaveAssets(); Debug.Log("EVERYTHING_GROWS_PROJECT_CREATED");
    }
    public static void CreateAndValidate()
    {
        Create();
        if(!Application.isBatchMode) throw new InvalidOperationException("Use batchmode for isolated checks.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var definitions=new PlantDefinition[5]; var names=new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"};
        for(int i=0;i<5;i++) definitions[i]=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+names[i]+".asset");
        var root=new GameObject("Validation Farm"); var farm=root.AddComponent<FarmGame>(); farm.Configure(definitions); farm.Initialize(false);
        void Check(bool value,string message) { if(!value) throw new Exception(message); }
        Check(farm.Capacity==5 && farm.InventoryCount==0,"Initial storage");
        Check(farm.DetachAt(new Vector2(-3,0)),"Tool pickup");
        Check(!farm.PlantSelected(new Vector2Int(0,1)) && farm.InventoryCount==1,"Untilled planting consumed item");
        Check(farm.Till(new Vector2Int(0,1)) && farm.PlantSelected(new Vector2Int(0,1)),"Tool planting");
        Check(farm.AddItem(definitions[2]),"Second item pickup");
        Check(!farm.PlantSelected(new Vector2Int(0,1)) && farm.InventoryCount==1,"Occupied planting consumed item");
        Check(farm.Till(new Vector2Int(2,2)) && farm.PlantSelected(new Vector2Int(2,2)),"Second planting");
        UnityEngine.Object.DestroyImmediate(root);
        root=new GameObject("Result validation"); farm=root.AddComponent<FarmGame>(); farm.Configure(definitions); farm.Initialize(false);
        Check(farm.WorldPieceCount==6 && farm.DetachAt(new Vector2(4,3)) && farm.WorldPieceCount==5,"Background cloud was not actually removed");
        Check(farm.Till(new Vector2Int(4,3)) && farm.PlantSelected(new Vector2Int(4,3)),"Background planting");
        Check(farm.DetachSlot() && farm.Capacity==4 && farm.InventoryCount==1,"UI slot removal does not reduce actual storage");
        Check(farm.Till(Vector2Int.zero) && farm.PlantSelected(Vector2Int.zero),"Slot planting");
        farm.Tick(20); Check(farm.StorageBonus==0,"Dry growth");
        farm.Water(Vector2.zero,1); farm.Tick(6); Check(farm.Capacity==7 && farm.StorageBonus==3,"Storage plant did not add storage");
        Check(farm.Uproot(Vector2Int.zero) && farm.Capacity==4 && farm.InventoryCount==1,"Uproot recovery and storage removal");
        Check(farm.RestoreSlot() && farm.Capacity==5 && farm.InventoryCount==0,"Inventory slot cannot be restored");
        Check(farm.AddItem(definitions[0]) && farm.PlantSelected(Vector2Int.zero),"Hoe planting");
        farm.Water(Vector2.zero,1); farm.Tick(6); Check(!farm.Till(Vector2Int.right),"Hoe did not dig adjacent soil");
        Check(farm.AddItem(definitions[2]) && farm.PlantSelected(Vector2Int.right),"Production plant setup");
        farm.Water(Vector2.right,1); farm.Tick(14); Check(farm.Collect(Vector2Int.right),"Stone production");
        Check(farm.AddItem(definitions[3]) && farm.PlantSelected(Vector2Int.left),"Cloud planting");
        farm.Water(Vector2.left,.1f); farm.Tick(6);
        Check(farm.AddItem(definitions[1]) && farm.PlantSelected(Vector2Int.up),"Water plant setup");
        farm.Tick(12); Check(farm.IsMature(Vector2Int.up) && farm.Uproot(Vector2Int.up),"Cloud irrigation / plant recovery");
        while(farm.AddItem(definitions[2])) {}
        Check(!farm.DetachSlot(),"Full inventory must not lose a slot or contents");
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("EVERYTHING_GROWS_VALIDATION_SUCCESS: detach UI with real cost, watering growth, automatic digging, production, irrigation, storage and recovery.");
    }
}
