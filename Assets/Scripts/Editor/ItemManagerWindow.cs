using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EverythingGrows;

public sealed class ItemManagerWindow : EditorWindow
{
    [SerializeField] private PlantDefinition selected;
    [SerializeField] private string search="";
    private PlantDefinition[] items=System.Array.Empty<PlantDefinition>();
    private Vector2 listScroll,detailScroll;
    private Editor inspector;
    private SetupValidationReport report;
    [MenuItem("Tools/Everything Grows/아이템 관리")]
    public static void Open() => GetWindow<ItemManagerWindow>("아이템 관리");
    private void OnEnable() { minSize=new Vector2(760,440);Refresh();EditorApplication.projectChanged+=Refresh;Undo.undoRedoPerformed+=OnUndo; }
    private void OnDisable() { EditorApplication.projectChanged-=Refresh;Undo.undoRedoPerformed-=OnUndo;if(inspector!=null)DestroyImmediate(inspector); }
    internal static PlantDefinition[] FindItems() => AssetDatabase.FindAssets("t:PlantDefinition",new[]{"Assets"})
        .Select(g=>AssetDatabase.LoadAssetAtPath<PlantDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(i=>i!=null).OrderBy(i=>i.label).ToArray();
    private void Refresh() { items=FindItems();if(selected==null)selected=items.FirstOrDefault();Validate();Repaint(); }
    private void OnUndo() { if(selected!=null)AssetDatabase.SaveAssetIfDirty(selected);Refresh(); }
    private void Validate()
    {
        report=new SetupValidationReport();SetupDataValidator.CheckItem(selected,Resources.Load<SaveItemCatalog>("SaveItemCatalog"),report,new HashSet<PlantDefinition>(),true);
    }
    private void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search=GUILayout.TextField(search,EditorStyles.toolbarSearchField);
            if(GUILayout.Button("새로고침",EditorStyles.toolbarButton,GUILayout.Width(75)))Refresh();
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(225)))
            {
                listScroll=EditorGUILayout.BeginScrollView(listScroll);
                foreach(var item in items.Where(i=>i!=null && ManagementEditorUI.Matches(i.label+" "+i.name+" "+i.GrowthType,search)))
                    if(GUILayout.Toggle(selected==item,(string.IsNullOrWhiteSpace(item.label)?item.name:item.label)+"\n"+item.GrowthType+" · "+item.sellPrice+" G","Button",GUILayout.Height(42)) && selected!=item)
                    { selected=item;detailScroll=Vector2.zero;GUI.FocusControl(null);Validate(); }
                EditorGUILayout.EndScrollView();
            }
            using(new EditorGUILayout.VerticalScope())
            {
                detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                if(selected==null)EditorGUILayout.HelpBox("아이템을 선택하세요.",MessageType.Info);
                else
                {
                    EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(selected),EditorStyles.miniLabel,GUILayout.Height(18));
                    EditorGUILayout.HelpBox("선택한 아이템 에셋을 직접 편집합니다. 자동 저장 · Ctrl+Z 되돌리기. 성장 유형에 필요한 설정만 표시합니다.",MessageType.Info);
                    using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
                    {
                        Editor.CreateCachedEditor(selected,null,ref inspector);EditorGUI.BeginChangeCheck();inspector.OnInspectorGUI();
                        if(EditorGUI.EndChangeCheck()) { AssetDatabase.SaveAssetIfDirty(selected);Validate(); }
                    }
                    if(GUILayout.Button("Project에서 찾기"))EditorGUIUtility.PingObject(selected);
                    ManagementEditorUI.DrawIssues(report);
                }
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
