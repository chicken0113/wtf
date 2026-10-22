using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class ActionFeelCheck
{
    const string Key="ActionFeelCheck";static double deadline;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static ActionFeelCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick;}}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Public|Private).GetValue(value);
    static void Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Private).Invoke(value,args);
    public static void Run()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Action check timed out");
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<.4f||GameSession.Busy||Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f==null||f.Combat==null)return;
            f.enabled=false;var c=f.Combat;var collision=new MapCollision(f);
            var hoe=Resources.Load<PlantDefinition>("Equipment/Hoe");
            if(hoe==null)hoe=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
            Check(f.AddItem(hoe),"Select hoe");f.TeleportPlayer(new Vector2(0,-4));
            f.ProcessPrimaryPointer(false,false);
            Check(f.UsePrimaryTool(new Vector2Int(0,-4),true),"Initial hoe press failed");
            Check(!f.UsePrimaryTool(new Vector2Int(0,-4),false),"Held tool repeated same cell");
            Check(!f.UsePrimaryTool(new Vector2Int(1,-4),false),"Held tool ignored drag interval");
            Call(f,"TickFeedback",.12f);Check(f.UsePrimaryTool(new Vector2Int(1,-4),false),"Held hoe did not use next cell");
            c.TickCombat(.3f);Check(!c.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Impact spark"&&r.gameObject.activeSelf),"Tool effects leaked");
            Vector2 point=Vector2.zero;bool found=false;
            for(int y=-8;y<=8&&!found;y++)for(int x=-8;x<=8&&!found;x++)
            {
                var p=new Vector2(x,y);if(collision.CanStand(p,.3f)&&collision.Trace(p,p+Vector2.right*3,.4f,true)>=1&&collision.CanStand(p+Vector2.right*3,.4f)){point=p;found=true;}
            }
            Check(found,"No clear fixture corridor");f.TeleportPlayer(point);
            c.SpawnEnemy(point+Vector2.right*1.4f,false,null,true);
            var enemies=(IList)Field(c,"enemies");var target=enemies[enemies.Count-1];
            var model=(SpriteRenderer)Field(target,"model");Color original=model.color;
            var sword=Resources.Load<PlantDefinition>("Equipment/Sword");var bow=Resources.Load<PlantDefinition>("Equipment/Bow");
            f.RemoveInventoryItem(f.InventoryCount-1);
            Check(f.AddItem(sword),"Equip sword");f.UpdateAim(point+Vector2.right*2,false);
            int kills=c.Kills,count=f.InventoryCount;
            Check(c.Attack(sword,point+Vector2.right*2),"Melee did not start");
            Check((float)Field(target,"health")==55-sword.damage,"Melee hit was delayed");
            Check(!c.Attack(sword,point+Vector2.right*2),"Melee ignored cooldown");
            Check(model.color!=original&&c.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.name=="Sword swing")==12,"Missing melee/hit feedback");
            Call(f,"AnimateFarmer",Vector2.zero);Capture("ActionFeel-Melee.png");
            c.TickCombat(.2f);Check(model.color==original,"Hit tint did not reset");
            c.TickCombat(1);Check(c.Kills==kills&&f.InventoryCount==count,"Training hit gave rewards");
            // A wall closer than the muzzle offset must still stop the actual attack.
            var wall=new GameObject("Muzzle wall").AddComponent<MapObstacle>();wall.transform.position=point+Vector2.right*.38f;wall.size=new Vector2(.1f,2);
            f.RemoveInventoryItem(f.InventoryCount-1);
            Check(f.AddItem(bow),"Equip bow");
            float before=(float)Field(target,"health");
            Check(c.Attack(bow,point+Vector2.right*2),"Ranged attack did not start");
            var shots=(IList)Field(c,"shots");var shot=shots[shots.Count-1];
            Check(Mathf.Abs(((Vector2)Field(shot,"velocity")).magnitude-f.Controls.projectileSpeed)<.001f,"Wrong player projectile speed");
            c.TickCombat(.15f);Check((float)Field(target,"health")==before,"Muzzle shot bypassed adjacent wall");
            UnityEngine.Object.DestroyImmediate(wall.gameObject);c.TickCombat(1);
            Check(c.Attack(bow,point+Vector2.right*2),"Second ranged attack failed");
            Call(f,"AnimateFarmer",Vector2.zero);
            var weaponModel=(SpriteRenderer)Field(f,"heldModel");
            Check(weaponModel.enabled&&weaponModel.transform.localPosition.x<.32f,"Held weapon recoil missing");Capture("ActionFeel-Ranged.png");
            c.TickCombat(.15f);Check((float)Field(target,"health")<before,"Fast swept shot missed target");
            Check(c.RequestRoll(Vector2.right),"Roll did not start");c.TickCombat(.08f);
            Check(c.RollProgress>0&&c.RollProgress<1,"Roll visual progress invalid");
            Call(f,"AnimateFarmer",Vector2.zero);
            var farmer=(SpriteRenderer)Field(f,"farmer");var held=(SpriteRenderer)Field(f,"heldModel");
            Check(!held.enabled&&Mathf.Abs(farmer.transform.eulerAngles.z)>1,"Roll pose did not hide held item or rotate");
            Check(c.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="Dodge trail"),"Roll has no trail");Capture("ActionFeel-Roll.png");
            Check(!f.UsePrimaryTool(new Vector2Int(2,-4),true),"Tool used during dodge");
            c.TickCombat(.3f);Call(f,"AnimateFarmer",Vector2.zero);
            Check(!c.IsRolling&&farmer.transform.rotation==Quaternion.identity,"Roll pose stuck");
            Check(!c.GetComponentsInChildren<SpriteRenderer>().Any(r=>(r.name=="Dodge trail"||r.name=="Sword swing"||r.name=="Muzzle flash")&&r.gameObject.activeSelf),"Transient effects leaked");
            float strength=f.Controls.cameraShake;f.Controls.cameraShake=0;Call(f,"TickFeedback",1f);f.ImpactFeedback(true);
            Vector2 offset=(Vector2)typeof(FarmGame).GetMethod("CameraFeedback",Private).Invoke(f,null);Check(offset==Vector2.zero,"Disabled shake moved camera");f.Controls.cameraShake=strength;
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            Debug.Log("ACTION_FEEL_SUCCESS: drag tools, immediate melee/cooldown, hit tint reset, training rewards excluded, swept player shot/muzzle wall, roll pose/trail cleanup, shake off.");EditorApplication.Exit(0);
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Capture(string file)
    {
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();Call(farm,"LateUpdate");
        var camera=Camera.main;var target=new RenderTexture(1280,720,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes(file,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
    }
}
