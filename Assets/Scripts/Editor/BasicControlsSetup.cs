using UnityEditor;
using UnityEngine;
using EverythingGrows;
public static class BasicControlsSetup
{
    const string Path="Assets/Resources/PlayerControls.asset";
    public static void Prepare()
    {
        if(AssetDatabase.LoadAssetAtPath<PlayerControlSettings>(Path)==null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<PlayerControlSettings>(),Path);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Everything Grows/플레이어 조작 설정")]
    public static void Edit(){Prepare();Selection.activeObject=AssetDatabase.LoadAssetAtPath<PlayerControlSettings>(Path);EditorGUIUtility.PingObject(Selection.activeObject);}
}
