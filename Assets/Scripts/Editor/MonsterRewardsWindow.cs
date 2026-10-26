using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EverythingGrows;

public sealed class MonsterRewardsWindow : EditorWindow
{
    [SerializeField] private MonsterType selected;
    [SerializeField] private string search="";
    private MonsterType[] types=Array.Empty<MonsterType>();
    private SerializedObject data;
    private SetupValidationReport report;
    private Vector2 listScroll,detailScroll;

    [MenuItem("Tools/Everything Grows/몬스터 관리")]
    public static void Open() => GetWindow<MonsterRewardsWindow>("몬스터 관리");
    private void OnEnable()
    {
        minSize=new Vector2(680,400);Refresh();
        EditorApplication.projectChanged+=Refresh;Undo.undoRedoPerformed+=OnUndo;
    }
    private void OnDisable()
    { EditorApplication.projectChanged-=Refresh;Undo.undoRedoPerformed-=OnUndo; }
    private void Refresh()
    {
        types=FindTypes();
        if(selected==null)selected=types.FirstOrDefault();
        data=selected!=null?new SerializedObject(selected):null;
        Validate();Repaint();
    }
    private void OnUndo()
    {
        if(selected!=null)AssetDatabase.SaveAssetIfDirty(selected);
        Refresh();
    }
    internal static MonsterType[] FindTypes() => AssetDatabase.FindAssets("t:MonsterType",new[]{"Assets"})
        .Select(g=>AssetDatabase.LoadAssetAtPath<MonsterType>(AssetDatabase.GUIDToAssetPath(g)))
        .Where(t=>t!=null).OrderBy(DisplayName,StringComparer.OrdinalIgnoreCase).ToArray();
    private static string DisplayName(MonsterType type) => string.IsNullOrWhiteSpace(type.label)?type.name:type.label;
    internal static bool Matches(MonsterType type,string query) => string.IsNullOrWhiteSpace(query) ||
        (DisplayName(type)+" "+type.name+" "+AssetDatabase.GetAssetPath(type)).IndexOf(query.Trim(),StringComparison.OrdinalIgnoreCase)>=0;
    internal static MonsterType CreateType(MonsterType source=null)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Play를 정지한 뒤 종류를 추가하세요.");
        const string folder="Assets/Resources/Monsters";
        if(!AssetDatabase.IsValidFolder(folder))throw new InvalidOperationException(folder+" 폴더를 복원하세요.");
        var created=source!=null?Instantiate(source):CreateInstance<MonsterType>();
        created.label=source!=null?DisplayName(source)+" 복사":"새 몬스터";
        string path=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+(source!=null?source.name+"Copy":"NewMonster")+".asset");
        AssetDatabase.CreateAsset(created,path);created.saveId=AssetDatabase.AssetPathToGUID(path);EditorUtility.SetDirty(created);AssetDatabase.SaveAssetIfDirty(created);
        return created;
    }
    internal static SetupValidationReport CheckType(MonsterType type)
    {
        var result=new SetupValidationReport();
        SetupDataValidator.CheckMonsterType(type,Resources.Load<SaveItemCatalog>("SaveItemCatalog"),result,new HashSet<PlantDefinition>(),true);
        return result;
    }
    private void Validate() => report=CheckType(selected);
    private void SelectType(MonsterType type)
    { selected=type;data=new SerializedObject(type);detailScroll=Vector2.zero;Validate();GUI.FocusControl(null); }
    private void AddType(bool duplicate)
    {
        try { var created=CreateType(duplicate?selected:null);Refresh();search="";SelectType(created);EditorGUIUtility.PingObject(created); }
        catch(Exception error) { Debug.LogException(error);ShowNotification(new GUIContent(error.Message)); }
    }
    private void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search=GUILayout.TextField(search,GUI.skin.FindStyle("ToolbarSearchTextField")??EditorStyles.toolbarTextField,GUILayout.MinWidth(120));
            if(GUILayout.Button("새로고침",EditorStyles.toolbarButton,GUILayout.Width(72)))Refresh();
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if(GUILayout.Button("종류 추가",EditorStyles.toolbarButton,GUILayout.Width(76)))AddType(false);
                using(new EditorGUI.DisabledScope(selected==null))
                    if(GUILayout.Button("선택 복제",EditorStyles.toolbarButton,GUILayout.Width(76)))AddType(true);
            }
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(210)))
            {
                var filtered=types.Where(t=>t!=null && Matches(t,search)).ToArray();
                EditorGUILayout.LabelField("종류 "+filtered.Length+" / "+types.Length,EditorStyles.boldLabel);
                listScroll=EditorGUILayout.BeginScrollView(listScroll);
                foreach(var type in filtered)
                {
                    string text=DisplayName(type)+"\n"+type.name;
                    if(GUILayout.Toggle(selected==type,new GUIContent(text,AssetDatabase.GetAssetPath(type)),"Button",GUILayout.Height(42)) && selected!=type)SelectType(type);
                }
                if(filtered.Length==0)EditorGUILayout.HelpBox("검색 결과가 없습니다.",MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
            using(new EditorGUILayout.VerticalScope())
            {
                detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                if(selected==null)EditorGUILayout.HelpBox("종류를 선택하거나 추가하세요.",MessageType.Info);
                else DrawDetails();
                EditorGUILayout.EndScrollView();
            }
        }
    }
    private void DrawDetails()
    {
        if(data==null || data.targetObject!=selected)data=new SerializedObject(selected);
        data.Update();
        EditorGUILayout.LabelField(DisplayName(selected),EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selected),EditorStyles.miniLabel,GUILayout.Height(18));
        if(GUILayout.Button("Project에서 에셋 찾기",GUILayout.Width(170)))EditorGUIUtility.PingObject(selected);
        EditorGUILayout.HelpBox("같은 종류를 사용하는 모든 몬스터에 적용됩니다. 변경은 자동 저장되며 Ctrl+Z로 되돌릴 수 있습니다.",MessageType.Info);
        using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUILayout.PropertyField(data.FindProperty("label"),new GUIContent("종류 이름"));
            EditorGUILayout.PropertyField(data.FindProperty("isBoss"),new GUIContent("보스 (지역 이탈 정리 제외)"));
            if(data.FindProperty("isBoss").boolValue)
            {
                EditorGUILayout.PropertyField(data.FindProperty("bossLeashRange"),new GUIContent("추적 범위 (고정 위치 기준)"));
                EditorGUILayout.PropertyField(data.FindProperty("bossRespawnSeconds"),new GUIContent("처치 후 재등장 (초)"));
            }
            EditorGUILayout.PropertyField(data.FindProperty("spawnRegions"),new GUIContent("출현 가능한 지역"),true);
            EditorGUILayout.HelpBox("일반 몬스터만 지역 스폰 대상입니다. 보스는 씬에 고정 배치하세요. 범위·간격·최대 수는 지역 설정에서 조절합니다.",MessageType.Info);
            if(GUILayout.Button("지역 설정 열기"))SpawnRegionWindow.Open();
            EditorGUILayout.LabelField("행동과 능력치",EditorStyles.boldLabel);
            foreach(string field in new[]{"behavior","maxHealth","moveSpeed","returnSpeed","detectionRange","attackRange","meleeHitRange","attackHalfHearts","windupSeconds","attackCooldown","projectileSpeed","projectileLifetime"})EditorGUILayout.PropertyField(data.FindProperty(field));
            EditorGUILayout.PropertyField(data.FindProperty("gold"),new GUIContent("골드 보상"));
            var drops=data.FindProperty("drops");
            EditorGUILayout.Space();EditorGUILayout.LabelField("드롭 목록",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("각 항목은 독립 추첨 · 확률 0%는 없음, 100%는 확정",EditorStyles.wordWrappedMiniLabel);
            int remove=-1;
            for(int i=0;i<drops.arraySize;i++)
            {
                var row=drops.GetArrayElementAtIndex(i);
                using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("항목 "+(i+1),EditorStyles.boldLabel);
                        if(GUILayout.Button("제거",GUILayout.Width(50)))remove=i;
                    }
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("item"),new GUIContent("아이템"));
                    var chance=row.FindPropertyRelative("probability");EditorGUI.BeginChangeCheck();
                    float percent=EditorGUILayout.Slider("확률 (%)",chance.floatValue*100,0,100);
                    if(EditorGUI.EndChangeCheck())chance.floatValue=percent/100;
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("minimum"),new GUIContent("최소 수량"));
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("maximum"),new GUIContent("최대 수량"));
                }
            }
            if(remove>=0)drops.DeleteArrayElementAtIndex(remove);
            if(GUILayout.Button("드롭 항목 추가"))
            {
                int index=drops.arraySize;drops.InsertArrayElementAtIndex(index);var row=drops.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("item").objectReferenceValue=null;row.FindPropertyRelative("probability").floatValue=1;
                row.FindPropertyRelative("minimum").intValue=1;row.FindPropertyRelative("maximum").intValue=1;
            }
            if(drops.arraySize==0)EditorGUILayout.HelpBox("아이템을 드롭하지 않습니다. 골드도 0이면 보상이 없습니다.",MessageType.Info);
            if(data.ApplyModifiedProperties()) { AssetDatabase.SaveAssetIfDirty(selected);Validate(); }
        }
        if(report!=null)foreach(var issue in report.Issues)EditorGUILayout.HelpBox(issue.Message,issue.IsError?MessageType.Error:MessageType.Warning);
    }
}
