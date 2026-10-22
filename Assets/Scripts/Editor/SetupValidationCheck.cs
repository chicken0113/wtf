using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class SetupValidationCheck
{
    public static void VerifyNormalWorld()
    {
        Guard();
        ProjectSetupCheck.Run();
        InteriorWorldCheck.Run();
    }
    private static void Guard()
    {
        if (!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Fault injection requires an isolated temporary project.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static bool Has(SetupValidationReport report, string code) => report.Issues.Any(issue => issue.Code == code);
    private static PlantDefinition[] Definitions() => new[] { "Hoe", "WateringCan", "Stone", "Cloud", "InventorySlot" }
        .Select(name => AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/" + name + ".asset")).ToArray();
    public static void Run()
    {
        Guard();
        CheckDataFailures();
        CheckStartupFailures();
        CheckFallbacks();
        CheckReadOnlySceneValidation();
        Debug.Log("SETUP_FAILURE_CHECK_SUCCESS: missing definitions/time, invalid IDs/numbers/seed results, no partial startup or save overwrite, retry after preflight repair, optional controls/art fallbacks and read-only scene checking.");
    }

    private static void CheckDataFailures()
    {
        var data = GameSetupResources.Load();
        var cycle = UnityEngine.Object.Instantiate(data.Cycle);
        var controls = UnityEngine.Object.Instantiate(data.Controls);
        var catalog = ScriptableObject.CreateInstance<SaveItemCatalog>();
        var starting = ScriptableObject.CreateInstance<StartingInventory>();
        var seed = ScriptableObject.CreateInstance<PlantDefinition>();
        try
        {
            cycle.daylightSeconds = float.NaN;
            controls.moveSpeed = float.PositiveInfinity;
            seed.name = "Unregistered broken seed";
            seed.isSeed = true;
            catalog.entries = new[] { new SaveItemCatalog.Entry { id = "same", item = seed }, new SaveItemCatalog.Entry { id = "same", item = data.Heart }, null };
            starting.items = new[] { new StartingItem() };
            data.Cycle = cycle; data.Controls = controls; data.Catalog = catalog; data.Starting = starting;
            var report = new SetupValidationReport();
            SetupDataValidator.CheckResources(data, report, true, true);
            Check(Has(report, "NUMBER_POSITIVE") && Has(report, "CATALOG_DUPLICATE") && Has(report, "CATALOG_ENTRY") && Has(report, "STARTING_ITEM") && Has(report, "SEED_RESULT") && Has(report, "ITEM_UNREGISTERED"), "Invalid data was not diagnosed");
            Check(report.Issues.All(issue => issue.Message.Contains("수정:")), "Diagnostic lacks repair guidance");
            data = GameSetupResources.Load(); data.Catalog = null; data.Starting = null; data.Cycle = null; data.Heart = null; data.HouseDoor = null;
            report = new SetupValidationReport(); SetupDataValidator.CheckResources(data, report, true, true);
            Check(Has(report, "CATALOG_MISSING") && Has(report, "STARTING_MISSING") && Has(report, "TIME_MISSING") && Has(report, "HEART_MISSING") && Has(report, "HOUSE_DOOR_MISSING"), "Missing required resources were accepted");
        }
        finally { foreach (var obj in new UnityEngine.Object[] { cycle, controls, catalog, starting, seed }) UnityEngine.Object.DestroyImmediate(obj); }
    }

    private static FarmGame NewFarm()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var farm = new GameObject("Startup fault fixture").AddComponent<FarmGame>();
        farm.Configure(Definitions());
        return farm;
    }

    private static void CheckStartupFailures()
    {
        string previous = GameSession.DirectoryOverride;
        string directory = Path.Combine(Path.GetTempPath(), "Wtf-SetupFaultSave-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        GameSession.DirectoryOverride = directory; GameSession.ResetForTests();
        const string sentinel = "existing user save must stay byte-identical";
        File.WriteAllText(GameSession.SavePath, sentinel);
        try
        {
            var farm = NewFarm(); farm.Configure(null); farm.Initialize(false);
            Check(!farm.IsInitialized && !farm.enabled && farm.Combat == null && farm.transform.childCount == 0 && farm.InitializationError.Contains("DEFINITIONS_MISSING"), "Missing definitions left partial runtime state");
            Check(!GameSession.Save(farm) && File.ReadAllText(GameSession.SavePath) == sentinel, "Failed startup overwrote existing save");
            Check(!GameSession.EnterHouse(farm, "missing", "entry") && !GameSession.ReturnFromHouse(farm, "missing", "entry"), "Failed startup attempted house save/load");
            var manager = farm.gameObject.AddComponent<NpcManager>(); manager.Initialize(farm);
            Check(!manager.CanTrade && !manager.Open(null), "NPC interaction reached incomplete combat state");
            foreach (var name in new[] { "Update", "LateUpdate", "OnGUI" })
                typeof(FarmGame).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(farm, null);
            GameSession.ResetForTests(); farm.Configure(Definitions()); farm.Initialize(false);
            Check(farm.IsInitialized && farm.enabled && farm.Combat != null && farm.InitializationError == null, "Preflight repair could not initialize");
            var cycleAsset = Resources.Load<DayNightSettings>("WorldCycle");
            WithHiddenResource(cycleAsset, () => {
                var broken = NewFarm(); broken.Initialize(false);
                Check(!broken.IsInitialized && broken.Combat == null && broken.transform.childCount == 0 && broken.InitializationError.Contains("TIME_MISSING"), "Missing time settings reached runtime construction");
                var cycle = broken.gameObject.AddComponent<DayNightCycle>(); cycle.Initialize(broken);
                Check(!cycle.enabled && cycle.Settings == null, "Standalone cycle failure kept running"); cycle.Tick(1);
                Check(!GameSession.Save(broken) && File.ReadAllText(GameSession.SavePath) == sentinel, "Missing time settings modified save");
            });
        }
        finally { GameSession.DirectoryOverride = previous; GameSession.ResetForTests(); }
    }

    private static void CheckFallbacks()
    {
        var data = GameSetupResources.Load();
        WithHiddenResource(data.Controls, () => WithHiddenResource(data.Atlas, () => {
            var missing = GameSetupResources.Load();
            Check(missing.Controls == null && missing.Atlas == null, "Fault resources remain visible in Resources");
            var report = new SetupValidationReport(); SetupDataValidator.CheckResources(missing, report, false, false);
            Check(!report.HasErrors && Has(report, "CONTROLS_DEFAULT") && Has(report, "ART_FALLBACK"), "Optional resources became fatal");
            var farm = NewFarm(); farm.Initialize(false);
            Check(farm.IsInitialized && farm.Controls == null && farm.MoveSpeed == 4.5f, "Default controls did not initialize");
            farm.DropItem(Definitions()[1], Vector2.right);
            Check(farm.AddItem(Definitions()[1]) && farm.Till(Vector2Int.zero) && farm.PlantSelected(Vector2Int.zero), "Fallback artwork prevented item use/planting");
            farm.Water(Vector2.zero, .1f); farm.Tick(10);
            Check(farm.IsMature(Vector2Int.zero) && farm.GetComponentsInChildren<SpriteRenderer>().All(renderer => renderer.sprite != null), "Fallback artwork caused missing sprite or growth failure");
        }));
    }

    private static void WithHiddenResource(UnityEngine.Object resource, Action action)
    {
        var path = AssetDatabase.GetAssetPath(resource);
        var hidden = "Assets/SetupHidden-" + Guid.NewGuid().ToString("N") + Path.GetExtension(path);
        Resources.UnloadAsset(resource);
        Check(string.IsNullOrEmpty(AssetDatabase.MoveAsset(path, hidden)), "Could not hide test resource " + path);
        try { action(); }
        finally { Check(string.IsNullOrEmpty(AssetDatabase.MoveAsset(hidden, path)), "Could not restore test resource " + path); AssetDatabase.Refresh(); }
    }

    private static void CheckReadOnlySceneValidation()
    {
        EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var farm = UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        var broken = new GameObject("Unsaved broken exit").AddComponent<MapExit>();
        broken.transform.SetParent(farm.transform); broken.destinationScene = "Assets/Scenes/Missing.unity";
        EditorSceneManager.MarkSceneDirty(scene);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        bool rejected = false;
        try { ProjectSetupCheck.Run(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && scene.isLoaded && scene.isDirty && broken != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene() == scene, "Validation discarded scene edits or accepted invalid exit");
        Check(setup.Select(entry => entry.path).SequenceEqual(EditorSceneManager.GetSceneManagerSetup().Select(entry => entry.path)), "Validation changed the open scene set");
        UnityEngine.Object.DestroyImmediate(broken.gameObject);
    }
}
