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
        var placed=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group);
        placed.transform.position=new Vector3(point.x,point.y,0); placed.transform.localScale=Vector3.one*Mathf.Max(.05f,scale);
        // Preserve existing blocks and render new overlapping sprites above their layer.
        foreach(var renderer in placed.GetComponentsInChildren<SpriteRenderer>())
        {
            int order=renderer.sortingOrder;
            foreach(Transform child in group)
            {
                if(child==placed.transform) continue;
                foreach(var other in child.GetComponentsInChildren<SpriteRenderer>())
                {
                    var a=renderer.bounds; var b=other.bounds;
                    bool overlap=a.max.x>b.min.x+.001f && b.max.x>a.min.x+.001f && a.max.y>b.min.y+.001f && b.max.y>a.min.y+.001f;
                    if(overlap && renderer.sortingLayerID==other.sortingLayerID) order=Mathf.Max(order,other.sortingOrder+1);
                }
            }
            renderer.sortingOrder=Mathf.Min(32767,order);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        Undo.RegisterCreatedObjectUndo(placed,"Place map object"); EditorSceneManager.MarkSceneDirty(map.gameObject.scene); return placed;
    }
    public static GameObject EraseTarget(FarmMapLayout map,Vector2 point)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || map==null) return null;
        GameObject target=null; int layer=int.MinValue,order=int.MinValue; float depth=float.PositiveInfinity;
        foreach(Category category in System.Enum.GetValues(typeof(Category)))
        {
            var group=map.transform.Find(category.ToString()); if(group==null) continue;
            foreach(var renderer in group.GetComponentsInChildren<SpriteRenderer>())
            {
                if(!renderer.enabled || renderer.sprite==null) continue;
                var bounds=renderer.bounds;
                if(point.x<bounds.min.x || point.x>bounds.max.x || point.y<bounds.min.y || point.y>bounds.max.y) continue;
                int candidateLayer=SortingLayer.GetLayerValueFromID(renderer.sortingLayerID); float candidateDepth=renderer.transform.position.z;
                if(candidateLayer<layer || (candidateLayer==layer && renderer.sortingOrder<order) || (candidateLayer==layer && renderer.sortingOrder==order && candidateDepth>depth)) continue;
                var root=renderer.transform; while(root.parent!=group && root.parent!=null) root=root.parent;
                target=root.gameObject; layer=candidateLayer; order=renderer.sortingOrder; depth=candidateDepth;
            }
        }
        return target;
    }
    public static bool Erase(FarmMapLayout map,Vector2 point)
    {
        var target=EraseTarget(map,point); if(target==null) return false;
        Undo.DestroyObjectImmediate(target); EditorSceneManager.MarkSceneDirty(map.gameObject.scene); return true;
    }
}
