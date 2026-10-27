using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public static class ManagementToolsCheck
{
    private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private static EditorWindow[] windows;
    private static double deadline;
    private static int ticks;
    private static string guiError;
    private static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    public static void Run()
    {
        if(!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new Exception("Isolated project only.");
        var menus=typeof(ItemManagerWindow).Assembly.GetTypes().SelectMany(t=>t.GetMethods(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
            .SelectMany(m=>m.GetCustomAttributes<MenuItem>()).Select(a=>a.menuItem).Where(m=>m.StartsWith("Tools/Everything Grows/")).ToArray();
        Check(menus.Length==15 && menus.Distinct().Count()==15,"Unexpected menu count/duplicates: "+menus.Length);
        foreach(string name in new[]{"아이템 관리","NPC 관리","길드 의뢰","맵 연결 관리","몬스터 관리","맵 배치"})Check(menus.Contains("Tools/Everything Grows/"+name),"Missing tool "+name);
        Check(!menus.Any(m=>m.Contains("Apply ") || m.Contains("Set up ") || m.Contains("Connect current farm")),"One-time menus still exposed");
        var items=ItemManagerWindow.FindItems();var item=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");
        Check(items.Contains(item) && ManagementEditorUI.Matches("Parsnip Seeds","parsnip"),"Item discovery/search failed");
        int price=item.sellPrice;var edited=new SerializedObject(item);Undo.IncrementCurrentGroup();edited.FindProperty("sellPrice").intValue=price+1;edited.ApplyModifiedProperties();
        Undo.FlushUndoRecordObjects();Undo.PerformUndo();Check(item.sellPrice==price,"Item Undo failed");
        var quests=Resources.Load<GuildQuestCatalog>("GuildQuests");Check(!GuildQuestWindow.CheckCatalog(quests).HasErrors,"Existing quests invalid");
        var copy=UnityEngine.Object.Instantiate(quests);copy.quests[1].id=copy.quests[0].id;Check(GuildQuestWindow.CheckCatalog(copy).HasErrors,"Duplicate quest ID accepted");UnityEngine.Object.DestroyImmediate(copy);

        const string path="Assets/Scenes/LegacyVillage/GeneralStore.unity";string saved=File.ReadAllText(path);var scene=EditorSceneManager.OpenScene(path);
        var manager=ManagementEditorUI.Components<NpcManager>(scene).First();var data=new SerializedObject(manager);var profile=data.FindProperty("npcs").GetArrayElementAtIndex(0);
        string greeting=profile.FindPropertyRelative("greeting").stringValue;Undo.IncrementCurrentGroup();profile.FindPropertyRelative("greeting").stringValue="Editor tool test";data.ApplyModifiedProperties();
        Undo.FlushUndoRecordObjects();Undo.PerformUndo();Check(manager.npcs[0].greeting==greeting,"NPC editing Undo failed");
        var work=new GameObject("Unsaved editor work check");SceneManager.MoveGameObjectToScene(work,scene);EditorSceneManager.MarkSceneDirty(scene);
        var exit=ManagementEditorUI.Components<MapExit>(scene).First();int count=SceneManager.sceneCount;
        Check(!MapConnectionsWindow.CheckExit(exit).HasErrors,"Existing map exit invalid");
        var exitData=new SerializedObject(exit);Undo.IncrementCurrentGroup();exitData.FindProperty("arrivalId").stringValue="missing_test_arrival";exitData.ApplyModifiedProperties();
        Check(MapConnectionsWindow.CheckExit(exit).HasErrors,"Missing arrival not reported");Undo.FlushUndoRecordObjects();Undo.PerformUndo();
        Check(!MapConnectionsWindow.CheckExit(exit).HasErrors && SceneManager.sceneCount==count && scene.isDirty && work!=null && File.ReadAllText(path)==saved,"Preview validation lost/saved dirty scene work or changed scene graph");
        Debug.Log("MANAGEMENT_TOOLS_RULES_SUCCESS: 15 menus, item discovery/search/Undo, guild diagnostics, NPC Undo, arrival errors and preview scene/unsaved work preservation.");
        Application.logMessageReceived+=OnLog;
        windows=new EditorWindow[]{EditorWindow.GetWindow<ItemManagerWindow>(),EditorWindow.GetWindow<NpcManagerWindow>(),EditorWindow.GetWindow<GuildQuestWindow>(),EditorWindow.GetWindow<MapConnectionsWindow>()};
        for(int i=0;i<windows.Length;i++) { windows[i].position=new Rect(30+i*30,30+i*30,900,650);windows[i].Show();windows[i].Repaint(); }
        deadline=EditorApplication.timeSinceStartup+45;EditorApplication.update+=Tick;
    }
    private static void OnLog(string message,string trace,LogType type)
    {
        if(type==LogType.Exception && new[]{"ItemManagerWindow","NpcManagerWindow","GuildQuestWindow","MapConnectionsWindow","ManagementEditorUI","PlantDefinitionInspector","MapExitInspector"}.Any(trace.Contains))guiError=message+trace;
    }
    private static void Tick()
    {
        try
        {
            Check(string.IsNullOrEmpty(guiError),guiError);
            foreach(var window in windows)window.Repaint();
            bool drawn=new[]{"inspector","manager","catalog","inspector"}.Select((name,i)=>windows[i].GetType().GetField(name,Members).GetValue(windows[i])).All(value=>value!=null);
            if(drawn && ++ticks>=12)
            {
                foreach(var window in windows)window.Close();EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
                Debug.Log("MANAGEMENT_TOOLS_GUI_SUCCESS: all four production windows rendered their real item/NPC/quest/map editors without GUI exceptions.");EditorApplication.Exit(0);
            }
            else Check(EditorApplication.timeSinceStartup<deadline,"Management window rendering timed out");
        }
        catch(Exception error) { EditorApplication.update-=Tick;Debug.LogException(error);EditorApplication.Exit(1); }
    }
}
