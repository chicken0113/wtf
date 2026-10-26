using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
public sealed class SpawnRegionWindow : EditorWindow
{
    SpawnRegion selected;
    SpawnRegion[] regions=Array.Empty<SpawnRegion>();
    string search="";
    Vector2 listScroll,detailsScroll;
    public static void Open()=>GetWindow<SpawnRegionWindow>("몬스터 지역 설정");
    void OnEnable(){minSize=new Vector2(680,420);Refresh();EditorApplication.projectChanged+=Refresh;Undo.undoRedoPerformed+=OnUndo;SceneView.duringSceneGui+=DrawScene;}
    void OnDisable(){EditorApplication.projectChanged-=Refresh;Undo.undoRedoPerformed-=OnUndo;SceneView.duringSceneGui-=DrawScene;}
    void OnUndo(){if(selected!=null)AssetDatabase.SaveAssetIfDirty(selected);Refresh();SceneView.RepaintAll();}
    void Refresh(){regions=AssetDatabase.FindAssets("t:SpawnRegion").Select(g=>AssetDatabase.LoadAssetAtPath<SpawnRegion>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(r=>r.label).ToArray();if(selected==null)selected=regions.FirstOrDefault();Repaint();}
    internal static SpawnRegion CreateRegion()
    {
        if(ManagementEditorUI.Playing)throw new InvalidOperationException("Play를 정지하세요.");
        const string folder="Assets/Resources/SpawnRegions";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","SpawnRegions");
        var region=CreateInstance<SpawnRegion>();region.label="새 지역";region.scenePath=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var path=AssetDatabase.GenerateUniqueAssetPath(folder+"/NewRegion.asset");AssetDatabase.CreateAsset(region,path);region.id=AssetDatabase.AssetPathToGUID(path);EditorUtility.SetDirty(region);AssetDatabase.SaveAssetIfDirty(region);return region;
    }
    void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search=GUILayout.TextField(search,EditorStyles.toolbarTextField);
            using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))if(GUILayout.Button("지역 추가",EditorStyles.toolbarButton,GUILayout.Width(90))){selected=CreateRegion();Refresh();}
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(190)))
            {
                listScroll=EditorGUILayout.BeginScrollView(listScroll);
                foreach(var region in regions.Where(r=>ManagementEditorUI.Matches(r.label+" "+r.scenePath,search)))
                    if(GUILayout.Toggle(selected==region,region.label,"Button"))selected=region;
                EditorGUILayout.EndScrollView();
            }
            using(new EditorGUILayout.VerticalScope())
            {
                detailsScroll=EditorGUILayout.BeginScrollView(detailsScroll);
                if(selected!=null)DrawDetails();else EditorGUILayout.HelpBox("지역을 추가하세요.",MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
        }
    }
    void DrawDetails()
    {
        var data=new SerializedObject(selected);data.Update();
        EditorGUILayout.HelpBox("변경은 자동 저장·Undo 지원. 몬스터 관리에서 각 종류의 출현 가능한 지역을 연결하세요. 씬 배치는 자동 변경하지 않습니다.",MessageType.Info);
        using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
        {
            EditorGUILayout.PropertyField(data.FindProperty("label"),new GUIContent("지역 이름"));
            EditorGUILayout.PropertyField(data.FindProperty("spawnEnabled"),new GUIContent("지역 활성화"));
            var scene=data.FindProperty("scenePath");var source=AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.stringValue);
            EditorGUI.BeginChangeCheck();source=(SceneAsset)EditorGUILayout.ObjectField("대상 씬",source,typeof(SceneAsset),false);if(EditorGUI.EndChangeCheck())scene.stringValue=source!=null?AssetDatabase.GetAssetPath(source):"";
            EditorGUILayout.PropertyField(data.FindProperty("useMapBounds"),new GUIContent("맵 전체 범위 사용"));
            if(!data.FindProperty("useMapBounds").boolValue)EditorGUILayout.PropertyField(data.FindProperty("area"),new GUIContent("지역 범위 (월드 좌표)"));
            EditorGUILayout.PropertyField(data.FindProperty("spawnInterval"),new GUIContent("스폰 간격 (초)"));
            EditorGUILayout.PropertyField(data.FindProperty("maxAlive"),new GUIContent("일반 몬스터 최대 수"));
            EditorGUILayout.PropertyField(data.FindProperty("viewPadding"),new GUIContent("카메라 시야 여유 (월드 단위)"));
            EditorGUILayout.PropertyField(data.FindProperty("playerExclusionRadius"),new GUIContent("플레이어 주변 생성 제외 거리"));
            if(data.ApplyModifiedProperties()){AssetDatabase.SaveAssetIfDirty(selected);SceneView.RepaintAll();}
        }
        if(GUILayout.Button("Project에서 찾기"))EditorGUIUtility.PingObject(selected);
        var report=new SetupValidationReport();SetupDataValidator.CheckSpawnRegion(selected,report);ManagementEditorUI.DrawIssues(report);
        if(!string.IsNullOrEmpty(selected.scenePath) && !EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==selected.scenePath))EditorGUILayout.HelpBox("대상 씬을 Build Settings에서 활성화하세요.",MessageType.Warning);
        var kinds=MonsterRewardsWindow.FindTypes().Where(t=>!t.isBoss && t.spawnRegions!=null && t.spawnRegions.Contains(selected));
        EditorGUILayout.LabelField("출현 종류: "+string.Join(", ",kinds.Select(t=>t.label)),EditorStyles.wordWrappedLabel);
    }
    void DrawScene(SceneView view)
    {
        if(selected==null || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=selected.scenePath)return;
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(farm==null)return;
        var bounds=selected.Bounds(farm);Handles.color=Color.cyan;Handles.DrawWireCube(bounds.center,bounds.size);Handles.Label(bounds.max,selected.label);
    }
}
