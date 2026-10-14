using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class MapPlacement
{
    public enum Category { Ground, Paths, Decoration, Items, EnemySpawns }
    public static Category CategoryOf(GameObject prefab)
    {
        if(prefab.GetComponent<FarmGround>()!=null) return Category.Ground;
        if(prefab.GetComponent<FarmItemPlacement>()!=null) return Category.Items;
        if(prefab.GetComponent<EnemyPlacement>()!=null) return Category.EnemySpawns;
        return prefab.name.Contains("Path")?Category.Paths:Category.Decoration;
    }
    public static GameObject Place(FarmMapLayout map,GameObject prefab,Vector2 point,float scale,Category category)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || map==null || prefab==null || !PrefabUtility.IsPartOfPrefabAsset(prefab)) return null;
        var group=map.transform.Find(category.ToString());
        if(group==null)
        {
            var root=new GameObject(category.ToString()); root.transform.SetParent(map.transform,false);
            Undo.RegisterCreatedObjectUndo(root,"Create map layer"); group=root.transform;
        }
        // Tiles occupy one position per layer. Decorations and items may intentionally overlap.
        if(category==Category.Ground || category==Category.Paths)
            foreach(Transform child in group) if(Vector2.Distance(child.position,point)<.01f) return null;
        var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group);
        placed.transform.position=new Vector3(point.x,point.y,0); placed.transform.localScale=Vector3.one*Mathf.Max(.05f,scale);
        Undo.RegisterCreatedObjectUndo(placed,"Place map object"); EditorSceneManager.MarkSceneDirty(map.gameObject.scene); return placed;
    }
    public static bool Erase(FarmMapLayout map,Vector2 point,float radius,Category category)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || map==null) return false;
        var group=map.transform.Find(category.ToString()); if(group==null) return false;
        Transform nearest=null; float distance=radius;
        foreach(Transform child in group) { float candidate=Vector2.Distance(child.position,point); if(candidate<=distance) { nearest=child; distance=candidate; } }
        if(nearest==null) return false;
        Undo.DestroyObjectImmediate(nearest.gameObject); EditorSceneManager.MarkSceneDirty(map.gameObject.scene); return true;
    }
}
