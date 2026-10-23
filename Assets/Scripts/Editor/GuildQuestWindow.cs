using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EverythingGrows;

public sealed class GuildQuestWindow : EditorWindow
{
    private GuildQuestCatalog catalog;
    private int selected;
    private string search="";
    private Vector2 listScroll,detailScroll;
    public static void Open() => GetWindow<GuildQuestWindow>("길드 의뢰 관리");
    private void OnEnable() { minSize=new Vector2(760,440);Undo.undoRedoPerformed+=OnUndo; }
    private void OnDisable() { Undo.undoRedoPerformed-=OnUndo; }
    private void OnUndo() { if(catalog!=null)AssetDatabase.SaveAssetIfDirty(catalog);Repaint(); }
    internal static SetupValidationReport CheckCatalog(GuildQuestCatalog value)
    {
        var report=new SetupValidationReport();
        if(value==null || value.quests==null) { report.Error("GUILD_CATALOG",value,"quests","의뢰 목록이 없습니다.","GuildQuests 에셋을 복원하세요.");return report; }
        foreach(var q in value.quests)
        {
            if(q==null || string.IsNullOrWhiteSpace(q.id) || value.quests.Count(other=>other!=null && other.id==q.id)>1)
            { report.Error("GUILD_ID",value,"quests.id","의뢰 ID가 비어 있거나 중복됩니다.","고유한 의뢰 ID를 사용하세요.");continue; }
            if(q.requiredKills<1 || q.rewardGold<0 || !Enum.IsDefined(typeof(GuildEnemyKind),q.enemyKind))report.Error("GUILD_VALUE",value,q.id,"목표 수/보상/대상 종류가 잘못됐습니다.","처치 수는 1 이상, 골드는 0 이상으로 설정하세요.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(q.targetScene)==null || !EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==q.targetScene))report.Error("GUILD_SCENE",value,q.id+".targetScene","목표 씬이 없거나 빌드에서 비활성 상태입니다.","활성 빌드 씬을 선택하세요.");
        }
        return report;
    }
    private void OnGUI()
    {
        if(catalog==null)catalog=Resources.Load<GuildQuestCatalog>("GuildQuests");
        if(catalog==null) { EditorGUILayout.HelpBox("Resources/GuildQuests 에셋이 없습니다.",MessageType.Error);return; }
        search=EditorGUILayout.TextField("의뢰 검색",search);
        var data=new SerializedObject(catalog);data.Update();var quests=data.FindProperty("quests");
        using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
        {
            if(GUILayout.Button("의뢰 추가"))
            {
                selected=quests.arraySize;quests.InsertArrayElementAtIndex(selected);var q=quests.GetArrayElementAtIndex(selected);
                q.FindPropertyRelative("id").stringValue="quest_"+Guid.NewGuid().ToString("N");q.FindPropertyRelative("title").stringValue="새 의뢰";
                q.FindPropertyRelative("description").stringValue="";q.FindPropertyRelative("targetScene").stringValue="Assets/Scenes/CombatTest.unity";
                q.FindPropertyRelative("enemyKind").enumValueIndex=0;q.FindPropertyRelative("requiredKills").intValue=1;q.FindPropertyRelative("rewardGold").intValue=0;q.FindPropertyRelative("repeatable").boolValue=false;search="";
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUILayout.VerticalScope(GUILayout.Width(220)))
                {
                    listScroll=EditorGUILayout.BeginScrollView(listScroll);
                    for(int i=0;i<quests.arraySize;i++)
                    {
                        var row=quests.GetArrayElementAtIndex(i);string title=row.FindPropertyRelative("title").stringValue,id=row.FindPropertyRelative("id").stringValue;
                        if(ManagementEditorUI.Matches(title+" "+id,search) && GUILayout.Toggle(selected==i,title+"\n"+id,"Button",GUILayout.Height(42)) && selected!=i) { selected=i;detailScroll=Vector2.zero;GUI.FocusControl(null); }
                    }
                    EditorGUILayout.EndScrollView();
                }
                using(new EditorGUILayout.VerticalScope())
                {
                    detailScroll=EditorGUILayout.BeginScrollView(detailScroll);selected=Mathf.Clamp(selected,0,Mathf.Max(0,quests.arraySize-1));
                    if(quests.arraySize>0)
                    {
                        var row=quests.GetArrayElementAtIndex(selected);
                        EditorGUILayout.HelpBox("기존 의뢰 ID는 저장과 연결되므로 유지하세요. 수정은 자동 저장되며 이미 수락한 의뢰는 수락 당시 조건을 유지합니다.",MessageType.Info);
                        foreach(string name in new[]{"id","title","description"})EditorGUILayout.PropertyField(row.FindPropertyRelative(name));
                        var path=row.FindPropertyRelative("targetScene");EditorGUI.BeginChangeCheck();
                        var scene=(SceneAsset)EditorGUILayout.ObjectField("목표 씬",AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue),typeof(SceneAsset),false);
                        if(EditorGUI.EndChangeCheck())path.stringValue=scene!=null?AssetDatabase.GetAssetPath(scene):"";
                        foreach(string name in new[]{"enemyKind","requiredKills","rewardGold","repeatable"})EditorGUILayout.PropertyField(row.FindPropertyRelative(name));
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            if(data.ApplyModifiedProperties())AssetDatabase.SaveAssetIfDirty(catalog);
        }
        ManagementEditorUI.DrawIssues(CheckCatalog(catalog));
    }
}
