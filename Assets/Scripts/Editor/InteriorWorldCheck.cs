using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class InteriorWorldCheck
{
    const string Key="InteriorWorldPlayCheck";
    static int step,gold=411;static double deadline;static bool resume;
    static string[] Route=>new[]{CombatTestMapSetup.FarmScene,InteriorWorldSetup.Home,CombatTestMapSetup.FarmScene,TriangleWorldSetup.Passage,
        WorldFrameworkSetup.Town,InteriorWorldSetup.Guild,WorldFrameworkSetup.Town,InteriorWorldSetup.Smith,WorldFrameworkSetup.Town,
        WorldFrameworkSetup.Store,WorldFrameworkSetup.Town,TriangleWorldSetup.Field,CombatTestMapSetup.FarmScene,TriangleWorldSetup.Field,
        TriangleWorldSetup.Forest,TriangleWorldSetup.Field,TriangleWorldSetup.Training,TriangleWorldSetup.Field,CombatTestMapSetup.CombatScene,
        TriangleWorldSetup.Field,WorldFrameworkSetup.Town,TriangleWorldSetup.Passage,CombatTestMapSetup.FarmScene}
        .SelectMany(path=>TowerDungeonSetup.Installed && path==CombatTestMapSetup.CombatScene?new[]{TowerDungeonSetup.Lobby,path,TowerDungeonSetup.Lobby}:new[]{path}).ToArray();
    static InteriorWorldCheck(){if(Application.isBatchMode&&SessionState.GetBool(Key,false)){resume=SessionState.GetBool(Key+"Resume",false);deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update+=Tick;}}
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Guard();InteriorWorldSetup.Apply();WorldConnectionValidator.Validate();
        foreach(var path in TriangleWorldSetup.Paths)
        {
            EditorSceneManager.OpenScene(path);var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();var collision=new MapCollision(f);
            var points=f.GetComponentsInChildren<MapExit>().Select(e=>(Vector2)e.transform.position)
                .Concat(f.GetComponentsInChildren<MapArrivalPoint>().Select(a=>(Vector2)a.transform.position))
                .Concat(f.GetComponentsInChildren<ShopNpc>().Select(n=>(Vector2)n.transform.position+Vector2.down*2.1f));
            foreach(var point in points)
            {
                var route=collision.FindPath(f.SpawnPosition,point,f.PlayerRadius);
                Check(collision.CanStand(point,f.PlayerRadius)&&(Vector2.Distance(f.SpawnPosition,point)<.4f||(route.Count>0&&Vector2.Distance(route.Last(),point)<.4f)),"Blocked interior/world approach: "+path+" "+point);
            }
        }
        Start(false);
    }
    public static void Resume(){Guard();Start(true);}
    static void Guard(){if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");}
    static void Start(bool resume){EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Resume",resume);EditorApplication.isPlaying=true;}
    static void Capture(FarmGame f)
    {
        var camera=Camera.main;var old=camera.transform.position;float size=camera.orthographicSize;
        var rt=new RenderTexture(1100,850,24);var previous=RenderTexture.active;
        camera.transform.position=new Vector3(0,0,-10);camera.orthographicSize=f.WorldBounds.height*.55f;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1100,850,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1100,850),0,0);image.Apply();File.WriteAllBytes("Interior-"+Path.GetFileNameWithoutExtension(f.gameObject.scene.path)+".png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;camera.transform.position=old;camera.orthographicSize=size;UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);
    }
    static void Travel(FarmGame f,string target)
    {
        var door=f.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==target);
        if(door.IsCombatBlocked(f))CombatCheckAccess.DefeatHostiles(f.Combat);
        f.TeleportPlayer(f.SpawnPosition);door.TryActivate(f,false);f.TeleportPlayer(door.transform.position);
        Check(door.TryActivate(f,door.requireInteract),"Door failed: "+target+" "+GameSession.LastError);
    }
    static void Finish(string text){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Debug.Log(text);EditorApplication.Exit(0);}
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Interior play timeout "+step);
            if(!EditorApplication.isPlaying||Time.timeSinceLevelLoad<.4f||GameSession.Busy||Time.unscaledTime<GameSession.ArrivalGraceUntil)return;
            var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();if(f==null||f.Combat==null)return;
            Check(string.IsNullOrEmpty(GameSession.LastError),GameSession.LastError);
            Check(new MapCollision(f).CanStand(f.PlayerPosition,f.PlayerRadius),"Blocked arrival "+step);
            if(resume)
            {
                if(step==0){Check(f.gameObject.scene.path==InteriorWorldSetup.Home,"Did not resume inside home");Check(f.InventoryCount==4,"Resume inventory mismatch");step=1;Travel(f,CombatTestMapSetup.FarmScene);return;}
                var savedPoint=JsonUtility.FromJson<Point>(File.ReadAllText("HouseReturnPoint.json")).position;
                Check(f.gameObject.scene.path==CombatTestMapSetup.FarmScene&&Vector2.Distance(f.PlayerPosition,savedPoint)<.1f,"Restart lost moved-house return location");
                Check(f.GetComponentsInChildren<FarmBuilding>().Any(b=>b.runtimePlaced&&b.GetComponentInChildren<HouseInteriorLink>()!=null),"Reinstalled house lost its door");Finish("HOUSE_INTERIOR_RESTART_SUCCESS");return;
            }
            if(step<Route.Length)
            {
                Check(f.gameObject.scene.path==Route[step],"Wrong room at "+step);
                if(step==0)
                {
                    f.Shop.RestoreGold(gold);
                    var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");f.AddItem(seed);f.TeleportPlayer(new Vector2(0,-3.5f));
                    Check(f.PlantSelected(new Vector2Int(0,-2)),"Farm crop preparation failed");f.Water(new Vector2(0,-2),.1f);f.Tick(seed.growthSeconds);
                    f.DropItem(Resources.Load<PlantDefinition>("Equipment/Heart"),new Vector2(2,-4));
                }
                else Check(f.Gold==gold,"Gold lost "+step);
                Check(f.InventoryCount==4,"Inventory changed "+step);
                if(InteriorWorldSetup.Rooms.Contains(f.gameObject.scene.path)){Check(!f.Layout.dayNightLighting&&f.Combat.EnemyCount==0,"Hostile/dark interior");Capture(f);}
                if(step==5)
                {
                    var npc=f.GetComponentsInChildren<ShopNpc>().Single(n=>n.name=="Guild reception");f.TeleportPlayer(new Vector2(0,1.2f));
                    Check(f.Shop.Open(npc)&&!f.Shop.IsTrading&&!f.Shop.Buy(0),"Guild receptionist should provide dialogue only");f.Shop.Close();
                }
                if(step==7)
                {
                    var npc=f.GetComponentInChildren<ShopNpc>();f.TeleportPlayer(new Vector2(0,1.2f));f.Cycle.RestoreElapsed(0);
                    Check(f.Shop.Open(npc)&&f.Shop.IsTrading,"Smith trade unavailable");var offer=f.Shop.npcs.Single().offers[0];
                    Check(f.Shop.Buy(0)&&f.InventoryCount==5,"Smith purchase failed");Check(f.Shop.Sell(4)&&f.InventoryCount==4,"Smith sale failed");gold=gold-offer.buyPrice+f.Shop.SalePrice(offer.item);
                    Check(f.Gold==gold,"Smith prices wrong");f.Shop.RestoreGold(0);Check(!f.Shop.Buy(0),"Smith allowed unfunded purchase");f.Shop.RestoreGold(gold);f.Shop.Close();
                }
                if(step<Route.Length-1){step++;Travel(f,Route[step]);return;}
                Check(f.IsMature(new Vector2Int(0,-2))&&f.WorldPieceCount==1,"Farm crop/drop state lost after full world loop");
                // Harvest/reinstall at a different location, then enter the same shared room.
                var house=f.GetComponentInChildren<FarmBuilding>();var body=house.GetComponentsInChildren<SpriteRenderer>().First(r=>r.GetComponentInParent<MapExit>()==null);
                var point=new Vector2(body.bounds.center.x,body.bounds.min.y+.1f);f.TeleportPlayer(point+Vector2.down*.5f);
                Check(f.HarvestBuildingAt(point)&&f.InventoryCount==5&&f.GetComponentsInChildren<HouseInteriorLink>().Length==0,"Harvest left a ghost entrance");
                f.TeleportPlayer(new Vector2(8,-4));Check(f.PlaceHouse(new Vector2(8,-2))&&f.InventoryCount==4,"House reinstall failed");
                var door=f.GetComponentInChildren<FarmBuilding>().GetComponentInChildren<MapExit>();
                var returnPoint=(Vector2)door.transform.position;File.WriteAllText("HouseReturnPoint.json",JsonUtility.ToJson(new Point{position=returnPoint}));
                Check(new MapCollision(f).CanStand(returnPoint,f.PlayerRadius),"Reinstalled door blocked");
                step++;Travel(f,InteriorWorldSetup.Home);return;
            }
            Check(f.gameObject.scene.path==InteriorWorldSetup.Home,"Moved house did not enter room");
            Check(GameSession.Save(f),"Home autosave failed");
            Finish("BUILDING_INTERIORS_PLAY_SUCCESS: "+TriangleWorldSetup.Paths.Length+" maps, "+(Route.Length-1)+" directed routes, guild dialogue, smith buy/sell/funds, inventory/gold, movable house door and inside save.");
        }
        catch(Exception e){SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    [Serializable]sealed class Point{public Vector2 position;}
}
