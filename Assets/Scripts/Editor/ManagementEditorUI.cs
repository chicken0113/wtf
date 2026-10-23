using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Small shared UI helpers for scene-backed management windows. Never saves or closes scenes implicitly.
internal static class ManagementEditorUI
{
    internal static bool Playing => EditorApplication.isPlayingOrWillChangePlaymode;
    internal static bool Matches(string value,string search) => string.IsNullOrWhiteSpace(search) ||
        (value??"").IndexOf(search.Trim(),StringComparison.OrdinalIgnoreCase)>=0;
    internal static T[] Components<T>(Scene scene) where T:Component => scene.IsValid() && scene.isLoaded?
        scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray():Array.Empty<T>();
    internal static Scene ScenePicker(ref string path)
    {
        var paths=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)
            .Concat(Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path)).Where(p=>!string.IsNullOrEmpty(p)).Distinct().ToArray();
        if(paths.Length==0) { EditorGUILayout.HelpBox("저장된 씬을 열어주세요.",MessageType.Info);return default; }
        if(string.IsNullOrEmpty(path))path=SceneManager.GetActiveScene().path;
        int index=Array.IndexOf(paths,path);if(index<0)index=0;
        using(new EditorGUI.DisabledScope(Playing))
        {
            index=EditorGUILayout.Popup("씬",index,paths.Select(p=>System.IO.Path.GetFileNameWithoutExtension(p)).ToArray());path=paths[index];
            var scene=SceneManager.GetSceneByPath(path);
            if(!scene.IsValid() || !scene.isLoaded)
            {
                EditorGUILayout.HelpBox("현재 씬을 유지하면서 선택한 씬을 추가로 엽니다.",MessageType.Info);
                if(GUILayout.Button("선택한 씬 열기"))scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            }
            return scene;
        }
    }
    internal static void SaveSceneButton(Scene scene)
    {
        using(new EditorGUI.DisabledScope(Playing || !scene.IsValid() || !scene.isLoaded))
            if(GUILayout.Button(scene.isDirty?"이 씬 저장 *":"이 씬 저장"))EditorSceneManager.SaveScene(scene);
    }
    internal static void DrawIssues(EverythingGrows.SetupValidationReport report)
    {
        if(report==null)return;
        foreach(var issue in report.Issues)EditorGUILayout.HelpBox(issue.Message,issue.IsError?MessageType.Error:MessageType.Warning);
    }
}
