using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class TowerDungeonCheck
{
    private const string Key="TowerDungeonPlayCheck";
    private static double deadline;
    private static int step;
    private static bool resume;
    private static int expectedKills=2, expectedBattleDrops=2;
    static TowerDungeonCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        { deadline=EditorApplication.timeSinceStartup+120;resume=SessionState.GetBool(Key+"Resume",false);EditorApplication.update+=Tick; }
    }
    private static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    private static void Guard()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")) ||
            !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use isolated temporary project and saves.");
    }
    public static void PrepareEnemyIds()
    {
        if(!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Prepare IDs in isolated copy.");
        EditorSceneManager.OpenScene(TowerDungeonSetup.Battle);MapSaveAuthoring.PrepareCurrentMap();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    public static void Run()
    {
        Guard();ProjectSetupCheck.Run();WorldRouteAudit.Validate();
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        var paths=new[]{TowerDungeonSetup.Lobby,TowerDungeonSetup.Battle,TriangleWorldSetup.Field};
        var contents=paths.Select(File.ReadAllText).ToArray();
        TowerDungeonSetup.Apply();
        Check(paths.Select(File.ReadAllText).SequenceEqual(contents),"Reapplying overwrote authored tower changes");
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path==active,"Authoring changed active scene");
        Start(false);
    }
    public static void Resume() { Guard();Start(true); }
    private static void Start(bool continuing)
    {
        EditorSceneManager.OpenScene(TriangleWorldSetup.Field);SessionState.SetBool(Key+"Resume",continuing);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static MapExit Exit(FarmGame farm,string target)=>farm.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==target);
    private static void Travel(FarmGame farm,string target,bool stairs)
    {
        var exit=Exit(farm,target);Check(exit.requireInteract==stairs,"Wrong activation for "+target);
        farm.TeleportPlayer(exit.transform.position);
        if(stairs)
        {
            Check(!MapExit.TryAutomatic(farm) && !exit.TryActivate(farm,false) && !GameSession.Busy,"Stairs moved without F");
            Check(!farm.FarmAction(exit.transform.position) && !GameSession.Busy,"E used stairs");
            Check(farm.InteractWorld(exit.transform.position),"F failed to use stairs: "+GameSession.LastError);
        }
        else Check(MapExit.TryAutomatic(farm),"Automatic exterior travel failed: "+GameSession.LastError);
        Check(GameSession.Busy,"Travel did not start");
    }
    private static void CheckStairBlocked(FarmGame farm)
    {
        var exit=Exit(farm,TowerDungeonSetup.Lobby);Check(exit.requireClearFloor,"Stair clear-floor setting missing");
        farm.TeleportPlayer(exit.transform.position);var before=File.ReadAllBytes(GameSession.SavePath);
        Check(exit.IsCombatBlocked(farm) && exit.InteractionPrompt(farm)=="몬스터를 모두 처치해야 합니다","Locked stair message missing");
        Check(!farm.InteractWorld(exit.transform.position) && !exit.Travel(farm) && !GameSession.Busy,"Living monster did not block F or direct stair travel");
        Check(File.ReadAllBytes(GameSession.SavePath).SequenceEqual(before),"Blocked staircase changed the saved game");
        bool previous=exit.requireClearFloor;exit.requireClearFloor=false;
        Check(!exit.IsCombatBlocked(farm),"Unrestricted doors were locked by monsters");exit.requireClearFloor=previous;
    }
    private static void InventoryAndHealth(FarmGame farm,float health,int kills)
    {
        Check(farm.Gold==123 && Mathf.Approximately(farm.Combat.Health,health) && farm.Combat.Kills==kills,"Gold/health/kills lost at "+farm.gameObject.scene.path+": "+farm.Gold+" / "+farm.Combat.Health+" / "+farm.Combat.Kills);
        Check(farm.InventoryCount==5 && Enumerable.Range(0,farm.InventoryCount).Any(i=>farm.InventoryItem(i)==Resources.Load<PlantDefinition>("Equipment/Sword")),"Equipment lost on stairs");
        Check(new MapCollision(farm).CanStand(farm.PlayerPosition,farm.PlayerRadius),"Blocked floor arrival");
    }
    private static void SafeLobby(FarmGame farm)
    {
        Check(farm.gameObject.scene.path==TowerDungeonSetup.Lobby,"Expected tower first floor");
        Check(farm.Layout.combatSchedule==FarmMapLayout.CombatSchedule.Peaceful && !farm.Layout.dayNightLighting && farm.Combat.EnemyCount==0,"Lobby is hostile or dark");
        float old=farm.Cycle.Elapsed;
        foreach(float time in new[]{0f,farm.Cycle.CycleSeconds*.65f,farm.Cycle.CycleSeconds*.9f})
        { farm.Cycle.RestoreElapsed(time);farm.Combat.TickCombat(.1f);Check(farm.Combat.EnemyCount==0,"Night spawned lobby enemies"); }
        farm.Cycle.RestoreElapsed(old);
    }
    private static void KillOne(FarmGame farm,string model="Chaser")
    {
        var target=farm.Combat.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name==model && r.enabled);
        Vector2 point=target.transform.position;var collision=new MapCollision(farm);
        var stand=new[]{Vector2.down,Vector2.up,Vector2.left,Vector2.right}.Select(d=>point+d).First(p=>collision.CanStand(p,farm.PlayerRadius) && collision.Trace(p,point,0,true)>=1);
        farm.TeleportPlayer(stand);
        var weapon=UnityEngine.Object.Instantiate(Resources.Load<PlantDefinition>("Equipment/Sword"));weapon.damage=1000;weapon.attackRange=3;
        Check(farm.Combat.Attack(weapon,point) && !target.enabled,"Existing melee did not defeat second-floor enemy");UnityEngine.Object.Destroy(weapon);
    }
    private static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Tower check timed out at "+step);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.4f || GameSession.Busy || Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(farm==null || !farm.IsInitialized)return;
            farm.enabled=false;Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(resume)
            {
                if(step==0)
                {
                    SafeLobby(farm);InventoryAndHealth(farm,60,expectedKills);Check(farm.WorldPieceCount==1,"Lobby drop lost after restart");step++;Travel(farm,TowerDungeonSetup.Battle,true);return;
                }
                Check(farm.gameObject.scene.path==TowerDungeonSetup.Battle,"Restart stair did not reach 2F");InventoryAndHealth(farm,60,expectedKills);
                Check(farm.WorldPieceCount==expectedBattleDrops,"Combat loot lost after restart");
                Check(farm.Combat.FloorCleared && farm.Combat.LivingHostileCount==0,"Cleared floor repopulated after restart");
                farm.TeleportPlayer(new Vector2(8,-5));farm.Combat.TickCombat(90);
                Check(farm.Combat.LivingHostileCount==0,"Cleared floor respawned over time");
                step++;Travel(farm,TowerDungeonSetup.Lobby,true);
                if(step==2) { SessionState.SetBool(Key+"ResumeFinish",true);resume=false;step=7; }
                return;
            }
            if(step==0)
            {
                Check(farm.gameObject.scene.path==TriangleWorldSetup.Field,"Start outside tower");
                var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");var player=farm.CapturePlayer(catalog);player.gold=123;player.health=70;farm.RestorePlayer(player,catalog);
                Check(farm.AddItem(Resources.Load<PlantDefinition>("Equipment/Sword")),"Cannot prepare existing sword");step++;Travel(farm,TowerDungeonSetup.Lobby,false);return;
            }
            if(step==1)
            {
                SafeLobby(farm);InventoryAndHealth(farm,70,0);farm.DropItem(Resources.Load<PlantDefinition>("Equipment/Sword"),Vector2.zero);
                Capture(farm,"Tower-1F.png");step++;Travel(farm,TowerDungeonSetup.Battle,true);return;
            }
            if(step==2)
            {
                Check(farm.gameObject.scene.path==TowerDungeonSetup.Battle && farm.Combat.EnemyCount==2,"2F must contain existing two enemies");InventoryAndHealth(farm,70,0);
                Check(farm.Layout.combatSchedule==FarmMapLayout.CombatSchedule.Always,"2F enemies depend on time");
                CheckStairBlocked(farm);
                farm.Combat.SpawnEnemy(new Vector2(9,5),false,null,true); // Living training dummies must not lock stairs.
                Capture(farm,"Tower-2F.png");farm.TeleportPlayer(new Vector2(8,-5));farm.Combat.TickCombat(1.1f);farm.Combat.DamagePlayer(10);Check(farm.Combat.Health==60,"Damage fixture remained invulnerable");KillOne(farm);Check(farm.Combat.LivingHostileCount==1,"Enemy kill not counted");
                CheckStairBlocked(farm);farm.Combat.TickCombat(.5f);KillOne(farm,"Spitter");
                Check(farm.Combat.LivingHostileCount==0 && farm.Combat.EnemyCount==1 && farm.Combat.FloorCleared,"Training dummy affected floor clear");
                var snapshot=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath)).maps.Single(m=>m.scene==TowerDungeonSetup.Battle);
                Check(snapshot.combatCleared && snapshot.defeatedEnemies.Count==2 && snapshot.pieces.Count==2,"Final kill did not autosave clear and loot");
                farm.TeleportPlayer(new Vector2(8,-5));farm.Combat.TickCombat(90);
                Check(farm.Combat.FloorCleared && farm.Combat.LivingHostileCount==0,"Ordinary enemies respawned after clear");
                step++;Travel(farm,TowerDungeonSetup.Lobby,true);return;
            }
            if(step==3) { SafeLobby(farm);InventoryAndHealth(farm,60,expectedKills);Check(farm.WorldPieceCount==1,"Lobby item lost");step++;Travel(farm,TriangleWorldSetup.Field,false);return; }
            if(step==4) { Check(farm.gameObject.scene.path==TriangleWorldSetup.Field,"Wrong exterior return");InventoryAndHealth(farm,60,expectedKills);step++;Travel(farm,TowerDungeonSetup.Lobby,false);return; }
            if(step==5) { SafeLobby(farm);step=6;Travel(farm,TowerDungeonSetup.Battle,true);return; }
            if(step==6)
            {
                Check(farm.gameObject.scene.path==TowerDungeonSetup.Battle && farm.Combat.FloorCleared && farm.Combat.LivingHostileCount==0,"Leaving tower reset cleared floor");
                InventoryAndHealth(farm,60,expectedKills);Check(farm.WorldPieceCount==expectedBattleDrops,"Restoring clear duplicated or lost loot");
                step=7;Travel(farm,TowerDungeonSetup.Lobby,true);return;
            }
            SafeLobby(farm);InventoryAndHealth(farm,60,expectedKills);Check(farm.WorldPieceCount==1,"Lobby state lost after outside loop");Check(GameSession.Save(farm),"Tower autosave failed");
            var saved=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
            Check(saved.version==1 && saved.currentScene==TowerDungeonSetup.Lobby && saved.maps.Any(m=>m.scene==TowerDungeonSetup.Battle && m.pieces.Count==expectedBattleDrops),"Tower map save incomplete");
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            Debug.Log(SessionState.GetBool(Key+"ResumeFinish",false)?"TOWER_RESTART_SUCCESS: 1F continue, F upstairs/downstairs, persistent combat loot, gold/items/HP.":"TOWER_PLAY_SUCCESS: safe 1F day/night, 2F existing combat, F-only stairs both ways, locked with living enemies, unlocked only after clearing, automatic exterior, autosaves and per-floor drops.");
            EditorApplication.Exit(0);
        }
        catch(Exception problem) { SessionState.SetBool(Key,false);Debug.LogException(problem);EditorApplication.Exit(1); }
    }
    private static void Capture(FarmGame farm,string file)
    {
        var camera=Camera.main;var position=camera.transform.position;float size=camera.orthographicSize;
        var rect=farm.WorldBounds;camera.transform.position=new Vector3(rect.center.x,rect.center.y,-10);camera.orthographicSize=rect.height*.55f;
        var target=new RenderTexture(1280,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(1280,900,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();File.WriteAllBytes(file,texture.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;camera.transform.position=position;camera.orthographicSize=size;
        UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(target);
    }
}
