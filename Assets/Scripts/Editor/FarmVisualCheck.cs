using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class FarmVisualCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new InvalidOperationException("Run in the isolated validation project.");
        PrototypeBuilder.CreateAndValidate();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var names=new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"};
        var data=new PlantDefinition[5];
        for(int i=0;i<data.Length;i++) data[i]=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+names[i]+".asset");
        var farm=new GameObject("Visual check").AddComponent<FarmGame>(); farm.Configure(data); farm.Initialize();
        for(int x=-1;x<=1;x++) for(int y=1;y<=2;y++) farm.Till(new Vector2Int(x,y));
        farm.AddItem(data[2]); farm.PlantSelected(new Vector2Int(-1,1)); farm.Water(new Vector2(-1,1),.1f); farm.Tick(6);
        farm.AddItem(data[1]); farm.PlantSelected(new Vector2Int(1,1)); farm.Water(new Vector2(1,1),.1f); farm.Tick(3);
        var camera=Camera.main;
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target; camera.Render();
        RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes("FarmVisualCheck.png",image.EncodeToPNG());
        camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("FARM_VISUAL_CHECK_SUCCESS");
    }
}
