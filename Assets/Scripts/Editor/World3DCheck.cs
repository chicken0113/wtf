using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
public static class World3DCheck
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode&&Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project required");World3DSetup.Author();
        var scene=EditorSceneManager.OpenScene(GameMenu.FirstScene);var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();var world=farm.GetComponent<World3D>();Check(world!=null&&world.models!=null,"Missing 3D presentation");
        Check(farm.GetComponentsInChildren<MeshRenderer>().Length>1000,"Terrain is not made of mesh models");
        var camera=ManagementEditorUI.Components<Camera>(scene).Single();Check(camera.orthographic&&camera.transform.forward.z>.1f&&Mathf.Abs(camera.transform.forward.y)>.1f,"Camera is not an angled orthographic view");
        foreach(var point in new[]{farm.SpawnPosition,farm.SpawnPosition+new Vector2(3,2),farm.SpawnPosition+new Vector2(-2,-1)})
        {var screen=camera.WorldToScreenPoint(point);Check(Vector2.Distance(WorldProjection.GroundPoint(camera,screen),point)<.002f,"Ground projection drift");}
        var bounds=WorldProjection.GroundBounds(camera,0);Check(bounds.Contains(farm.SpawnPosition)&&!bounds.Contains((Vector2)camera.transform.position),"Visibility still uses shifted camera origin");
        var item=farm.GetComponentsInChildren<FarmItemPlacement>().First(p=>p.item!=null&&p.item.name=="Sword");var view=item.GetComponent<ModelView3D>();Check(view!=null&&view.Model!=null,"Pickup model missing after scene reload");view.Sync();
        var screenItem=camera.WorldToScreenPoint((Vector3)(Vector2)item.transform.position+Vector3.back*.3f);Check(Vector2.Distance(WorldProjection.Pointer(camera,screenItem),item.transform.position)<.01f,"Mesh picking does not resolve to logical item");
        string before=File.ReadAllText(GameMenu.FirstScene);Check(World3DSetup.Apply()&&before==File.ReadAllText(GameMenu.FirstScene),"Conversion reran over existing scene");
        Check(!string.IsNullOrEmpty(farm.GetComponentsInChildren<EnemyPlacement>().Single().GetComponent<MapObjectId>().Id),"Boss save ID lost");
        var placed=MapPlacement.Place(farm.Layout,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/GroundTile.prefab"),farm.SpawnPosition+Vector2.right*3,1,MapPlacement.Category.Ground);
        Check(placed.GetComponentsInChildren<MeshRenderer>().Length>0&&MapPlacement.EraseTarget(farm.Layout,placed.transform.position)==placed,"3D placement/erase target mismatch");Check(MapPlacement.Erase(farm.Layout,placed.transform.position),"Cannot erase 3D tile");
        Debug.Log("WORLD_3D_CHECK_PASSED: meshes, angled camera, projection, visibility, picking, idempotence and IDs");
    }
    public static void Preview()
    {
        var scene=EditorSceneManager.OpenScene(GameMenu.FirstScene);var camera=ManagementEditorUI.Components<Camera>(scene).Single();var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();farm.GetComponent<World3D>().ConfigureCamera(camera,farm.SpawnPosition);
        camera.orthographicSize=9;var texture=new RenderTexture(1200,850,24);camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
        var image=new Texture2D(1200,850,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,850),0,0);image.Apply();File.WriteAllBytes("Library/World3DPreview.png",image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(texture);Debug.Log("WORLD_3D_PREVIEW_RENDERED");
    }
}
