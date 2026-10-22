using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public static class ProjectSetupCheck
{
    [MenuItem("Tools/Everything Grows/Validation/Check project setup")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Play를 정지한 뒤 설정 검사를 실행하세요."); return; }
        var report = new SetupValidationReport();
        var resources = GameSetupResources.Load();
        SetupDataValidator.CheckResources(resources, report, true, true);
        var opened = new List<Scene>();
        var active = SceneManager.GetActiveScene();
        try
        {
            var paths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).Distinct().ToArray();
            if (paths.Length == 0) report.Error("BUILD_SCENES", null, "Build Settings", "활성 씬이 없습니다.", "게임 씬을 빌드 목록에 추가하세요.");
            foreach (var path in paths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) { report.Error("SCENE_MISSING", null, path, "씬 파일이 없습니다.", "씬 파일을 복원하거나 빌드 목록을 수정하세요."); continue; }
                var scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(scene); }
                var farms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FarmGame>()).ToArray();
                if (farms.Length != 1) report.Error("FARM_COUNT", null, path, "활성 FarmGame이 정확히 하나여야 합니다.", "중복 또는 누락된 게임 컴포넌트를 확인하세요.");
                else FarmSetupValidator.Check(farms[0], resources, report, true, true);
            }
        }
        finally
        {
            for (int i = opened.Count - 1; i >= 0; i--) EditorSceneManager.CloseScene(opened[i], true);
            if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        }
        report.Publish();
        // Reuse the existing graph/collision checks after local data is safe to inspect.
        if (!report.HasErrors)
        {
            try { WorldConnectionValidator.Validate(); }
            catch (Exception problem) { report.Error("WORLD_CONNECTIONS", null, "MapExit / MapArrivalPoint", problem.Message, "위 Console 오류의 대상 씬·오브젝트·도착 ID를 확인하세요."); Debug.LogError(report.FirstError); }
        }
        if (report.HasErrors) throw new InvalidOperationException("설정 검사 실패: " + report.ErrorCount + "개 오류. Console에서 해당 오브젝트를 선택해 수정하세요.");
        Debug.Log("PROJECT_SETUP_VALID: 기본 아이템·시간·저장 ID·NPC·씬 연결 검사 완료. 경고 " + report.Issues.Count + "개. 씬과 에셋은 변경하지 않았습니다.");
    }
}
