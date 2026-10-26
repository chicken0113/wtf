using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
[InitializeOnLoad]
public static class RegionalMonstersCheck
{
    const string Key="RegionalMonstersCheckActive";
    const string Battle="Assets/Scenes/CombatTest.unity",Other="Assets/Scenes/TrainingGround.unity";
    static int step,baselineKills;static double deadline;static Vector2 woundedPosition;static float woundedHealth;
    static RegionalMonstersCheck(){if(Application.isBatchMode && SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        Check(Application.isBatchMode && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project/save only");
        EditorSceneManager.OpenScene(Battle);
        foreach(var region in Resources.LoadAll<SpawnRegion>("")){region.spawnEnabled=false;EditorUtility.SetDirty(region);}
        var a=MakeRegion("FixtureA",new Rect(-22,-14,18,28));var b=MakeRegion("FixtureB",new Rect(4,-14,18,28));
        var melee=MonsterType.Default(false);melee.spawnRegions=new[]{a};EditorUtility.SetDirty(melee);
        var ranged=MonsterType.Default(true);ranged.spawnRegions=new[]{b};EditorUtility.SetDirty(ranged);
        var boss=UnityEngine.Object.Instantiate(melee);boss.name="FixtureBoss";boss.label="Fixture Boss";boss.isBoss=true;boss.saveId="fixture-boss";
        AssetDatabase.CreateAsset(boss,"Assets/Resources/Monsters/FixtureBoss.asset");
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        foreach(var marker in farm.GetComponentsInChildren<EnemyPlacement>())UnityEngine.Object.DestroyImmediate(marker.gameObject);
        var root=new GameObject("Fixed boss");root.transform.SetParent(farm.Layout.transform);root.transform.position=new Vector2(-16,8);
        var placement=root.AddComponent<EnemyPlacement>();placement.monsterType=boss;var id=root.AddComponent<MapObjectId>();id.AssignNew();
        var legacy=new GameObject("Legacy normal marker");legacy.transform.SetParent(farm.Layout.transform);legacy.transform.position=new Vector2(12,5);legacy.AddComponent<EnemyPlacement>().monsterType=melee;legacy.AddComponent<MapObjectId>().AssignNew();
        EditorSceneManager.SaveScene(farm.gameObject.scene);AssetDatabase.SaveAssets();
        ProjectSetupCheck.Run();
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static SpawnRegion MakeRegion(string id,Rect area)
    {
        var r=ScriptableObject.CreateInstance<SpawnRegion>();r.id=id;r.label=id;r.scenePath=Battle;r.useMapBounds=false;r.area=area;r.spawnInterval=.1f;r.maxAlive=2;
        AssetDatabase.CreateAsset(r,"Assets/Resources/SpawnRegions/"+id+".asset");return r;
    }
    static object[] Enemies(FarmGame f)=>CombatCheckAccess.Enemies(f.Combat).Cast<object>().Where(e=>(float)CombatCheckAccess.Read(e,"health")>0).ToArray();
    static bool Boss(object enemy)=>((MonsterType)CombatCheckAccess.Read(enemy,"monsterType")).isBoss;
    static string Region(object enemy)=>(string)CombatCheckAccess.Read(enemy,"regionId");
    static SpriteRenderer Model(object enemy)=>(SpriteRenderer)CombatCheckAccess.Read(enemy,"model");
    static MapSave SavedMap()=>JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath)).maps.Single(m=>m.scene==Battle);
    static void Position(FarmGame f,Vector2 point){f.TeleportPlayer(point);Camera.main.transform.position=(Vector3)point+Vector3.back*10;}
    static void Night(FarmGame f)=>f.Cycle.RestoreElapsed(f.Cycle.Settings.daylightSeconds+f.Cycle.Settings.duskSeconds+1);
    static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup<deadline,"Regional test timeout: "+step);
            if(!EditorApplication.isPlaying)return;
            var loadedFarm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(loadedFarm!=null && loadedFarm.IsInitialized)loadedFarm.enabled=false;
            if(GameSession.Busy || Time.timeSinceLevelLoad<.1f)return;
            var f=GameSession.CurrentFarm;if(f==null || !f.IsInitialized)return;f.enabled=false;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(step==0)
            {
                Check(f.Combat.UsesRegions,"Region configuration not loaded");
                Check(Enemies(f).Where(e=>!Boss(e)).All(e=>string.IsNullOrEmpty((string)CombatCheckAccess.Read(e,"placementId"))),"Legacy normal markers bypassed the daytime regional spawner");Position(f,new Vector2(-10,-7));f.Cycle.RestoreElapsed(0);
                Check(!f.IsSafe(Vector2.zero) && new MapCollision(f,true).CanStand(Vector2.zero,.1f)==new MapCollision(f).CanStand(Vector2.zero,.1f),"Old safe zone remains");
                for(int i=0;i<4;i++)f.Combat.TickCombat(.11f);
                var normal=Enemies(f).Where(e=>!Boss(e)).ToArray();Check(normal.Length==2 && normal.All(e=>Region(e)=="FixtureA" && ((MonsterType)CombatCheckAccess.Read(e,"monsterType"))==MonsterType.Default(false)),"Region eligibility/max population failed");
                baselineKills=f.Combat.Kills;Night(f);var wounded=normal[0];woundedPosition=Model(wounded).transform.position;
                f.Combat.TickCombat(.2f);Check(normal.All(e=>(bool)CombatCheckAccess.Read(e,"sleeping")) && (Vector2)Model(wounded).transform.position==woundedPosition,"Sleeping enemies moved");
                CombatCheckAccess.HitEnemy(f.Combat,wounded,1);Check(!(bool)CombatCheckAccess.Read(wounded,"sleeping") && (bool)CombatCheckAccess.Read(wounded,"provoked") && (float)CombatCheckAccess.Read(wounded,"cooldown")==0,"Night hit did not immediately provoke");
                woundedHealth=(float)CombatCheckAccess.Read(wounded,"health");Position(f,new Vector2(14,-7));f.Combat.TickCombat(.11f);
                woundedPosition=Model(wounded).transform.position;Check(Enemies(f).Length==3,"Night entry spawned or removed enemies");
                Check(GameSession.Travel(f,Other,null,new Vector2(9,-5)),"Night departure failed");step=1;return;
            }
            if(step==1)
            {
                Check(!f.Cycle.IsDaytime && SavedMap().regionalEnemies.Count==3,"Night unloading removed population");
                Check(GameSession.Travel(f,Battle,null,new Vector2(14,-7)),"Night return failed");step=2;return;
            }
            if(step==2)
            {
                Position(f,new Vector2(14,-7));var normal=Enemies(f).Where(e=>!Boss(e)).ToArray();Check(normal.Length==2,"Night restore population mismatch");
                var wounded=normal.Single(e=>(bool)CombatCheckAccess.Read(e,"provoked"));Check(Mathf.Approximately((float)CombatCheckAccess.Read(wounded,"health"),woundedHealth) && Vector2.Distance(Model(wounded).transform.position,woundedPosition)<.3f,"Night health/position/wake state lost");
                Check(normal.Any(e=>(bool)CombatCheckAccess.Read(e,"sleeping")),"Unprovoked monster woke after travel");
                f.Cycle.RestoreElapsed(0);f.Combat.TickCombat(.11f);
                Check(Enemies(f).Count(Boss)==1 && Enemies(f).Where(e=>!Boss(e)).All(e=>Region(e)=="FixtureB"),"Day offscreen cleanup removed boss or retained normal A");
                Check(f.Combat.Kills==baselineKills,"Cleanup counted as a kill");
                Check(Enemies(f).Where(e=>!Boss(e)).All(e=>((MonsterType)CombatCheckAccess.Read(e,"monsterType"))==MonsterType.Default(true)),"Region B spawned disallowed monster");
                Night(f);Check(GameSession.Travel(f,Other,null,new Vector2(9,-5)),"Second night departure failed");step=3;return;
            }
            if(step==3)
            {
                Check(SavedMap().regionalEnemies.Count>1,"Night departure lost B population");
                f.Cycle.Tick(f.Cycle.CycleSeconds-f.Cycle.Elapsed+.1f);Check(f.Cycle.IsDaytime,"Day transition failed");Check(GameSession.Save(f),"Day cleanup save failed");
                Check(SavedMap().regionalEnemies.Count==1 && SavedMap().regionalEnemies[0].type=="fixture-boss","Unloaded daylight cleanup failed/boss removed");
                Check(GameSession.Travel(f,Battle,null,new Vector2(-10,-7)),"Day re-entry failed");step=4;return;
            }
            if(step==4)
            {
                Position(f,new Vector2(-10,-7));f.Combat.TickCombat(.11f);
                Check(Enemies(f).Any(e=>!Boss(e) && Region(e)=="FixtureA"),"Cleaned region did not restart spawning");
                var victim=Enemies(f).First(e=>!Boss(e));Night(f);int kills=f.Combat.Kills;CombatCheckAccess.DefeatEnemy(f.Combat,victim);
                Check(f.Combat.Kills==kills+1,"Sleeping combat kill lost normal rewards");
                Finish(0,"REGIONAL_MONSTERS_SUCCESS: type-region rules, max population, safe-zone removal, night sleep/hit wake, night persistence across maps, day offscreen/unloaded cleanup without kills, boss retention, re-entry spawning, sleeping kill rewards.");
            }
        }
        catch(Exception e){Debug.LogException(e);Finish(1,"REGIONAL_MONSTERS_FAILED step "+step+": "+e.Message);}
    }
    static void Finish(int code,string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(code);}
}
