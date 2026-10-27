using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public sealed class FoundationTestEffect : ItemUseEffect
{
    public bool succeeds;public int calls;
    public override bool TryUse(FarmGame farm,PlantDefinition item,Vector2 point) { calls++;return succeeds; }
}
public static class FoundationExpansionCheck
{
    private static void Check(bool value,string text) { if(!value)throw new Exception(text); }
    private static FarmGame Open(string path)
    {
        GameSession.ResetForTests();EditorSceneManager.OpenScene(path);var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();farm.Initialize(false);GameSession.Bind(farm);
        Check(farm.IsInitialized && string.IsNullOrEmpty(GameSession.LastError),farm.InitializationError+GameSession.LastError);return farm;
    }
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new Exception("Isolated project and save only.");
        EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");MenuFoundationSetup.Ensure();ProjectSetupCheck.Run();
        var f=Open(TowerDungeonSetup.Battle);InventoryCheckAccess.Clear(f);
        var hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");var effect=ScriptableObject.CreateInstance<FoundationTestEffect>();hoe.useEffect=effect;hoe.consumeOnUse=true;
        f.AddItem(hoe);f.AddItem(hoe);int count=f.InventoryCount;
        Check(!f.UseSelectedItem(f.PlayerPosition,true) && f.InventoryCount==count && effect.calls==1,"Failed effect consumed item");
        effect.succeeds=true;Check(f.UseSelectedItem(f.PlayerPosition,true) && f.InventoryCount==count-1,"Successful effect consumption failed");
        Check(!f.UseSelectedItem(f.PlayerPosition,true) && f.InventoryCount==count-1,"Effect cooldown consumed again");
        hoe.useEffect=null;hoe.consumeOnUse=false;UnityEngine.Object.DestroyImmediate(effect);
        var type=MonsterType.Default(false);float oldHealth=type.maxHealth;var oldBehavior=type.behavior;
        type.maxHealth=123;type.behavior=MonsterBehavior.Ranged;f.Combat.SpawnEnemy(new Vector2(9,5),false);
        var enemy=CombatCheckAccess.Enemies(f.Combat).Cast<object>().Last();
        Check((float)CombatCheckAccess.Read(enemy,"health")==123 && (bool)CombatCheckAccess.Read(enemy,"ranged"),"Configured monster health/behavior ignored");type.maxHealth=oldHealth;type.behavior=oldBehavior;
        Check(GameSession.Save(f),"Pre-menu save failed");string original=File.ReadAllText(GameSession.SavePath);
        Directory.CreateDirectory(GameSession.SavePath+".tmp");Check(!GameSession.Save(f) && File.ReadAllText(GameSession.SavePath)==original,"Failed save replaced original");Directory.Delete(GameSession.SavePath+".tmp");
        Check(GameSession.Save(f) && string.IsNullOrEmpty(GameSession.LastError),"Save retry failed");
        GameSession.ReleaseToTitle();Check(GameSession.TryContinue(out var scene,out _) && scene==TowerDungeonSetup.Battle,"Continue did not validate saved scene");
        File.Move(GameSession.SavePath,GameSession.SavePath+".held");GameSession.ReleaseToTitle();Check(GameSession.TryContinue(out _,out _),"Backup-only continuation failed");File.Move(GameSession.SavePath+".held",GameSession.SavePath);
        Check(GameSession.PrepareNewGame(out var reason) && !GameSession.HasSave,reason);
        Check(Directory.GetFiles(Path.Combine(GameSession.SaveDirectory,"PreviousRuns"),"wtf-autosave.json",SearchOption.AllDirectories).Any(p=>File.ReadAllText(p)==original),"Old save not archived intact");

        const string createdPath="Assets/Scenes/FoundationTestFloor.unity";
        EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/GeneralStore.unity");var active=SceneManager.GetActiveScene();string before=File.ReadAllText(active.path);
        var work=new GameObject("Unsaved work retention");SceneManager.MoveGameObjectToScene(work,active);EditorSceneManager.MarkSceneDirty(active);var builds=EditorBuildSettings.scenes;
        Scene created=default;
        try
        {
            created=TowerFloorWindow.CreateFloor(TowerDungeonSetup.Battle,createdPath,3,DungeonFloorKind.Boss,TowerDungeonSetup.Lobby,"tower_from_second",null,"from_previous");
            var floor=ManagementEditorUI.Components<DungeonFloor>(created).Single();
            Check(floor.floorNumber==3 && floor.kind==DungeonFloorKind.Boss && floor.BlocksStairs(1) && !floor.BlocksStairs(0),"Floor metadata/clear condition wrong");
            Check(ManagementEditorUI.Components<EnemyPlacement>(created).Length==0 && ManagementEditorUI.Components<MapArrivalPoint>(created).Length==2,"Template did not clear enemies/create standard arrivals");
            var stair=ManagementEditorUI.Components<MapExit>(created).Single();Check(stair.requireInteract && stair.requireClearFloor && !MapConnectionsWindow.CheckExit(stair).HasErrors,"Generated stair connection invalid");
            bool refused=false;try { TowerFloorWindow.CreateFloor(TowerDungeonSetup.Battle,createdPath,4,DungeonFloorKind.Normal,TowerDungeonSetup.Lobby,"tower_from_second",null,null); }catch(InvalidOperationException) { refused=true; }
            Check(refused && SceneManager.GetActiveScene()==active && active.isDirty && work!=null && File.ReadAllText(active.path)==before,"Floor creation overwrote existing/dirty user scene");
            floor.clearRule=FloorClearRule.NoCombat;Check(!floor.BlocksStairs(10),"Preparation clear rule ignored");
        }
        finally { if(created.IsValid())EditorSceneManager.CloseScene(created,true);AssetDatabase.DeleteAsset(createdPath);EditorBuildSettings.scenes=builds; }
        Debug.Log("FOUNDATION_EXPANSION_SUCCESS: effect success/failure/consumption/cooldown, typed monster stats/behavior, safe save failure/retry/continue/backup/archive, floor template/metadata/arrivals/locks/unique path and dirty scene preservation.");
    }
}
