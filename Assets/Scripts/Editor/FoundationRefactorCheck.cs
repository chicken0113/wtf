using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Reuses behavioral checks; do not duplicate each subsystem's implementation here.
public static class FoundationRefactorCheck
{
    public static void Run()
    {
        if (!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Run foundation checks in an isolated temporary Unity project.");
        var builds = EditorBuildSettings.scenes;
        try
        {
            PrototypeBuilder.CreateAndValidate();
            HousePlantingCheck.Regression();
            HousePlantingCheck.Run();
            Debug.Log("FOUNDATION_REFACTOR_RULES_SUCCESS: inventory slots/storage/overflow, dry and watered growth, regrowth, heart harvest and house placement.");
        }
        finally { EditorBuildSettings.scenes = builds; }
    }
}
