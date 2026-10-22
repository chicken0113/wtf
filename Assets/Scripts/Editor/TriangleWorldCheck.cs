using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class TriangleWorldCheck
{
    const string Key="TriangleWorldPlayCheck";
    static int step;static double deadline;
    static readonly string[] Route={CombatTestMapSetup.FarmScene,TriangleWorldSetup.Passage,WorldFrameworkSetup.Town,WorldFrameworkSetup.Store,WorldFrameworkSetup.Town,
        TriangleWorldSetup.Field,CombatTestMapSetup.FarmScene,TriangleWorldSetup.Field,TriangleWorldSetup.Forest,TriangleWorldSetup.Field,
        TriangleWorldSetup.Training,TriangleWorldSetup.Field,CombatTestMapSetup.CombatScene,TriangleWorldSetup.Field,WorldFrameworkSetup.Town,TriangleWorldSetup.Passage,CombatTestMapSetup.FarmScene};
    static TriangleWorldCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        TriangleWorldSetup.Apply();WorldConnectionValidator.Validate();
        foreach(var path in TriangleWorldSetup.Paths)
        {
            var scene=EditorSceneManager.OpenScene(path);var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var collision=new MapCollision(f);
            var points=f.GetComponentsInChildren<MapExit>().Select(e=>(Vector2)e.transform.position)
                .Concat(f.GetComponentsInChildren<FarmItemPlacement>().Select(p=>(Vector2)p.transform.position))
                .Concat(f.GetComponentsInChildren<MapArrivalPoint>().Select(a=>(Vector2)a.transform.position));
            foreach(var p in points)
            {
                var route=collision.FindPath(f.SpawnPosition,p,f.PlayerRadius);
                Check(collision.CanStand(p,f.PlayerRadius)&&(Vector2.Distance(f.SpawnPosition,p)<.4f||(route.Count>0&&Vector2.Distance(route.Last(),p)<.4f)),"Blocked route in "+path+" at "+p);
            }
            if(path==TriangleWorldSetup.Forest)Check(f.GetComponentsInChildren<FarmItemPlacement>().Length==4,"Forest item placement missing");
            if(path==TriangleWorldSetup.Training)Check(f.GetComponentsInChildren<EnemyPlacement>().Count(e=>e.trainingDummy)==3,"Training markers missing");
        }
        // Capture new maps using the real Play renderer in Tick, after FarmArt assets initialize.
        EditorSceneManager.OpenScene(Route[0]);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Capture(FarmGame f)
    {
        var camera=Camera.main;var p=camera.transform.position;var size=camera.orthographicSize;
        camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=f.WorldBounds.height*.55f;
        var target=new RenderTexture(1100,800,24);var previous=RenderTexture.active;var oldTarget=camera.targetTexture;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var picture=new Texture2D(1100,800,TextureFormat.RGBA32,false);picture.ReadPixels(new Rect(0,0,1100,800),0,0);picture.Apply();
            File.WriteAllBytes("Triangle-"+Path.GetFileNameWithoutExtension(f.gameObject.scene.path)+".png",picture.EncodeToPNG());UnityEngine.Object.Destroy(picture);
        }
        finally{camera.targetTexture=oldTarget;RenderTexture.active=previous;camera.transform.position=p;camera.orthographicSize=size;UnityEngine.Object.Destroy(target);}
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Triangle play test timed out at "+step);
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<.4f||GameSession.Busy||Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f==null||f.Combat==null)return;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            Check(f.gameObject.scene.path==Route[step],"Wrong scene at step "+step);
            Check(new MapCollision(f).CanStand(f.PlayerPosition,f.PlayerRadius),"Blocked arrival at "+step);
            if(step==0){Check(f.InventoryCount==4,"Starter inventory changed");f.Shop.RestoreGold(321);}
            else Check(f.Gold==321,"Gold lost on transition "+step);
            if(step==8)
            {
                Check(f.WorldPieceCount==4&&f.DetachAt(new Vector2(-3,1))&&f.WorldPieceCount==3,"Existing forest pickup failed");
                Check(f.InventoryCount==5,"Picked item missing");f.RemoveInventoryItem(4);
            }
            if(step==10)
            {
                var target=f.Combat.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Training dummy");
                Vector2 p=target.transform.position;f.TeleportPlayer(p+Vector2.down);
                float hp=f.Combat.Health;int pieces=f.WorldPieceCount,kills=f.Combat.Kills;
                var sword=Resources.Load<PlantDefinition>("Equipment/Sword");
                for(int i=0;i<10&&target.enabled;i++){f.Combat.TickCombat(.6f);Check(f.Combat.Attack(sword,p),"Melee attack failed");}
                Check(!target.enabled&&f.Combat.Kills==kills&&f.WorldPieceCount==pieces,"Target did not take normal damage, or granted loot/kills");
                f.Combat.TickCombat(3.1f);Check(target.enabled,"Practice target did not recover");
                Check(f.Combat.Attack(Resources.Load<PlantDefinition>("Equipment/Bow"),p),"Ranged attack failed");f.Combat.TickCombat(.2f);
                f.Combat.TickCombat(5);Check(f.Combat.Health==hp&&Vector2.Distance(target.transform.position,p)<.01f,"Dummy attacked or moved");
                Debug.Log("TRAINING_HITS_VERIFIED: normal melee/projectile hit paths, no player damage, loot or kills, target recovery.");
            }
            if(new[]{5,8,10}.Contains(step))Capture(f);
            if(step==Route.Length-1)
            {
                Check(f.InventoryCount==4,"Inventory lost between maps");
                Check(File.Exists(GameSession.SavePath),"Transition autosave absent");
                SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
                Debug.Log("TRIANGLE_WORLD_PLAY_SUCCESS: 8 maps, 16 directed transitions, pickup, training, inventory and gold continuity.");EditorApplication.Exit(0);return;
            }
            var exit=f.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==Route[step+1]);
            Check(exit.requireInteract==(Route[step]==WorldFrameworkSetup.Store||Route[step+1]==WorldFrameworkSetup.Store),"Unexpected E door");
            // Explicit leave/re-enter also covers return routes following arrival suppression.
            f.TeleportPlayer(f.SpawnPosition);exit.TryActivate(f,false);
            f.TeleportPlayer(exit.transform.position);Check(exit.TryActivate(f,exit.requireInteract),"Transition rejected at "+step);
            step++;
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
