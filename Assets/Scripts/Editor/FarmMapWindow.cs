using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public sealed class FarmMapWindow : EditorWindow
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private float grid=1,scale=1;
    [SerializeField] private bool snap=true;
    private bool painting;
    private Tool previousTool;
    private int stroke=-1;
    private int ownedControl;
    private readonly HashSet<Vector2> visited=new();
    private Vector2 scroll;
    private GameObject[] palette;
    [MenuItem("Tools/Everything Grows/Map placement")]
    public static void Open() => GetWindow<FarmMapWindow>("맵 배치");
    private void OnEnable() { SceneView.duringSceneGui+=SceneGUI; Refresh(); }
    private void OnDisable() { SceneView.duringSceneGui-=SceneGUI; SetPainting(false); EndStroke(); }
    private void Refresh() { palette=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Map"}).Select(guid=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))).Where(item=>item!=null && item.GetComponent<MapArea>()==null && item.name!="PlayerSpawn").ToArray(); }
    private static FarmMapLayout Map()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return scene.IsValid()?scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<FarmMapLayout>()).FirstOrDefault():null;
    }
    private void SetPainting(bool value)
    {
        if(value==painting) return;
        if(value) { previousTool=Tools.current; Tools.current=Tool.None; }
        else { Tools.current=previousTool; EndStroke(); }
        painting=value; SceneView.RepaintAll(); Repaint();
    }
    private void OnGUI()
    {
        var map=Map();
        EditorGUILayout.LabelField("씬에서 직접 배치",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("배치 시작 → Scene에서 클릭. 바닥·길은 드래그로 배치. Shift+클릭/드래그로 현재 계층의 대상만 삭제. Esc로 일반 편집 복귀. Ctrl+Z로 취소.",MessageType.Info);
        using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if(map==null)
            {
                EditorGUILayout.HelpBox("편집용 맵이 있는 씬을 열어주세요.",MessageType.Warning);
                if(GUILayout.Button("현재 씬을 편집용 맵으로 연결")) EditableMapBuilder.Setup();
                return;
            }
            EditorGUILayout.ObjectField("현재 맵",map,typeof(FarmMapLayout),true);
            EditorGUI.BeginChangeCheck();
            var chosen=(GameObject)EditorGUILayout.ObjectField("배치할 프리팹",prefab,typeof(GameObject),false);
            if(EditorGUI.EndChangeCheck()) Choose(chosen);
            snap=EditorGUILayout.Toggle("격자에 맞추기",snap); grid=Mathf.Max(.1f,EditorGUILayout.FloatField("격자 간격",grid));
            scale=Mathf.Max(.05f,EditorGUILayout.FloatField("배치 크기",scale));
            using(new EditorGUI.DisabledScope(prefab==null))
                if(GUILayout.Button(painting?"배치 종료 · 일반 선택/이동":"배치 시작",GUILayout.Height(30))) SetPainting(!painting);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("맵 경계")) Select(map.worldBounds!=null?map.worldBounds.transform:null);
            if(GUILayout.Button("안전 구역")) Select(map.safeArea!=null?map.safeArea.transform:null);
            if(GUILayout.Button("시작 위치")) Select(map.playerSpawn);
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("씬 저장 · Ctrl+S")) EditorSceneManager.SaveScene(map.gameObject.scene);
            if(GUILayout.Button("프리팹 목록 새로고침")) Refresh();
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(MapPlacement.Category category in System.Enum.GetValues(typeof(MapPlacement.Category)))
            {
                EditorGUILayout.LabelField(category.ToString(),EditorStyles.boldLabel);
                foreach(var item in palette.Where(item=>MapPlacement.CategoryOf(item)==category))
                {
                    var icon=AssetPreview.GetMiniThumbnail(item);
                    if(GUILayout.Button(new GUIContent(item.name,icon),GUILayout.Height(26))) Choose(item);
                }
            }
            EditorGUILayout.EndScrollView();
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode && painting) SetPainting(false);
    }
    private void Choose(GameObject value)
    {
        SetPainting(false); prefab=value;
        if(prefab!=null) { var category=MapPlacement.CategoryOf(prefab); scale=category==MapPlacement.Category.Items?.8f:category==MapPlacement.Category.EnemySpawns?1.2f:1; }
    }
    private void Select(Transform target)
    {
        SetPainting(false); if(target==null) return;
        Selection.activeGameObject=target.gameObject; SceneView.lastActiveSceneView?.FrameSelected();
    }
    private void EndStroke() { if(ownedControl!=0 && GUIUtility.hotControl==ownedControl) GUIUtility.hotControl=0; ownedControl=0; if(stroke>=0) Undo.CollapseUndoOperations(stroke); stroke=-1; visited.Clear(); }
    private void SceneGUI(SceneView view)
    {
        if(!painting || prefab==null || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var map=Map(); if(map==null) { SetPainting(false); return; }
        var e=Event.current;
        if(e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape) { SetPainting(false); e.Use(); return; }
        if(e.alt || e.control || e.command) { if(stroke>=0) EndStroke(); return; }
        int control=GUIUtility.GetControlID("FarmMapPaint".GetHashCode(),FocusType.Passive);
        if(e.type==EventType.Layout) HandleUtility.AddDefaultControl(control);
        var ray=HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if(!new Plane(Vector3.forward,Vector3.zero).Raycast(ray,out float distance)) return;
        Vector2 point=ray.GetPoint(distance);
        if(snap) point=new Vector2(Mathf.Round(point.x/grid)*grid,Mathf.Round(point.y/grid)*grid);
        Handles.color=e.shift?Color.red:Color.cyan; Handles.DrawWireCube(point,new Vector3(scale,scale,0));
        Handles.Label((Vector3)point+Vector3.up*.6f,(e.shift?"삭제: ":"배치: ")+prefab.name);
        var category=MapPlacement.CategoryOf(prefab);
        bool drag=category==MapPlacement.Category.Ground || category==MapPlacement.Category.Paths || e.shift;
        if(e.button==0 && (e.type==EventType.MouseDown || (e.type==EventType.MouseDrag && drag)))
        {
            if(e.type==EventType.MouseDown)
            {
                if(HandleUtility.nearestControl!=control) return;
                EndStroke(); ownedControl=control; GUIUtility.hotControl=control;
                Undo.IncrementCurrentGroup(); stroke=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Paint map layer");
            }
            else if(ownedControl!=control || GUIUtility.hotControl!=control) return;
            if(visited.Add(point))
            {
                if(e.shift) MapPlacement.Erase(map,point,Mathf.Max(.25f,grid*.45f),category);
                else { var placed=MapPlacement.Place(map,prefab,point,scale,category); if(placed!=null) Selection.activeGameObject=placed; }
            }
            e.Use();
        }
        if(e.type==EventType.MouseUp && e.button==0 && ownedControl==control) { EndStroke(); e.Use(); }
        if(e.type==EventType.MouseMove || e.type==EventType.MouseDrag) view.Repaint();
    }
}

[InitializeOnLoad]
public static class FarmMapWindowRequest
{
    const string Request="Library/MapTools.open";
    static FarmMapWindowRequest() { if(!Application.isBatchMode) EditorApplication.update+=Tick; }
    static void Tick()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !System.IO.File.Exists(Request)) return;
        System.IO.File.Delete(Request); FarmMapWindow.Open(); Debug.Log("MAP_PLACEMENT_WINDOW_READY");
    }
}
