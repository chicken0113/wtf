using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

// One-time authoring. Never rebuild the hospital or town on runtime startup.
public static class HospitalSetup
{
    public static bool Installed => File.Exists(GameSession.HospitalScene);
    private const string Marker = "Hospital connection 2026-10-23";
    private static void Label(Transform parent, string text, Vector2 position)
    { Root(text, parent, position).AddComponent<WorldLabel>().text = text; }

    // One-time authoring entry retained for fixtures; no production menu.
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Stop Play and save modified scenes before adding the hospital.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            if (!Installed) CreateRoom();
            ConnectTown();
            var scenes = EditorBuildSettings.scenes.ToList();
            var entry = scenes.Find(s => s.path == GameSession.HospitalScene);
            if (entry == null) scenes.Add(new EditorBuildSettingsScene(GameSession.HospitalScene, true));
            else entry.enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("HOSPITAL_AUTHORED: editable room, recovery arrival and F doors; existing town layout retained.");
        }
        finally
        {
            if(setup.Any(scene=>scene.isLoaded && scene.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);
        }
    }
    private static void CreateRoom()
    {
        var farm = InteriorWorldSetup.Room(GameSession.HospitalScene, new Vector2(18,14), new Color(.72f,.78f,.75f));
        farm.Layout.passiveHealthRecovery = false;
        var furniture = Group(farm.Layout.transform, "Hospital furniture");
        Solid(furniture, "Recovery bed", new Vector2(-4,2), new Vector2(2.2f,3), new Color(.4f,.47f,.5f));
        Draw("Bed linen", furniture, new Vector2(-4,1.8f), new Vector2(1.9f,2.3f), new Color(.72f,.87f,.88f), Depth(2)+1);
        Draw("Pillow", furniture, new Vector2(-4,3), new Vector2(1.7f,.6f), Color.white, Depth(2)+2);
        Solid(furniture, "Medical cabinet", new Vector2(5,3.8f), new Vector2(2,1.4f), new Color(.83f,.85f,.82f));
        Draw("Medical cross vertical", furniture, new Vector2(5,3.8f), new Vector2(.28f,.9f), new Color(.82f,.25f,.25f), Depth(3.8f)+1);
        Draw("Medical cross horizontal", furniture, new Vector2(5,3.8f), new Vector2(.9f,.28f), new Color(.82f,.25f,.25f), Depth(3.8f)+2);
        Solid(furniture, "Waiting bench", new Vector2(4,-1), new Vector2(3,.8f), new Color(.47f,.34f,.24f));
        Label(furniture, "Hospital", new Vector2(0,4.6f));
        var travel = Group(farm.Layout.transform, "Travel");
        InteriorWorldSetup.Door(travel, "Town", new Vector2(0,-5.2f), WorldFrameworkSetup.Town, "town_from_hospital");
        InteriorWorldSetup.Arrival(travel, "hospital_entry", new Vector2(0,-3.7f));
        InteriorWorldSetup.Arrival(travel, GameSession.HospitalRecoveryArrival, new Vector2(-2,1));
        EditorSceneManager.MarkSceneDirty(farm.gameObject.scene);
        EditorSceneManager.SaveScene(farm.gameObject.scene);
    }
    private static void ConnectTown()
    {
        var scene = EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);
        var farm = UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        if (farm.Layout.transform.Find(Marker) != null) return;
        var clinic = farm.Layout.GetComponentsInChildren<Transform>().Single(t => t.name == "Clinic exterior");
        var bounds = clinic.GetComponent<MapObstacle>().Bounds;
        var entry = new Vector2(bounds.center.x, bounds.yMin-.7f);
        InteriorWorldSetup.Door(clinic, "Hospital entrance", entry, GameSession.HospitalScene, "hospital_entry");
        InteriorWorldSetup.Arrival(Group(farm.Layout.transform, "Travel"), "town_from_hospital", entry+Vector2.down*1.3f);
        Root(Marker, farm.Layout.transform, Vector2.zero);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
