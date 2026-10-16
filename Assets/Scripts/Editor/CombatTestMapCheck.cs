using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class CombatTestMapCheck
{
    const string Key="WtfCombatMapCheck";
    static int step; static double deadline;
    static CombatTestMapCheck()
    {
        if(Application.isBatchMode && SessionState.GetBool(Key,false))
        { deadline=EditorApplication.timeSinceStartup+80; EditorApplication.update+=Tick; }
    }
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"))) throw new Exception("Use isolated project and save directory.");
        EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);
        CombatTestMapSetup.ConnectFarm(); SessionState.SetBool(Key,true); EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new Exception("Combat test timed out at step "+step+": "+GameSession.LastError);
            if(!EditorApplication.isPlaying || Time.timeSinceLevelLoad<.3f || GameSession.Busy) return;
            var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); if(farm==null || farm.Combat==null) return;
            Require(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(step==0)
            {
                Require(farm.gameObject.scene.path==CombatTestMapSetup.FarmScene,"Wrong initial scene");
                Require(farm.InventoryCount==4 && farm.InventoryQuantity(2)==10,"Starting inventory changed");
                farm.Cycle.Tick(farm.Cycle.Settings.daylightSeconds+1);
                Require(!farm.Cycle.MonstersActive && farm.Combat.EnemyCount==0 && farm.IsSafe(new Vector2(20,12)),"Farm is not peaceful at dusk");
                var exit=UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None).Single(e=>e.destinationScene==CombatTestMapSetup.CombatScene);
                farm.TeleportPlayer(exit.transform.position); Require(exit.Contains(farm.PlayerPosition),"Farm entrance unreachable");
                Require(new MapCollision(farm).CanStand(farm.PlayerPosition,farm.PlayerRadius),"Farm entrance overlaps obstacle");
                step=1; Require(exit.Travel(farm),"Farm to combat travel failed"); return;
            }
            if(step==1)
            {
                Require(farm.gameObject.scene.path==CombatTestMapSetup.CombatScene,"Did not enter combat scene");
                var arrival=MapArrivalPoint.Find(farm.gameObject.scene,"combat_entry");
                Require(Vector2.Distance(farm.PlayerPosition,arrival.transform.position)<.1f && farm.IsSafe(farm.PlayerPosition),"Unsafe combat arrival");
                Require(farm.InventoryCount==4 && farm.InventoryQuantity(2)==10,"Map travel changed inventory");
                Require(farm.Combat.EnemyCount==2 && farm.Layout.GetComponentsInChildren<EnemyPlacement>().Count(e=>e.ranged)==1,"Enemy population incorrect");
                farm.Cycle.RestoreElapsed(0); Require(farm.Combat.EnemyCount==2 && farm.Cycle.Darkness==0,"Daylight disabled combat enemies");
                farm.Cycle.Tick(farm.Cycle.CycleSeconds-.1f); Require(farm.Combat.EnemyCount==2 && farm.Cycle.Darkness==0,"Clock disabled enemies or darkened test map");
                Require(farm.Layout.GetComponentsInChildren<MapObstacle>().Length==8,"Missing walls and cover");
                farm.TeleportPlayer(new Vector2(0,6)); farm.MovePlayer(new Vector2(0,12)); Require(farm.PlayerPosition.y<7.251f,"Boundary wall is not solid");
                var chaser=farm.Combat.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Chaser");
                var spitter=farm.Combat.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="Spitter");
                Require(chaser.enabled && spitter.enabled,"Enemies are invisible");
                farm.TeleportPlayer(new Vector2(-5.5f,0)); Vector2 before=chaser.transform.position;
                for(int i=0;i<15;i++) farm.Combat.TickCombat(.05f);
                Require(chaser.transform.position.x<before.x-.5f,"Melee enemy did not pursue");
                farm.TeleportPlayer(new Vector2(2,1));
                bool warning=false,shot=false;
                for(int i=0;i<50;i++)
                {
                    farm.Combat.TickCombat(.05f);
                    warning|=farm.Combat.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Attack warning" && r.enabled);
                    shot|=farm.Combat.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Enemy shot");
                }
                Require(warning && shot,"Ranged enemy did not warn and fire");
                Snapshot(farm,"CombatTestOverview.png",Vector2.zero,9.5f);
                var exit=UnityEngine.Object.FindObjectsByType<MapExit>(FindObjectsSortMode.None).Single(e=>e.destinationScene==CombatTestMapSetup.FarmScene);
                farm.TeleportPlayer(exit.transform.position); step=2; Require(exit.Travel(farm),"Return travel failed"); return;
            }
            if(step==2)
            {
                Require(farm.gameObject.scene.path==CombatTestMapSetup.FarmScene,"Did not return to farm");
                var arrival=MapArrivalPoint.Find(farm.gameObject.scene,"farm_from_combat");
                Require(Vector2.Distance(farm.PlayerPosition,arrival.transform.position)<.1f && farm.Combat.EnemyCount==0,"Wrong farm arrival or hostile farm");
                Require(farm.InventoryCount==4 && farm.InventoryQuantity(2)==10,"Return trip duplicated or removed inventory");
                Require(File.Exists(GameSession.SavePath),"Autosave missing");
                Snapshot(farm,"CombatFarmEntrance.png",new Vector2(3,0),5);
                SessionState.SetBool(Key,false); EditorApplication.update-=Tick;
                Debug.Log("COMBAT_MAP_PLAY_SUCCESS: peaceful farm, both portals and safe arrivals, inventory preservation, autosave, permanent enemies, collision, pursuit, warning and ranged shots."); EditorApplication.Exit(0);
            }
        }
        catch(Exception error) { SessionState.SetBool(Key,false); EditorApplication.update-=Tick; Debug.LogException(error); EditorApplication.Exit(1); }
    }
    static void Snapshot(FarmGame farm,string name,Vector2 position,float size)
    {
        var camera=Camera.main; camera.transform.position=new Vector3(position.x,position.y,-10); camera.orthographicSize=size;
        var target=new RenderTexture(1280,800,24); camera.targetTexture=target; farm.Cycle.RefreshLighting(); camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,800,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply(); File.WriteAllBytes(name,image.EncodeToPNG());
        camera.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}
