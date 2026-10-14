using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public sealed class NpcManagerWindow : EditorWindow
{
    private Vector2 scroll;
    [MenuItem("Tools/Everything Grows/NPC manager")]
    public static void Open() => GetWindow<NpcManagerWindow>("NPC 매니저");
    private void OnGUI()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var manager=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<NpcManager>()).FirstOrDefault();
        if(manager==null) { EditorGUILayout.HelpBox("NPC 매니저가 있는 농장 씬을 열어주세요.",MessageType.Info); return; }
        EditorGUILayout.HelpBox("NPC마다 ID와 판매 목록을 설정하세요. NPC 오브젝트의 Npc Id를 같은 ID로 맞춥니다. Buy Price는 구매 가격, Sell Price는 이 NPC에게 판매할 가격(-1이면 아이템 기본 판매가)입니다.",MessageType.Info);
        using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var data=new SerializedObject(manager); data.Update();
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.PropertyField(data.FindProperty("startingGold"));
            EditorGUILayout.PropertyField(data.FindProperty("npcs"),true);
            if(data.ApplyModifiedProperties()) EditorSceneManager.MarkSceneDirty(scene);
            EditorGUILayout.Space(); EditorGUILayout.LabelField("모든 아이템 기본 판매가",EditorStyles.boldLabel);
            foreach(string guid in AssetDatabase.FindAssets("t:PlantDefinition"))
            {
                var item=AssetDatabase.LoadAssetAtPath<PlantDefinition>(AssetDatabase.GUIDToAssetPath(guid)); if(item==null) continue;
                EditorGUI.BeginChangeCheck(); int price=EditorGUILayout.IntField(item.label,item.sellPrice);
                if(EditorGUI.EndChangeCheck()) { Undo.RecordObject(item,"Change sale price"); item.sellPrice=Mathf.Max(0,price); EditorUtility.SetDirty(item); }
            }
            EditorGUILayout.EndScrollView();
            if(GUILayout.Button("NPC 설정과 가격 저장")) { AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene); }
        }
    }
}
