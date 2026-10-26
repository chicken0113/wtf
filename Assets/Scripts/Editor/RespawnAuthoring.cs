using UnityEditor;
using UnityEngine;
using EverythingGrows;
public static class RespawnAuthoring
{
    [MenuItem("GameObject/Everything Grows/부활 침대",false,23)]
    public static void CreateBed()
    {
        var root=new GameObject("Respawn Bed");Undo.RegisterCreatedObjectUndo(root,"Create respawn bed");
        if(Selection.activeTransform!=null)root.transform.SetParent(Selection.activeTransform,false);
        var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.color=new Color(.25f,.52f,.7f);renderer.sortingOrder=5;root.transform.localScale=new Vector3(.9f,1.3f,1);
        root.AddComponent<RespawnAnchor>();var marker=root.AddComponent<MapObjectId>();marker.AssignNew();
        Selection.activeGameObject=root;
    }
}
