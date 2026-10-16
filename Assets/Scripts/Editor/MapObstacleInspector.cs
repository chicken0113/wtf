using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using EverythingGrows;

[CustomEditor(typeof(MapObstacle))]
public sealed class MapObstacleInspector : Editor
{
    readonly BoxBoundsHandle bounds=new BoxBoundsHandle();
    void OnSceneGUI()
    {
        var obstacle=(MapObstacle)target;
        using(new Handles.DrawingScope(obstacle.transform.localToWorldMatrix))
        {
            bounds.center=obstacle.offset; bounds.size=new Vector3(obstacle.size.x,obstacle.size.y,0);
            EditorGUI.BeginChangeCheck(); bounds.DrawHandle();
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(obstacle,"Resize map obstacle"); obstacle.offset=bounds.center;
                obstacle.size=new Vector2(Mathf.Max(.05f,bounds.size.x),Mathf.Max(.05f,bounds.size.y));
                PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);
            }
        }
    }
    [MenuItem("GameObject/Everything Grows/Map Obstacle",false,22)]
    public static void Create()
    {
        var root=new GameObject("Map Obstacle"); Undo.RegisterCreatedObjectUndo(root,"Create map obstacle");
        if(Selection.activeTransform!=null) root.transform.SetParent(Selection.activeTransform,false);
        root.AddComponent<MapObstacle>(); Selection.activeGameObject=root;
    }
    [MenuItem("Tools/Everything Grows/Map travel/Create obstacle prefab")]
    public static void GeneratePrefab()
    {
        const string path="Assets/Prefabs/Map/MapObstacle.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null) return;
        var root=new GameObject("Map Obstacle"); root.AddComponent<MapObstacle>();
        PrefabUtility.SaveAsPrefabAsset(root,path); Object.DestroyImmediate(root);
    }
}
