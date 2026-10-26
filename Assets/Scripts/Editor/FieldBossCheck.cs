using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
[InitializeOnLoad]
public static class FieldBossCheck
{
    const string Key="FieldBossCheckActive",Other="Assets/Scenes/TrainingGround.unity";
    static int step;static double deadline,respawnAt;static Vector2 home;
    static FieldBossCheck(){if(Application.isBatchMode && SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project/save only");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        Check(BossFoundationSetup.Apply(),"Prototype authoring failed");string first=File.ReadAllText(BossFoundationSetup.ScenePath);
        Check(BossFoundationSetup.Apply() && File.ReadAllText(BossFoundationSetup.ScenePath)==first,"Prototype authoring duplicated or changed scene");
        var type=Resources.Load<MonsterType>("Monsters/FieldBoss");type.bossRespawnSeconds=3;EditorUtility.SetDirty(type);
        foreach(var region in SpawnRegion.ForScene(BossFoundationSetup.ScenePath)){region.maxAlive=0;EditorUtility.SetDirty(region);}AssetDatabase.SaveAssets();
        ProjectSetupCheck.Run();EditorSceneManager.OpenScene(BossFoundationSetup.ScenePath);
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static object Boss(FarmGame f)=>CombatCheckAccess.Enemies(f.Combat).Cast<object>().Single(e=>((MonsterType)CombatCheckAccess.Read(e,"monsterType"))?.name=="FieldBoss");
    static float HP(object e)=>(float)CombatCheckAccess.Read(e,"health");
    static bool Flag(object e,string name)=>(bool)CombatCheckAccess.Read(e,name);
    static SpriteRenderer Model(object e)=>(SpriteRenderer)CombatCheckAccess.Read(e,"model");
    static GameSaveData Saved()=>JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
    static RegionalEnemySave SavedBoss()=>Saved().maps.Single(m=>m.scene==BossFoundationSetup.ScenePath).regionalEnemies.Single(e=>e.type==Resources.Load<MonsterType>("Monsters/FieldBoss").SaveKey);
    static void Position(FarmGame f,Vector2 point){f.TeleportPlayer(point);Camera.main.transform.position=(Vector3)point+Vector3.back*10;}
    static Vector2 Outside(FarmGame f)
    {
        var collision=new MapCollision(f);var bounds=f.WorldBounds;
        for(float x=bounds.xMin+1;x<bounds.xMax-1;x+=2)for(float y=bounds.yMin+1;y<bounds.yMax-1;y+=2)
        { var p=new Vector2(x,y);if(Vector2.Distance(p,home)>14 && collision.CanStand(p,.4f))return p; }
        throw new Exception("Fixture needs room outside boss leash");
    }
    static void Night(FarmGame f)=>f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+1);
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Boss check timeout: "+step);
            if(!EditorApplication.isPlaying)return;var loaded=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(loaded!=null && loaded.IsInitialized)loaded.enabled=false;
            if(GameSession.Busy || Time.timeSinceLevelLoad<.1f)return;
            var f=GameSession.CurrentFarm;if(f==null || !f.IsInitialized)return;Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(step==0)
            {
                var boss=Boss(f);var type=(MonsterType)CombatCheckAccess.Read(boss,"monsterType");home=(Vector2)CombatCheckAccess.Read(boss,"home");f.Cycle.RestoreElapsed(0);
                Position(f,home+Vector2.down*.8f);f.Combat.TickCombat(.1f);Check(Flag(boss,"bossEngaged"),"Nearby player did not engage");
                CombatCheckAccess.HitEnemy(f.Combat,boss,1);Model(boss).transform.position=home+Vector2.right;
                CombatCheckAccess.ShootOwned(f.Combat,boss,home,Vector2.up,1,20);
                int kills=f.Combat.Kills;Position(f,Outside(f));f.Combat.TickCombat(.1f);
                Check(HP(boss)==type.maxHealth && Flag(boss,"bossReturning") && !Flag(boss,"bossEngaged"),"Leash did not reset health/combat");
                CombatCheckAccess.HitEnemy(f.Combat,boss,1);Check(HP(boss)==type.maxHealth,"Returning boss can be farmed for damage");
                Check(CombatCheckAccess.Shots(f.Combat).Count==0 && f.Combat.Kills==kills,"Reset left boss shots or rewarded a kill");
                Night(f);f.Combat.TickCombat(.1f);Check(Flag(boss,"sleeping"),"Returning boss did not sleep at night");
                f.Combat.TickCombat(1);Check(!Flag(boss,"bossReturning") && Vector2.Distance(Model(boss).transform.position,home)<.15f,"Boss did not return home");
                Night(f);Position(f,home+Vector2.down*.8f);CombatCheckAccess.HitEnemy(f.Combat,boss,1);
                Check(!Flag(boss,"sleeping"),"Boss did not wake when hit");f.Combat.ResetBossEncounters();Check(HP(boss)==type.maxHealth && Flag(boss,"sleeping"),"Night reset broke sleep");
                f.Cycle.RestoreElapsed(0);CombatCheckAccess.DefeatEnemy(f.Combat,boss);respawnAt=SavedBoss().bossRespawnAt;
                Check(respawnAt>GameSession.WorldSeconds && SavedBoss().health==0,"Death deadline not saved");kills=f.Combat.Kills;CombatCheckAccess.DefeatEnemy(f.Combat,boss);Check(f.Combat.Kills==kills,"Repeated corpse hit duplicated rewards");
                GameSession.AdvanceTime(1);Check(GameSession.Travel(f,Other,null,new Vector2(9,-5)),"Cannot leave dead boss map");step=1;return;
            }
            if(step==1)
            {
                Check(SavedBoss().bossRespawnAt==respawnAt,"Travel reset respawn deadline");GameSession.AdvanceTime(4);Night(f);Check(GameSession.Save(f),"Timer save failed");
                Check(Saved().worldSeconds>respawnAt,"World timer did not advance remotely");
                Check(GameSession.Travel(f,BossFoundationSetup.ScenePath,null,home+Vector2.down*2),"Cannot return to boss");step=2;return;
            }
            if(step==2)
            {
                var boss=Boss(f);Check(HP(boss)==0 && (double)CombatCheckAccess.Read(boss,"bossRespawnAt")==respawnAt,"Dead boss/timer not restored");
                f.Combat.TickCombat(.1f);Check(HP(boss)==0,"Boss respawned at night");
                f.Cycle.RestoreElapsed(0);f.Combat.TickCombat(.1f);Check(HP(boss)==55 && Model(boss).enabled && (double)CombatCheckAccess.Read(boss,"bossRespawnAt")==0,"Daytime respawn failed");
                Position(f,Outside(f));f.Combat.TickCombat(1.1f);Position(f,home+Vector2.down*.8f);CombatCheckAccess.HitEnemy(f.Combat,boss,1);
                var before=f.Combat.Health;CombatCheckAccess.ShootOwned(f.Combat,boss,f.PlayerPosition+Vector2.left*.3f,Vector2.right*10,100000,1);
                f.Combat.TickCombat(.1f);
                Check(before>0 && f.Combat.Health==f.Combat.HalfHeartHealth && HP(boss)==55 && !Flag(boss,"bossEngaged") && (float)CombatCheckAccess.Read(boss,"windup")==0,"Lethal boss projectile did not reset safely");
                Check(Vector2.Distance(Model(boss).transform.position,home)<.01f,"Death did not return boss home");
                Finish(0,"FIELD_BOSS_SUCCESS: idempotent prototype authoring, engage/leash/return/reset immunity, owned-shot cancellation, night wake/sleep, one reward per death, saved remote respawn deadline, daytime-only reappearance and lethal-projectile player-death reset.");
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1,"FIELD_BOSS_FAILED step "+step+": "+e.Message);}
    }
    static void Finish(int code,string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(code);}
}
