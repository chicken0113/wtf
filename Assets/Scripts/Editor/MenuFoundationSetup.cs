using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class MenuFoundationSetup
{
    static MenuFoundationSetup() { if(!Application.isBatchMode)EditorApplication.delayCall+=Ensure; }
    public static void Ensure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) { if(!Application.isBatchMode)EditorApplication.delayCall+=Ensure;return; }
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))) { if(!Application.isBatchMode)EditorApplication.delayCall+=Ensure;return; }
        if(!File.Exists(GameMenu.TitleScene))
        {
            var active=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                var camera=new GameObject("Main Camera");SceneManager.MoveGameObjectToScene(camera,scene);camera.tag="MainCamera";var view=camera.AddComponent<Camera>();view.orthographic=true;view.backgroundColor=new Color(.04f,.07f,.07f);camera.transform.position=new Vector3(0,0,-10);
                EditorSceneManager.SaveScene(scene,GameMenu.TitleScene);
            }
            finally { EditorSceneManager.CloseScene(scene,true);if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active); }
        }
        var scenes=EditorBuildSettings.scenes;
        if(scenes.Length>0 && scenes[0].path==GameMenu.TitleScene && scenes[0].enabled)return;
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(GameMenu.TitleScene,true)}.Concat(scenes.Where(s=>s.path!=GameMenu.TitleScene)).ToArray();
    }
}
