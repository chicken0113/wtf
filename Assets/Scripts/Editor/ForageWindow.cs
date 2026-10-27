using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
public sealed class ForageWindow : EditorWindow
{
    ForageDefinition selected;
    ForageDefinition[] profiles=Array.Empty<ForageDefinition>();
    string search="";Vector2 listScroll,detailScroll;
    public static void Open()=>GetWindow<ForageWindow>("채집물 설정");
    void OnEnable(){minSize=new Vector2(640,350);Refresh();EditorApplication.projectChanged+=Refresh;Undo.undoRedoPerformed+=OnUndo;}
    void OnDisable(){EditorApplication.projectChanged-=Refresh;Undo.undoRedoPerformed-=OnUndo;}
    void OnUndo(){if(selected!=null)AssetDatabase.SaveAssetIfDirty(selected);Refresh();}
    void Refresh(){profiles=AssetDatabase.FindAssets("t:ForageDefinition").Select(id=>AssetDatabase.LoadAssetAtPath<ForageDefinition>(AssetDatabase.GUIDToAssetPath(id))).OrderBy(p=>p.label).ToArray();if(selected==null)selected=profiles.FirstOrDefault();Repaint();}
    void OnGUI()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            search=GUILayout.TextField(search,EditorStyles.toolbarTextField);
            using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))if(GUILayout.Button("종류 추가",EditorStyles.toolbarButton,GUILayout.Width(90)))
            {
                const string folder="Assets/Resources/Foraging";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","Foraging");
                selected=CreateInstance<ForageDefinition>();selected.label="새 채집물";AssetDatabase.CreateAsset(selected,AssetDatabase.GenerateUniqueAssetPath(folder+"/NewForage.asset"));Refresh();
            }
        }
        using(new EditorGUILayout.HorizontalScope())
        {
            using(new EditorGUILayout.VerticalScope(GUILayout.Width(190)))
            {
                listScroll=EditorGUILayout.BeginScrollView(listScroll);
                foreach(var profile in profiles.Where(p=>ManagementEditorUI.Matches(p.label+" "+p.name,search)))if(GUILayout.Toggle(selected==profile,profile.label,"Button"))selected=profile;
                EditorGUILayout.EndScrollView();
            }
            detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
            if(selected!=null)
            {
                using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
                {
                    var data=new SerializedObject(selected);data.Update();
                    foreach(var pair in new[]{("label","종류 이름"),("item","획득 아이템"),("quantity","한 번에 준비되는 수량"),("respawns","재생 여부")})EditorGUILayout.PropertyField(data.FindProperty(pair.Item1),new GUIContent(pair.Item2));
                    if(data.FindProperty("respawns").boolValue)EditorGUILayout.PropertyField(data.FindProperty("respawnSeconds"),new GUIContent("전량 획득 후 재생 시간 (초)"));
                    if(data.ApplyModifiedProperties())AssetDatabase.SaveAssetIfDirty(selected);
                    if(GUILayout.Button("현재 씬에 배치"))
                    {
                        try
                        {
                            var scene=SceneManager.GetActiveScene();var farm=ManagementEditorUI.Components<FarmGame>(scene).SingleOrDefault();
                            if(farm==null || farm.Layout==null)throw new InvalidOperationException("FarmMapLayout이 있는 맵 씬을 활성화하세요.");
                            Vector2 point=SceneView.lastActiveSceneView!=null?(Vector2)SceneView.lastActiveSceneView.pivot:farm.SpawnPosition;
                            Selection.activeGameObject=Place(selected,farm,point);
                        }
                        catch(Exception error){ShowNotification(new GUIContent(error.Message));}
                    }
                }
                EditorGUILayout.HelpBox("변경은 자동 저장·Undo 지원. 씬 배치는 직접 저장하세요. 남은 수량은 유지되며 전량을 획득한 뒤 재생 시간을 셉니다. 재생을 끄면 한 번만 획득합니다.",MessageType.Info);
                var report=new SetupValidationReport();SetupDataValidator.CheckForage(selected,Resources.Load<SaveItemCatalog>("SaveItemCatalog"),report,new System.Collections.Generic.HashSet<PlantDefinition>(),true);ManagementEditorUI.DrawIssues(report);
            }
            EditorGUILayout.EndScrollView();
        }
    }
    internal static GameObject Place(ForageDefinition profile,FarmGame farm,Vector2 point)
    {
        if(ManagementEditorUI.Playing || profile==null || profile.item==null || farm.Layout==null)throw new InvalidOperationException("Play를 정지하고 채집 아이템을 지정하세요.");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/"+profile.item.name+"Pickup.prefab")??AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/StonePickup.prefab");
        var root=MapPlacement.Place(farm.Layout,prefab,point,.8f,MapPlacement.Category.Items);root.name=profile.label;
        var marker=root.GetComponent<FarmItemPlacement>();Undo.RecordObject(marker,"Assign forage");marker.forage=profile;marker.item=profile.item;PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
        var id=root.GetComponent<MapObjectId>();if(id==null)id=Undo.AddComponent<MapObjectId>(root);else Undo.RecordObject(id,"Assign forage ID");id.AssignNew();PrefabUtility.RecordPrefabInstancePropertyModifications(id);
        var renderer=root.GetComponent<SpriteRenderer>();if(renderer!=null){Undo.RecordObject(renderer,"Forage preview");var art=new FarmArt();renderer.sprite=profile.item.itemSprite!=null?profile.item.itemSprite:EditableMapBuilder.PreviewIcon(art.ItemIndex(profile.item));PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
        return root;
    }
}
