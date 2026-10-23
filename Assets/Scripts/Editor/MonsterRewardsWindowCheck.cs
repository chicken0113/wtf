using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EverythingGrows;

public static class MonsterRewardsWindowCheck
{
    private static void Check(bool value,string text) { if(!value)throw new Exception(text); }
    public static void Run()
    {
        if(!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new Exception("Use an isolated project.");
        var paths=new List<string>();
        try
        {
            var defaults=MonsterRewardsWindow.FindTypes();
            Check(defaults.Contains(MonsterType.Default(false)) && defaults.Contains(MonsterType.Default(true)),"Existing kinds missing");
            string original=JsonUtility.ToJson(MonsterType.Default(false));
            var created=MonsterRewardsWindow.CreateType();paths.Add(AssetDatabase.GetAssetPath(created));
            Check(created.gold==0 && created.drops.Length==0 && !MonsterRewardsWindow.CheckType(created).HasErrors,"New kind not empty/valid");
            created.label="Reward Tool Search Test";created.gold=27;
            created.drops=new[]{new MonsterDrop {item=Resources.Load<PlantDefinition>("Equipment/Sword"),probability=.25f,minimum=2,maximum=5}};
            EditorUtility.SetDirty(created);AssetDatabase.SaveAssetIfDirty(created);
            var copy=MonsterRewardsWindow.CreateType(created);paths.Add(AssetDatabase.GetAssetPath(copy));
            Check(paths[0]!=paths[1] && AssetDatabase.AssetPathToGUID(paths[0])!=AssetDatabase.AssetPathToGUID(paths[1]),"Duplicate overwrote source");
            Check(copy.gold==27 && copy.drops.Length==1 && copy.drops[0].item==created.drops[0].item && copy.drops[0].maximum==5,"Duplicate lost settings/references");
            copy.drops[0].minimum=4;Check(created.drops[0].minimum==2,"Duplicate shared mutable drop rows");
            Check(MonsterRewardsWindow.Matches(created,"reward tool") && MonsterRewardsWindow.Matches(created,created.name) && !MonsterRewardsWindow.Matches(created,"not present"),"Search mismatch");
            var serialized=new SerializedObject(created);serialized.FindProperty("gold").intValue=99;serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();Undo.PerformUndo();Check(created.gold==27,"Serialized editing did not support Undo");
            AssetDatabase.SaveAssetIfDirty(created);AssetDatabase.ImportAsset(paths[0],ImportAssetOptions.ForceUpdate);
            Check(AssetDatabase.LoadAssetAtPath<MonsterType>(paths[0]).gold==27,"Undo/save persistence failed");
            copy.drops[0].item=null;Check(MonsterRewardsWindow.CheckType(copy).HasErrors,"Missing item not reported");
            copy.drops[0].item=Resources.Load<PlantDefinition>("Equipment/Sword");copy.drops[0].minimum=8;copy.drops[0].maximum=2;
            Check(MonsterRewardsWindow.CheckType(copy).HasErrors,"Invalid quantity not reported");
            Check(JsonUtility.ToJson(MonsterType.Default(false))==original,"Existing default kind was changed");
            Debug.Log("MONSTER_REWARDS_WINDOW_SUCCESS: discovery/search, empty creation, unique duplicate/deep rows/shared item references, serialized Undo/persistence, validation and original preservation.");
        }
        finally { foreach(var path in paths)AssetDatabase.DeleteAsset(path); }
    }
}
