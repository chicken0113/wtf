using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

[InitializeOnLoad]
public static class BasicControlsCheck
{
    const string Key="BasicControlsCheck";static int step;static double deadline;
    static BasicControlsCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        BasicControlsSetup.Prepare();EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Controls check timeout "+step);
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<.4f||GameSession.Busy||Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f==null||f.Combat==null)return;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            if(step==0)
            {
                Check(FarmGame.MovementInput(true,true,true,true)==Vector2.zero,"Opposite movement keys did not cancel");
                Check(Mathf.Abs(FarmGame.MovementInput(false,true,true,false).magnitude-1)<.001f,"Diagonal movement faster than cardinal");
                f.TeleportPlayer(new Vector2(0,-5));f.UpdateAim(new Vector2(5,-5),false);f.MovePlayer(new Vector2(0,-4));
                Check(f.AimDirection==Vector2.right,"Movement overwrote aim");f.UpdateAim(new Vector2(0,5),true);Check(f.AimDirection==Vector2.right,"UI mouse changed aim");
                f.UpdateAim(f.PlayerPosition,false);Check(f.AimDirection==Vector2.right,"Center cursor erased aim");
                Check(f.DodgeDirection(Vector2.up)==Vector2.up&&f.DodgeDirection(Vector2.zero)==Vector2.right,"Dodge direction priority wrong");
                f.ProcessMenuInput(true,false,false,false);Check(f.InventoryOpen&&!f.InteractWorld(Vector2.zero)&&!f.HarvestWorld(Vector2.zero),"World action behind menu");
                f.ProcessMenuInput(false,true,false,false);Check(!f.ProcessPrimaryPointer(true,false),"Menu close leaked held attack");
                f.ProcessPrimaryPointer(false,false);Check(f.ProcessPrimaryPointer(true,false),"Released click stayed blocked");
                Check(!f.ProcessPrimaryPointer(true,true)&&!f.ProcessPrimaryPointer(true,false),"Dragging UI click into world used item");f.ProcessPrimaryPointer(false,false);
                var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");f.AddItem(seed);int count=f.InventoryQuantity(2);
                f.TeleportPlayer(new Vector2(.5f,.5f));
                Check(!f.HarvestWorld(new Vector2(0,-2))&&f.InventoryQuantity(2)==count,"Harvest key planted on empty soil");
                Check(f.InteractWorld(new Vector2(0,-2))&&!f.Shop.IsOpen&&f.InventoryQuantity(2)==count-1,"E planting opened nearby NPC");
                f.Water(new Vector2(0,-2),.1f);f.Tick(seed.growthSeconds);
                Check(f.InteractWorld(new Vector2(0,-2))&&f.IsMature(new Vector2Int(0,-2)),"E harvested a crop");
                Check(f.HarvestWorld(new Vector2(0,-2))&&!f.IsMature(new Vector2Int(0,-2))&&f.InventoryCount==5,"F crop harvest failed");f.RemoveInventoryItem(4);
                var npc=f.GetComponentInChildren<ShopNpc>();
                Check(!f.InteractWorld(npc.transform.position,true)&&!f.Shop.IsOpen,"HUD E opened NPC");
                Check(f.InteractWorld(npc.transform.position)&&f.Shop.IsOpen,"Pointed NPC did not interact");f.ProcessMenuInput(false,false,false,true);
                Check(!f.HarvestWorld(npc.transform.position)&&!f.Shop.IsOpen,"F opened NPC");
                var house=f.GetComponentInChildren<FarmBuilding>();var door=house.GetComponentInChildren<MapExit>();f.TeleportPlayer(door.transform.position);
                Check(!f.InteractWorld(house.transform.position,true)&&house.gameObject.activeInHierarchy,"UI harvested house");
                step=1;Check(f.InteractWorld(house.transform.position)&&GameSession.Busy,"E did not enter house from doormat");return;
            }
            if(step==1)
            {
                Check(f.gameObject.scene.path==InteriorWorldSetup.Home,"E did not enter room");
                var door=f.GetComponentInChildren<MapExit>();f.TeleportPlayer(door.transform.position);
                Check(!f.HarvestWorld(door.transform.position)&&!GameSession.Busy,"F triggered door travel");
                step=2;Check(f.InteractWorld(new Vector2(50,50))&&GameSession.Busy,"E did not exit room");return;
            }
            if(step==2)
            {
                var house=f.GetComponentInChildren<FarmBuilding>();var body=house.GetComponentsInChildren<SpriteRenderer>().First(r=>r.GetComponentInParent<MapExit>()==null);
                var point=new Vector2(body.bounds.center.x,body.bounds.min.y+.1f);
                Check(f.HarvestWorld(point)&&!GameSession.Busy&&f.GetComponentsInChildren<FarmBuilding>().Length==0,"F house harvest triggered travel");
                step=3;Check(GameSession.Travel(f,CombatTestMapSetup.CombatScene,"combat_path_entry"),"Combat fixture travel failed");return;
            }
            f.TeleportPlayer(new Vector2(7,-5));var combat=f.Combat;combat.TickCombat(1.1f);float hp=combat.Health;
            Check(combat.RequestRoll(Vector2.right)&&combat.IsRolling,"Dodge did not start");combat.DamagePlayer(combat.HalfHeartHealth);Check(combat.Health==hp,"Roll invulnerability failed");
            combat.TickCombat(.23f);Check(!combat.IsRolling&&Mathf.Abs(f.PlayerPosition.x-9.42f)<.05f,"Dodge distance/duration mismatch");
            combat.DamagePlayer(combat.HalfHeartHealth);Check(combat.Health==hp-combat.HalfHeartHealth,"Damage remained disabled after roll");
            combat.TickCombat(.31f);Check(combat.RequestRoll(Vector2.left)&&!combat.IsRolling,"Dodge cooldown ignored");
            combat.TickCombat(.06f);Check(!combat.IsRolling,"Buffered dodge started too early");combat.TickCombat(.06f);Check(combat.IsRolling,"Buffered dodge was dropped near cooldown end");combat.TickCombat(.2f);
            combat.RequestRoll(Vector2.up);combat.TickCombat(.4f);Check(!combat.IsRolling,"Expired buffered input triggered a later dodge");
            combat.TickCombat(.5f);f.TeleportPlayer(new Vector2(10.8f,-5));Check(combat.RequestRoll(Vector2.right),"Wall test dodge failed");combat.TickCombat(.3f);
            Check(f.PlayerPosition.x<=11.26f&&new MapCollision(f).CanStand(f.PlayerPosition,f.PlayerRadius),"Dodge crossed wall");
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log("BASIC_CONTROLS_SUCCESS: movement/aim separation, diagonal/opposite keys, UI click isolation, E interaction/planting vs F harvest, NPC/house doors, roll invulnerability/buffer/expiry/walls.");EditorApplication.Exit(0);
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
