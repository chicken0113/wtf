using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

// Explicit request only. Move assets with their GUIDs; never save or close loaded scenes.
[InitializeOnLoad]
public static class SceneOrganization
{
    const string Request="Library/SceneOrganization.request",Result="Library/SceneOrganization.result";
    static SceneOrganization(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(Request);
        try{Apply();File.WriteAllText(Result,"SUCCESS\n"+string.Join("\n",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>s.path+(s.isDirty?" *":""))));}
        catch(Exception e){File.WriteAllText(Result,e.ToString());Debug.LogException(e);}
    }
    static string ReplacePaths(string text)
    {foreach(var name in ScenePaths.Names)text=text.Replace(ScenePaths.Original(name),ScenePaths.Organized(name));return text;}
    static void Rewrite(string path)
    {string before=File.ReadAllText(path),after=ReplacePaths(before);if(before!=after)File.WriteAllText(path,after);}
    static void UpdateObject(UnityEngine.Object target)
    {
        if(target==null)return;
        var serialized=new SerializedObject(target);var property=serialized.GetIterator();bool changed=false;
        while(property.Next(true))if(property.propertyType==SerializedPropertyType.String)
        {string after=ScenePaths.Resolve(property.stringValue);if(after!=property.stringValue){property.stringValue=after;changed=true;}}
        if(changed){serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);if(target is Component c)EditorSceneManager.MarkSceneDirty(c.gameObject.scene);}
    }
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before organizing scenes.");
        foreach(string name in ScenePaths.Names)
        {
            bool old=File.Exists(ScenePaths.Original(name)),next=File.Exists(ScenePaths.Organized(name));
            if(old==next)throw new InvalidOperationException("Missing scene or destination conflict: "+name);
        }
        foreach(string folder in ScenePaths.Names.Select(ScenePaths.Folder).Distinct())
            if(!AssetDatabase.IsValidFolder("Assets/Scenes/"+folder))AssetDatabase.CreateFolder("Assets/Scenes",folder);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var name in ScenePaths.Names)if(File.Exists(ScenePaths.Original(name)))
            {
                string error=AssetDatabase.MoveAsset(ScenePaths.Original(name),ScenePaths.Organized(name));
                if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
            }
            foreach(string path in Directory.EnumerateFiles("Assets","*",SearchOption.AllDirectories).Where(p=>new[]{".cs",".unity",".asset",".prefab"}.Contains(Path.GetExtension(p))))Rewrite(path);
            // Keep loaded dirty scene contents and synchronize their string references, without saving.
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);if(!scene.isLoaded)continue;
                foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))UpdateObject(component);
            }
            // Also patch already loaded assets so a later editor save cannot restore old references.
            foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>())if(EditorUtility.IsPersistent(asset))UpdateObject(asset);
            Rewrite("ProjectSettings/EditorBuildSettings.asset");
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Select(s=>new EditorBuildSettingsScene(ScenePaths.Resolve(s.path),s.enabled)).ToArray();
        }
        finally{AssetDatabase.StopAssetEditing();AssetDatabase.Refresh();}
        Debug.Log("SCENES_ORGANIZED: scene GUIDs, build order and loaded unsaved work preserved.");
    }
    public static void Check()
    {
        Apply();
        foreach(var name in ScenePaths.Names)if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePaths.Organized(name))==null)throw new Exception("Missing organized scene: "+name);
        var sample=new GameSaveData {currentScene=ScenePaths.Original("ForestClearing"),houseReturnScene=ScenePaths.Original("FirstGarden"),respawn=new RespawnSave {scene=ScenePaths.Original("DungeonEntrance")},defaultRespawn=new RespawnSave {scene=ScenePaths.Original("FirstGarden")}};
        sample.maps.Add(new MapSave {scene=ScenePaths.Original("ForestClearing"),deathCaches={new DeathCacheSave {id="kept"}}});
        sample.nightVisits.Add(new NightVisitSave {scene=ScenePaths.Original("ForestClearing"),appears=true});sample.guildQuests.Add(new GuildQuestSave {targetScene=ScenePaths.Original("CombatTest"),progress=2});
        ScenePaths.Migrate(sample);string json=JsonUtility.ToJson(sample);ScenePaths.Migrate(sample);
        if(json!=JsonUtility.ToJson(sample)||sample.currentScene!=ScenePaths.Organized("ForestClearing")||sample.maps[0].deathCaches[0].id!="kept"||sample.guildQuests[0].progress!=2||sample.guildQuests[0].targetScene!=ScenePaths.Organized("CombatTest")||sample.respawn.scene!=ScenePaths.Organized("DungeonEntrance")||sample.nightVisits[0].scene!=ScenePaths.Organized("ForestClearing"))throw new Exception("Save path migration failed");
        Debug.Log("SCENE_ORGANIZATION_CHECK_PASSED");
    }
}
