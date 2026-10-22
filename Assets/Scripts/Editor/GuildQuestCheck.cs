using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class GuildQuestCheck
{
    const string Key="GuildQuestPlayCheck";
    static int step;static bool resume,finishing;static double deadline,finishAt;static float walked;
    static string[] StartRoute=>new[]{CombatTestMapSetup.FarmScene,TriangleWorldSetup.Passage,WorldFrameworkSetup.Town,InteriorWorldSetup.Smith,
        WorldFrameworkSetup.Town,InteriorWorldSetup.Guild,WorldFrameworkSetup.Town,TriangleWorldSetup.Field,TriangleWorldSetup.Training,TriangleWorldSetup.Field,
        CombatTestMapSetup.CombatScene,TriangleWorldSetup.Field}
        .Take(TowerDungeonSetup.Installed?11:12)
        .SelectMany(p=>TowerDungeonSetup.Installed&&p==CombatTestMapSetup.CombatScene?new[]{TowerDungeonSetup.Lobby,p}:new[]{p}).ToArray();
    static string[] ResumeRoute=>TowerDungeonSetup.Installed
        ?new[]{CombatTestMapSetup.CombatScene,TowerDungeonSetup.Lobby,TriangleWorldSetup.Field,WorldFrameworkSetup.Town,InteriorWorldSetup.Guild}
        :new[]{TriangleWorldSetup.Field,CombatTestMapSetup.CombatScene,TriangleWorldSetup.Field,WorldFrameworkSetup.Town,InteriorWorldSetup.Guild};
    static GuildQuestCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){resume=SessionState.GetBool(Key+"Resume",false);deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Tick;}}
    static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run(){Guard();GuildQuestSetup.Apply();WorldRouteAudit.Validate();Start(false);}
    public static void Resume(){Guard();Start(true);}
    static void Guard(){if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated validation and saves.");}
    static void Start(bool value){EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Resume",value);EditorApplication.isPlaying=true;}
    static void Travel(FarmGame f,string target)
    {
        var exit=f.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==target);var path=new MapCollision(f).FindPath(f.PlayerPosition,exit.transform.position,f.PlayerRadius);
        foreach(var point in path){var before=f.PlayerPosition;f.MovePlayer(point);Require(Vector2.Distance(f.PlayerPosition,point)<.05f,"Collision prevented walking route "+target);walked+=Vector2.Distance(before,point);}
        Require(Vector2.Distance(f.PlayerPosition,exit.transform.position)<.3f,"Travel approach did not reach exit");
        if(exit.requireInteract)Require(!exit.TryActivate(f,false),"Interior door entered without E");
        Require(exit.TryActivate(f,exit.requireInteract),"Travel rejected "+GameSession.LastError);
    }
    static void OpenDesk(FarmGame f,bool board=false)
    {
        var npc=f.GetComponentsInChildren<ShopNpc>().Single(n=>board?n.name=="Quest notice board - E":n.name=="Guild reception");
        f.TeleportPlayer((Vector2)npc.transform.position+Vector2.down*2.1f);Require(f.Shop.Open(npc)&&f.Shop.IsQuestDesk,"Guild desk/board did not open");
    }
    static void Kill(FarmGame f,string model)
    {
        var enemy=f.Combat.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name==model&&r.enabled);
        var point=(Vector2)enemy.transform.position;var collision=new MapCollision(f);
        var stand=new[]{Vector2.down,Vector2.up,Vector2.left,Vector2.right}.Select(d=>point+d).First(p=>collision.CanStand(p,f.PlayerRadius)&&collision.Trace(p,point,0,true)>=1);
        f.TeleportPlayer(stand);f.Combat.TickCombat(.6f);
        var weapon=ScriptableObject.CreateInstance<PlantDefinition>();weapon.effect=GrowthEffect.Weapon;weapon.damage=1000;weapon.attackRange=3;weapon.attackInterval=.1f;
        Require(f.Combat.Attack(weapon,enemy.transform.position)&&!enemy.enabled,"Actual melee hit did not kill "+model);UnityEngine.Object.Destroy(weapon);
    }
    static void CameraCheck(FarmGame f)
    {
        var c=Camera.main;float h=c.orthographicSize,w=h*c.aspect;var p=c.transform.position;var b=f.WorldBounds;
        Require(c.orthographic,"Perspective camera in top-down map");
        if(b.width>=w*2)Require(p.x-w>=b.xMin-.1f&&p.x+w<=b.xMax+.1f,"Camera crossed horizontal map bounds");
        if(b.height>=h*2)Require(p.y-h>=b.yMin-.1f&&p.y+h<=b.yMax+.1f,"Camera crossed vertical map bounds");
        Require(Mathf.Abs(f.PlayerPosition.x-p.x)<w+.2f&&Mathf.Abs(f.PlayerPosition.y-p.y)<h+.2f,"Player outside camera");
    }
    static void Tick()
    {
        try
        {
            if(finishing){if(EditorApplication.timeSinceStartup>=finishAt)Finish("GUILD_QUEST_FLOW_SUCCESS: restart progress, real dungeon kills, return/report, atomic reward rollback/retry, duplicate protection, repeat/one-time rules, saved gold.");return;}
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Guild request test timeout "+step);
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<.45f||GameSession.Busy||Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f==null||f.Combat==null)return;
            Require(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);CameraCheck(f);
            var route=resume?ResumeRoute:StartRoute;Require(f.gameObject.scene.path==route[step],"Wrong map "+step);
            if(!resume)
            {
                if(step==0){f.Shop.RestoreGold(500);Require(!GameSession.AcceptGuildQuest(f,"clear_dungeon"),"Accepted outside guild");}
                if(step==3)
                {
                    f.Cycle.RestoreElapsed(0);var npc=f.GetComponentInChildren<ShopNpc>();f.TeleportPlayer(new Vector2(0,1.2f));Require(f.Shop.Open(npc),"Smith unavailable");
                    var i=f.Shop.npcs.Single().offers.FindIndex(o=>o.item==Resources.Load<PlantDefinition>("Equipment/Sword"));Require(i>=0&&f.Shop.Buy(i),"Could not prepare weapon at smith");f.Shop.Close();
                }
                if(step==5)
                {
                    OpenDesk(f);Require(!GameSession.ClaimGuildQuest(f,"clear_dungeon"),"Claimed unaccepted request");
                    Require(GameSession.AcceptGuildQuest(f,"clear_dungeon")&&!GameSession.AcceptGuildQuest(f,"clear_dungeon"),"Accept duplicate guard failed");
                    Require(GameSession.AcceptGuildQuest(f,"ranged_watch")&&!GameSession.ClaimGuildQuest(f,"clear_dungeon"),"Incomplete request granted gold");
                    Require(f.WorldInputBlocked,"Guild menu did not pause input");Require(f.ProcessMenuInput(false,false,false,true)&&!f.Shop.IsOpen,"E did not close board");
                }
                if(step==8){Kill(f,"Training dummy");Require(GameSession.GuildQuestState("clear_dungeon").progress==0,"Training target counted toward request");}
                if(f.gameObject.scene.path==CombatTestMapSetup.CombatScene){Kill(f,"Chaser");Require(GameSession.GuildQuestState("clear_dungeon").progress==1&&GameSession.GuildQuestState("ranged_watch").progress==0,"Enemy filtering/progress failed");}
                if(step==StartRoute.Length-1)
                {
                    Require(GameSession.GuildQuestState("clear_dungeon").progress==1&&f.InventoryCount==5,"Progress/inventory lost on dungeon exit");
                    Require(GameSession.Save(f),"Failed to checkpoint partial progress inside locked floor");
                    Finish("GUILD_PARTIAL_SAVE_SUCCESS: accepted requests, actual combat, training exclusion, "+walked.ToString("F1")+" collision-checked walking units.");return;
                }
            }
            else
            {
                if(step==0)Require(GameSession.GuildQuestState("clear_dungeon").progress==1&&GameSession.GuildQuestState("ranged_watch").progress==0,"Restart lost partial progress");
                if(f.gameObject.scene.path==CombatTestMapSetup.CombatScene){Kill(f,"Spitter");Require(GameSession.GuildQuestState("clear_dungeon").progress==2&&GameSession.GuildQuestState("ranged_watch").progress==1,"Ranged kill did not complete requests");Require(!GameSession.ClaimGuildQuest(f,"clear_dungeon"),"Claimed reward outside guild");
                    if(TowerDungeonSetup.Installed && f.Combat.LivingHostileCount>0){f.Combat.TickCombat(.5f);Kill(f,"Chaser");}}
                if(step==ResumeRoute.Length-1)
                {
                    OpenDesk(f,true);int before=f.Gold;
                    using(var locked=new FileStream(GameSession.SavePath,FileMode.Open,FileAccess.Read,FileShare.None))
                    {Require(!GameSession.ClaimGuildQuest(f,"clear_dungeon"),"Locked save unexpectedly accepted reward");}
                    Require(f.Gold==before&&GameSession.GuildQuestState("clear_dungeon").accepted,"Failed reward save lost money/progress");
                    Require(GameSession.ClaimGuildQuest(f,"clear_dungeon")&&f.Gold==before+50&&!GameSession.ClaimGuildQuest(f,"clear_dungeon"),"Reward duplicated or wrong");
                    Require(GameSession.ClaimGuildQuest(f,"ranged_watch")&&f.Gold==before+90&&!GameSession.AcceptGuildQuest(f,"ranged_watch"),"One-time request repeated");
                    Require(GameSession.AcceptGuildQuest(f,"clear_dungeon")&&GameSession.GuildQuestState("clear_dungeon").progress==0,"Repeat request did not reset");
                    var saved=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(GameSession.SavePath));
                    Require(saved.player.gold==before+90&&saved.guildQuests.Single(q=>q.id=="clear_dungeon").completedRuns==1&&saved.guildQuests.Single(q=>q.id=="ranged_watch").completedRuns==1,"Reward state not durably saved");
                    ScreenCapture.CaptureScreenshot("GuildQuestBoard.png");finishing=true;finishAt=EditorApplication.timeSinceStartup+.5;return;
                }
            }
            step++;Travel(f,route[step]);
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Finish(string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(0);}
}
