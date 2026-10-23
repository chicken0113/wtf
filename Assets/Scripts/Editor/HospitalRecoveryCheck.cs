using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class HospitalRecoveryCheck
{
    private const string Key="HospitalRecoveryCheck";
    private static int step;
    private static bool resume;
    private static double deadline;
    private static readonly string[] Route={WorldFrameworkSetup.Town,TriangleWorldSetup.Field,TowerDungeonSetup.Lobby,TowerDungeonSetup.Battle};
    static HospitalRecoveryCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        { resume=SessionState.GetBool(Key+"Resume",false);deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick; }
    }
    private static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    private static void Guard()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) ||
            !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use isolated project and saves.");
    }
    public static void Prepare()
    {
        Guard();HospitalSetup.Apply();ProjectSetupCheck.Run();WorldRouteAudit.Validate();
        var town=File.ReadAllText(WorldFrameworkSetup.Town);var hospital=File.ReadAllText(GameSession.HospitalScene);
        HospitalSetup.Apply();Check(File.ReadAllText(WorldFrameworkSetup.Town)==town && File.ReadAllText(GameSession.HospitalScene)==hospital,"Authoring overwrote existing layout");
    }
    public static void Run() { Guard();Start(false); }
    public static void Resume() { Guard();Start(true); }
    private static void Start(bool continuing)
    {
        EditorSceneManager.OpenScene(TowerDungeonSetup.Battle);
        SessionState.SetBool(Key+"Resume",continuing);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static string Inventory(FarmGame farm)
    {
        var player=farm.CapturePlayer(Resources.Load<SaveItemCatalog>("SaveItemCatalog"));
        player.position=Vector2.zero;player.cycleElapsed=0;return JsonUtility.ToJson(player);
    }
    private static void VerifyHospital(FarmGame farm)
    {
        Check(farm.gameObject.scene.path==GameSession.HospitalScene,"Did not arrive at hospital");
        Check(farm.Combat.Health==farm.Combat.HalfHeartHealth && farm.Combat.HeartCount==3 && !farm.Combat.IsGameOver,"Wrong recovery health or lost heart capacity");
        Check(farm.Gold==321 && Inventory(farm)==File.ReadAllText(GameSession.SavePath+".player"),"Hospital changed inventory/gold/heart capacity or kill count");
        var arrival=farm.GetComponentsInChildren<MapArrivalPoint>().Single(a=>a.arrivalId==GameSession.HospitalRecoveryArrival);
        Check(Vector2.Distance(farm.PlayerPosition,arrival.transform.position)<.01f && new MapCollision(farm).CanStand(farm.PlayerPosition,farm.PlayerRadius),"Recovery point blocked or wrong");
        farm.Combat.TickCombat(20);Check(farm.Combat.Health==farm.Combat.HalfHeartHealth,"Hospital passively healed recovery health");
        var saved=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
        Check(saved.currentScene==GameSession.HospitalScene && saved.player.health==farm.Combat.HalfHeartHealth && saved.player.gold==321,"Hospital arrival was not autosaved");
        var battle=saved.maps.Single(m=>m.scene==TowerDungeonSetup.Battle);
        Check(!battle.combatCleared && battle.defeatedEnemies.Count==1 && battle.pieces.Count==1,"Death lost partial floor state or duplicated loot");
    }
    private static void Travel(FarmGame farm,string target)
    {
        var exit=farm.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==target);
        farm.TeleportPlayer(exit.transform.position);
        Check(exit.requireInteract?farm.InteractWorld(exit.transform.position):MapExit.TryAutomatic(farm),"Cannot leave hospital/return to tower: "+GameSession.LastError);
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Hospital check timed out: "+step);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.4f || GameSession.Busy || Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(farm==null || !farm.IsInitialized)return;
            farm.enabled=false;Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(resume)
            {
                VerifyHospital(farm);Finish("HOSPITAL_RESTART_SUCCESS: saved hospital arrival, half heart, retained inventory/gold and partial floor state.");return;
            }
            if(step==0)
            {
                var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");var player=farm.CapturePlayer(catalog);player.gold=321;player.health=60;player.removedHearts=2;farm.RestorePlayer(player,catalog);
                Check(farm.AddItem(Resources.Load<PlantDefinition>("Equipment/Sword")),"Sword fixture missing");
                var chaser=farm.Combat.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Chaser" && r.enabled);
                farm.TeleportPlayer((Vector2)chaser.transform.position+Vector2.down);
                var sword=UnityEngine.Object.Instantiate(Resources.Load<PlantDefinition>("Equipment/Sword"));sword.damage=1000;sword.attackRange=3;
                Check(farm.Combat.Attack(sword,chaser.transform.position),"Cannot defeat partial-floor enemy");UnityEngine.Object.Destroy(sword);
                farm.TeleportPlayer(new Vector2(8,-5));farm.Combat.TickCombat(1.1f);farm.Combat.DamagePlayer(200);
                Check(GameSession.Busy && farm.Combat.Health==farm.Combat.HalfHeartHealth,"Lethal damage did not initiate half-heart recovery");
                var snapshot=farm.CapturePlayer(catalog);snapshot.position=Vector2.zero;snapshot.cycleElapsed=0;
                File.WriteAllText(GameSession.SavePath+".player",JsonUtility.ToJson(snapshot));step=1;return;
            }
            if(step==1)
            {
                VerifyHospital(farm);File.Copy(GameSession.SavePath,GameSession.SavePath+".first",true);
                var door=farm.GetComponentsInChildren<MapExit>().Single();farm.TeleportPlayer(door.transform.position);
                Check(!door.TryActivate(farm,false) && !GameSession.Busy,"Hospital door used without F");
                step=2;Travel(farm,Route[0]);return;
            }
            if(step>=2 && step<5) { int index=step-1;step++;Travel(farm,Route[index]);return; }
            if(step==5)
            {
                Check(farm.Combat.LivingHostileCount==1 && !farm.Combat.FloorCleared && farm.WorldPieceCount==1,"Returning after death reset partial floor or loot");
                CombatCheckAccess.DefeatHostiles(farm.Combat);Check(farm.Combat.FloorCleared,"Remaining enemy did not clear floor");
                farm.TeleportPlayer(new Vector2(8,-5));farm.Combat.TickCombat(1.1f);farm.Combat.DamagePlayer(200);Check(GameSession.Busy,"Second defeat failed");step=6;return;
            }
            Check(farm.gameObject.scene.path==GameSession.HospitalScene && farm.Combat.Health==farm.Combat.HalfHeartHealth,"Second recovery failed");
            Check(GameSession.Save(farm),"Recovery save failed");
            var saved=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
            Check(saved.maps.Single(m=>m.scene==TowerDungeonSetup.Battle).combatCleared,"Death reset cleared floor");
            // Keep a separate first-recovery snapshot for restart checks of the partial floor.
            File.Copy(GameSession.SavePath+".first",GameSession.SavePath,true);
            Finish("HOSPITAL_RECOVERY_SUCCESS: lethal damage, F doors, half-heart/no loss/autosave, partial and cleared floor retention.");
        }
        catch(Exception problem) { SessionState.SetBool(Key,false);Debug.LogException(problem);EditorApplication.Exit(1); }
    }
    private static void Finish(string message)
    { SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(message);EditorApplication.Exit(0); }
}
