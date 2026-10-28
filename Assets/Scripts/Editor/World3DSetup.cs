using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class World3DSetup
{
    const string Request="Library/World3D.request",Result="Library/World3D.result";
    static World3DSetup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        try{if(!Apply()){File.WriteAllText(Result,"WAITING_FOR_GENERATED_WORLD_SAVE");return;}File.Delete(Request);File.WriteAllText(Result,"SUCCESS");}catch(Exception e){File.Delete(Request);File.WriteAllText(Result,e.ToString());Debug.LogException(e);}
    }
    public static void Author(){if(!Apply())throw new InvalidOperationException("Save the dirty target scene before conversion.");}
    public static bool Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
        string path=WorldSceneGenerator.DefaultWorld;var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(!opened&&scene.isDirty)return false;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();if(farm.GetComponent<World3D>()!=null){AssignModels(farm.GetComponent<World3D>().models);return true;}
            SceneManager.SetActiveScene(scene);EditorSettings.defaultBehaviorMode=EditorBehaviorMode.Mode3D;Convert(scene,World3DModels.Ensure());EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("3D scene save failed");Debug.Log("WORLD_3D_CONVERTED");return true;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);}
    }
    static void AssignModels(WorldModelLibrary library)
    {
        foreach(var guid in AssetDatabase.FindAssets("t:PlantDefinition"))
        {
            var item=AssetDatabase.LoadAssetAtPath<PlantDefinition>(AssetDatabase.GUIDToAssetPath(guid));if(item.modelPrefab!=null)continue;
            item.modelPrefab=item.name=="Cloud"?library.Get(ModelKind3D.Cloud):item.name=="Stone"?library.Get(ModelKind3D.Stone):library.Item(item);EditorUtility.SetDirty(item);AssetDatabase.SaveAssetIfDirty(item);
        }
        foreach(var type in Resources.LoadAll<MonsterType>(""))if(type.modelPrefab==null){type.modelPrefab=library.Get(type.IsRanged(false)?ModelKind3D.RangedEnemy:ModelKind3D.Enemy);EditorUtility.SetDirty(type);AssetDatabase.SaveAssetIfDirty(type);}
    }
    internal static void Convert(Scene scene,WorldModelLibrary library)
    {
        var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();if(farm.GetComponent<World3D>()!=null)return;
        AssignModels(library);
        var world=farm.gameObject.AddComponent<World3D>();world.models=library;
        foreach(var renderer in farm.Layout.GetComponentsInChildren<SpriteRenderer>().ToArray())ConvertRenderer(world,renderer);
        foreach(var camera in ManagementEditorUI.Components<Camera>(scene))world.ConfigureCamera(camera,farm.SpawnPosition);
        var light=new GameObject("World sunlight");SceneManager.MoveGameObjectToScene(light,scene);var sun=light.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1;sun.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(30,-25,0);
        RenderSettings.ambientLight=new Color(.6f,.65f,.7f);
    }
    internal static void ConvertRenderer(World3D world,SpriteRenderer renderer)
    {
        var library=world.models;
        var item=renderer.GetComponent<FarmItemPlacement>();var npc=renderer.GetComponent<ShopNpc>();var enemy=renderer.GetComponent<EnemyPlacement>();
        if(item!=null){world.Item(renderer,item.DisplayItem);return;}
        if(npc!=null){var pose=world.Show(renderer,ModelKind3D.Merchant,true);pose.actor=true;pose.facing=Vector2.down;pose.Sync();return;}
        if(enemy!=null){world.Show(renderer,enemy.ranged?ModelKind3D.RangedEnemy:ModelKind3D.Enemy);return;}
        bool ground=renderer.GetComponentInParent<FarmGround>()!=null,tree=renderer.name=="Tree";
        var kind=ground?(renderer.transform.parent.name=="Water"?ModelKind3D.Water:renderer.transform.parent.name=="Forest"?ModelKind3D.ForestGround:ModelKind3D.Ground):tree?ModelKind3D.Tree:ModelKind3D.Stone;
        var model=(GameObject)PrefabUtility.InstantiatePrefab(library.Get(kind),renderer.transform);model.name="3D "+kind;
        var size=renderer.bounds.size;var p=renderer.transform.position;if(tree)p.y-=size.x*.416667f;
        model.transform.SetPositionAndRotation(p,World3D.ModelRotation);
        var parent=renderer.transform.lossyScale;
        var desired=ground?new Vector3(size.x,1,size.y):tree?new Vector3(size.x*.65f,size.y*.5f,size.x*.65f):new Vector3(size.x,size.y,size.x);
        model.transform.localScale=new Vector3(desired.x/Mathf.Max(.001f,Mathf.Abs(parent.x)),desired.y/Mathf.Max(.001f,Mathf.Abs(parent.z)),desired.z/Mathf.Max(.001f,Mathf.Abs(parent.y)));
        if(tree){model.layer=29;var box=model.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up;box.size=new Vector3(1.4f,2.2f,1.4f);}
        renderer.enabled=false;
    }

}
