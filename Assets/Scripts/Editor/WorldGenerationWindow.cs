using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public sealed class WorldGenerationWindow : EditorWindow
{
    const string SettingsPath="Assets/Settings/WorldGeneration/Default.asset",Request="Library/WorldGeneration.request",Result="Library/WorldGeneration.result";
    [SerializeField] WorldGenerationSettings settings;
    Editor inspector;string message;
    static WorldGenerationWindow(){if(!Application.isBatchMode)EditorApplication.update+=ApplyRequest;}
    [MenuItem("Tools/Everything Grows/월드 생성")]
    public static void Open()=>GetWindow<WorldGenerationWindow>("월드 생성");
    internal static WorldGenerationSettings Defaults()
    {
        var result=AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(SettingsPath);
        if(result==null){WorldSceneGenerator.EnsureFolder("Assets/Settings/WorldGeneration");result=CreateInstance<WorldGenerationSettings>();AssetDatabase.CreateAsset(result,SettingsPath);}
        return result;
    }
    static void ApplyRequest()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(Request);try{AuthorDefault();File.WriteAllText(Result,"SUCCESS");}catch(Exception e){File.WriteAllText(Result,e.ToString());Debug.LogException(e);}
    }
    public static void AuthorDefault()
    {
        if(!File.Exists(WorldSceneGenerator.DefaultWorld))WorldSceneGenerator.Generate(Defaults(),WorldSceneGenerator.DefaultWorld);
        var settings=Defaults();settings.scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(WorldSceneGenerator.DefaultWorld);EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
        Debug.Log("DEFAULT_GENERATED_WORLD_READY");
    }
    void OnEnable(){minSize=new Vector2(430,430);}
    void OnDisable(){if(inspector!=null)DestroyImmediate(inspector);}
    void OnGUI()
    {
        EditorGUILayout.HelpBox("저장된 ForestClearing을 바탕으로 지형·공터를 생성합니다. 항상 새 씬에 저장하며 기존 씬과 열린 작업을 덮어쓰지 않습니다. 실행 중 자동 생성은 없습니다.",MessageType.Info);
        using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if(settings==null)settings=Defaults();
            var next=(WorldGenerationSettings)EditorGUILayout.ObjectField("생성 설정",settings,typeof(WorldGenerationSettings),false);
            if(next!=settings){settings=next;if(inspector!=null)DestroyImmediate(inspector);inspector=null;}
            if(settings==null)return;
            Editor.CreateCachedEditor(settings,null,ref inspector);EditorGUI.BeginChangeCheck();inspector.OnInspectorGUI();if(EditorGUI.EndChangeCheck())AssetDatabase.SaveAssetIfDirty(settings);
            if(GUILayout.Button("새 월드 씬 생성",GUILayout.Height(32)))
            {
                try
                {
                    var path=WorldSceneGenerator.Generate(settings,WorldSceneGenerator.DefaultWorld);settings.scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);EditorUtility.SetDirty(settings);AssetDatabase.SaveAssetIfDirty(settings);
                    Selection.activeObject=settings.scene;EditorGUIUtility.PingObject(settings.scene);message="생성 완료: "+path;
                }
                catch(Exception e){message=e.Message;Debug.LogException(e);}
            }
            if(settings.scene!=null&&GUILayout.Button("생성한 씬 열기"))
            {if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(settings.scene));}
        }
        if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
        EditorGUILayout.HelpBox("seed: 같은 설정이면 같은 배치\nwidth / height: 40~96의 짝수\nwaterAmount: 물 지형의 크기\nforestAmount: 숲 영역 비중\ntreeDensity: 나무 배치 밀도\n생성 후 Ground의 Chunk와 Decoration을 Scene에서 직접 편집하세요. 새 변형 씬은 직접 열어 Play로 확인하며, 기본 새 게임은 GeneratedWorld를 사용합니다.",MessageType.None);
    }
}
