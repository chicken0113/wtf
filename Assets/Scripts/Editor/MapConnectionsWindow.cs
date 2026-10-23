using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public sealed class MapConnectionsWindow : EditorWindow
{
    [SerializeField] private string scenePath,search="";
    private Component selected;
    private Editor inspector;
    private int tab;
    private Vector2 listScroll,detailScroll;
    private SetupValidationReport report;
    private string result;
    [MenuItem("Tools/Everything Grows/맵 연결 관리")]
    public static void Open() => GetWindow<MapConnectionsWindow>("맵 연결 관리");
    private void OnEnable() { minSize=new Vector2(800,440);Undo.undoRedoPerformed+=OnUndo; }
    private void OnDisable() { Undo.undoRedoPerformed-=OnUndo;if(inspector!=null)DestroyImmediate(inspector); }
    private void OnUndo() { RefreshValidation();Repaint(); }
    private void RefreshValidation() => report=selected is MapExit exit?CheckExit(exit):null;
    internal static SetupValidationReport CheckExit(MapExit exit)
    {
        var report=new SetupValidationReport();if(exit==null)return report;
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(exit.destinationScene)==null)
        { report.Error("EXIT_SCENE",exit,"destinationScene","목적지 씬이 없습니다.","유효한 목적지 씬을 지정하세요.");return report; }
        if(!EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==exit.destinationScene))report.Error("EXIT_BUILD",exit,"destinationScene","목적지 씬이 빌드에 포함되지 않았습니다.","목적지 씬을 활성 빌드 목록에 추가하세요.");
        if(!SetupDataValidator.Finite(exit.areaSize.x) || !SetupDataValidator.Finite(exit.areaSize.y) || exit.areaSize.x<=0 || exit.areaSize.y<=0)report.Error("EXIT_SIZE",exit,"areaSize","출입구 범위가 유효하지 않습니다.","양수 범위를 설정하세요.");
        var target=SceneManager.GetSceneByPath(exit.destinationScene);bool preview=!target.IsValid() || !target.isLoaded;
        try
        {
            if(preview)target=EditorSceneManager.OpenPreviewScene(exit.destinationScene);
            var points=ManagementEditorUI.Components<MapArrivalPoint>(target).Where(p=>p.isActiveAndEnabled && p.arrivalId==exit.arrivalId).ToArray();
            if(string.IsNullOrWhiteSpace(exit.arrivalId) || points.Length!=1)report.Error("EXIT_ARRIVAL",exit,"arrivalId","도착점이 없거나 ID가 중복됩니다.","목적지 씬의 고유 도착 ID를 지정하세요.");
        }
        catch(Exception error) { report.Error("EXIT_READ",exit,"destinationScene",error.Message,"목적지 씬을 확인하세요."); }
        finally { if(preview && target.IsValid())EditorSceneManager.ClosePreviewScene(target); }
        return report;
    }
    private void OnGUI()
    {
        var scene=ManagementEditorUI.ScenePicker(ref scenePath);
        search=EditorGUILayout.TextField("이름 / 목적지 / ID 검색",search);
        tab=GUILayout.Toolbar(tab,new[]{"출입구","도착점"});
        Component[] entries=tab==0?ManagementEditorUI.Components<MapExit>(scene).Cast<Component>().ToArray():ManagementEditorUI.Components<MapArrivalPoint>(scene).Cast<Component>().ToArray();
        if(!entries.Contains(selected)) { selected=entries.FirstOrDefault();RefreshValidation(); }
        using(new EditorGUILayout.HorizontalScope())
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(270)))
            {
                listScroll=EditorGUILayout.BeginScrollView(listScroll);
                foreach(var entry in entries)
                {
                    string description=entry is MapExit e?entry.name+"\n"+(e.requireInteract?"우클릭":"접촉")+" → "+System.IO.Path.GetFileNameWithoutExtension(e.destinationScene)+" / "+e.arrivalId:entry.name+" / "+((MapArrivalPoint)entry).arrivalId;
                    if(ManagementEditorUI.Matches(description,search) && GUILayout.Toggle(selected==entry,description,"Button",GUILayout.Height(46)) && selected!=entry)
                    { selected=entry;detailScroll=Vector2.zero;GUI.FocusControl(null);RefreshValidation(); }
                }
                EditorGUILayout.EndScrollView();
            }
            using(new EditorGUILayout.VerticalScope())
            {
                detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                if(selected!=null)
                {
                    if(GUILayout.Button("Scene에서 선택")) { Selection.activeGameObject=selected.gameObject;SceneView.lastActiveSceneView?.FrameSelected(); }
                    EditorGUILayout.HelpBox("현재 씬의 출입구/도착점을 직접 편집합니다. 수정 후 씬 저장을 누르세요. 배치 자동 생성이나 이동은 하지 않습니다.",MessageType.Info);
                    using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
                    {
                        Editor.CreateCachedEditor(selected,null,ref inspector);EditorGUI.BeginChangeCheck();inspector.OnInspectorGUI();
                        if(EditorGUI.EndChangeCheck()) { PrefabUtility.RecordPrefabInstancePropertyModifications(selected);EditorSceneManager.MarkSceneDirty(scene);RefreshValidation(); }
                    }
                    if(GUILayout.Button("선택 연결 검사"))RefreshValidation();
                    ManagementEditorUI.DrawIssues(report);
                }
                else EditorGUILayout.HelpBox("선택한 씬에 해당 항목이 없습니다.",MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
        }
        using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
            if(GUILayout.Button("전체 맵 연결 검사"))
            {
                try { WorldConnectionValidator.Validate();result="전체 연결 검사를 통과했습니다."; }
                catch(Exception error) { result=error.Message+" (자세한 내용은 Console)"; }
            }
        if(!string.IsNullOrEmpty(result))EditorGUILayout.HelpBox(result,MessageType.Info);
        ManagementEditorUI.SaveSceneButton(scene);
    }
}
