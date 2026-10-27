using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public sealed class NpcManagerWindow : EditorWindow
{
    [SerializeField] private string scenePath,search="";
    private NpcManager manager;
    private int selected;
    private Vector2 listScroll,detailScroll;
    private SetupValidationReport report;
    [MenuItem("Tools/Everything Grows/NPC 관리")]
    public static void Open() => GetWindow<NpcManagerWindow>("NPC 관리");
    private void OnEnable() { minSize=new Vector2(760,440);Undo.undoRedoPerformed+=OnUndo; }
    private void OnDisable() { Undo.undoRedoPerformed-=OnUndo; }
    private void OnUndo() { Validate();Repaint(); }
    private void Validate()
    {
        report=new SetupValidationReport();if(manager==null)return;
        var farm=manager.GetComponent<FarmGame>();
        if(farm!=null)FarmSetupValidator.Check(farm,GameSetupResources.Load(),report,true,false);
        if(manager.npcs==null)return;
        foreach(var group in manager.npcs.Where(p=>p!=null).GroupBy(p=>p.id))
            if(string.IsNullOrWhiteSpace(group.Key) || group.Count()>1)report.Error("NPC_PROFILE_ID",manager,"npcs.id","NPC ID가 비어 있거나 중복됩니다.","각 프로필에 고유한 ID를 지정하세요.");
    }
    private void OnGUI()
    {
        var scene=ManagementEditorUI.ScenePicker(ref scenePath);
        var managers=ManagementEditorUI.Components<NpcManager>(scene);
        if(!managers.Contains(manager)) { manager=managers.FirstOrDefault();selected=0;Validate(); }
        if(manager==null) { EditorGUILayout.HelpBox("선택한 씬에 NPC 매니저가 없습니다.",MessageType.Info);return; }
        if(managers.Length>1)
        {
            int index=Array.IndexOf(managers,manager),next=EditorGUILayout.Popup("관리 대상",index,managers.Select(m=>m.name).ToArray());
            if(next!=index) { manager=managers[next];selected=0;Validate(); }
        }
        var data=new SerializedObject(manager);data.Update();var profiles=data.FindProperty("npcs");
        search=EditorGUILayout.TextField("NPC 검색",search);
        using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
        {
            EditorGUILayout.PropertyField(data.FindProperty("startingGold"),new GUIContent("시작 골드"));
            if(GUILayout.Button("프로필 추가"))
            {
                selected=profiles.arraySize;profiles.InsertArrayElementAtIndex(selected);var row=profiles.GetArrayElementAtIndex(selected);
                row.FindPropertyRelative("id").stringValue="npc_"+Guid.NewGuid().ToString("N");
                row.FindPropertyRelative("displayName").stringValue="새 NPC";row.FindPropertyRelative("greeting").stringValue="안녕하세요.";
                row.FindPropertyRelative("visitingMerchant").boolValue=false;row.FindPropertyRelative("visitChance").floatValue=.35f;row.FindPropertyRelative("visitLocations").ClearArray();
                row.FindPropertyRelative("shopEnabled").boolValue=true;row.FindPropertyRelative("guildQuestGiver").boolValue=false;row.FindPropertyRelative("offers").ClearArray();search="";
            }
            using(new EditorGUILayout.HorizontalScope())
            {
                using(new EditorGUILayout.VerticalScope(GUILayout.Width(220)))
                {
                    listScroll=EditorGUILayout.BeginScrollView(listScroll);
                    for(int i=0;i<profiles.arraySize;i++)
                    {
                        var row=profiles.GetArrayElementAtIndex(i);string id=row.FindPropertyRelative("id").stringValue,name=row.FindPropertyRelative("displayName").stringValue;
                        if(!ManagementEditorUI.Matches(name+" "+id,search))continue;
                        if(GUILayout.Toggle(selected==i,name+" / "+id,"Button",GUILayout.Height(36)) && selected!=i) { selected=i;detailScroll=Vector2.zero;GUI.FocusControl(null); }
                    }
                    EditorGUILayout.EndScrollView();
                }
                using(new EditorGUILayout.VerticalScope())
                {
                    detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                    selected=Mathf.Clamp(selected,0,Mathf.Max(0,profiles.arraySize-1));
                    if(profiles.arraySize>0)
                    {
                        var row=profiles.GetArrayElementAtIndex(selected);
                        EditorGUILayout.HelpBox("씬 NPC는 이 프로필의 ID로 연결됩니다. ID 변경 시 해당 NPC의 Npc Id도 맞춰주세요. 변경 후 아래 버튼으로 씬을 저장합니다.",MessageType.Info);
                        Field(row,"id","NPC ID");Field(row,"displayName","이름");Field(row,"greeting","대사");Field(row,"shopEnabled","상점 사용");Field(row,"guildQuestGiver","길드 의뢰 접수");
                        if(row.FindPropertyRelative("shopEnabled").boolValue)
                        {
                            EditorGUILayout.HelpBox("상인은 밤 구간에만 출현합니다. 해질녘·새벽에는 퇴장합니다.",MessageType.Info);
                            Field(row,"visitingMerchant","확률 방문 상인");
                            if(row.FindPropertyRelative("visitingMerchant").boolValue)
                            {
                                var chance=row.FindPropertyRelative("visitChance");EditorGUI.BeginChangeCheck();float percent=EditorGUILayout.Slider("밤마다 방문 확률 (%)",chance.floatValue*100,0,100);if(EditorGUI.EndChangeCheck())chance.floatValue=percent/100;
                                Field(row,"visitLocations","방문 후보 위치");
                                if(GUILayout.Button("후보 위치 추가"))
                                {
                                    var point=new GameObject("Merchant visit point");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(point,scene);point.transform.SetParent(manager.transform,false);
                                    point.transform.position=SceneView.lastActiveSceneView!=null?SceneView.lastActiveSceneView.pivot:Vector3.zero;Undo.RegisterCreatedObjectUndo(point,"Add merchant visit point");
                                    var locations=row.FindPropertyRelative("visitLocations");int i=locations.arraySize;locations.InsertArrayElementAtIndex(i);locations.GetArrayElementAtIndex(i).objectReferenceValue=point.transform;Selection.activeGameObject=point;
                                }
                                EditorGUILayout.HelpBox("후보는 NPC 몸체 밖의 빈 오브젝트로 지정하세요. 같은 밤에는 결과와 위치를 다시 추첨하지 않습니다.",MessageType.Info);
                            }
                            EditorGUILayout.HelpBox("Buy Price: 구매가 / Sell Price: 판매가 (-1은 아이템 기본 판매가)",MessageType.Info);EditorGUILayout.PropertyField(row.FindPropertyRelative("offers"),new GUIContent("판매 품목"),true); }
                        string id=row.FindPropertyRelative("id").stringValue;
                        var npcs=ManagementEditorUI.Components<ShopNpc>(scene).Where(n=>n.npcId==id).ToArray();
                        EditorGUILayout.LabelField("연결된 NPC: "+npcs.Length);
                        foreach(var npc in npcs)EditorGUILayout.ObjectField(npc,typeof(ShopNpc),true);
                    }
                    else EditorGUILayout.HelpBox("프로필을 추가하세요. 실제 캐릭터는 씬에서 배치합니다.",MessageType.Info);
                    EditorGUILayout.EndScrollView();
                }
            }
            if(data.ApplyModifiedProperties()) { PrefabUtility.RecordPrefabInstancePropertyModifications(manager);EditorSceneManager.MarkSceneDirty(scene);Validate(); }
        }
        if(GUILayout.Button("설정 검사"))Validate();
        ManagementEditorUI.DrawIssues(report);ManagementEditorUI.SaveSceneButton(scene);
    }
    private static void Field(SerializedProperty row,string name,string label)=>EditorGUILayout.PropertyField(row.FindPropertyRelative(name),new GUIContent(label),true);
}
