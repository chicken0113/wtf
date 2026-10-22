using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class WorldRouteAudit
{
    public static void Verify()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR")))throw new Exception("Use isolated project and saves.");
        Validate();InteriorWorldCheck.Run();
    }
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static float Length(FarmGame f,Vector2 start,Vector2 end)
    {
        var collision=new MapCollision(f);var path=collision.FindPath(start,end,f.PlayerRadius);
        if(!collision.CanStand(start,f.PlayerRadius)||!collision.CanStand(end,f.PlayerRadius)
            ||(Vector2.Distance(start,end)>.3f&&(path.Count==0||Vector2.Distance(path.Last(),end)>.3f)))throw new Exception("Blocked world route: "+f.gameObject.scene.path+" "+start+" -> "+end);
        float length=0;foreach(var point in path){length+=Vector2.Distance(start,point);start=point;}return length;
    }
    public static void Validate()
    {
        WorldConnectionValidator.Validate();int routes=0;
        foreach(var path in TriangleWorldSetup.Paths)
        {
            EditorSceneManager.OpenScene(path);var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
            foreach(var point in f.GetComponentsInChildren<MapExit>().Select(e=>(Vector2)e.transform.position)
                .Concat(f.GetComponentsInChildren<FarmItemPlacement>().Select(p=>(Vector2)p.transform.position))
                .Concat(f.GetComponentsInChildren<ShopNpc>().Select(n=>(Vector2)n.transform.position+Vector2.down*2.1f)))
            {Length(f,f.SpawnPosition,point);routes++;}
        }
        EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var town=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        var guildArrival=town.GetComponentsInChildren<MapArrivalPoint>().Single(a=>a.arrivalId=="town_from_guild");
        var fieldExit=town.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==TriangleWorldSetup.Field);
        float outward=Length(town,guildArrival.transform.position,fieldExit.transform.position);
        var townArrival=town.GetComponentsInChildren<MapArrivalPoint>().Single(a=>a.arrivalId=="town_from_dungeon_path");
        var guildExit=town.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==InteriorWorldSetup.Guild);
        float inward=Length(town,townArrival.transform.position,guildExit.transform.position);
        EditorSceneManager.OpenScene(TriangleWorldSetup.Field);var field=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        outward+=Length(field,field.GetComponentsInChildren<MapArrivalPoint>().Single(a=>a.arrivalId=="field_from_town").transform.position,
            field.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==(TowerDungeonSetup.Installed?TowerDungeonSetup.Lobby:CombatTestMapSetup.CombatScene)).transform.position);
        inward+=Length(field,field.GetComponentsInChildren<MapArrivalPoint>().Single(a=>a.arrivalId=="field_from_dungeon").transform.position,
            field.GetComponentsInChildren<MapExit>().Single(e=>e.destinationScene==WorldFrameworkSetup.Town).transform.position);
        var settings=Resources.Load<PlayerControlSettings>("PlayerControls");float speed=settings!=null?Mathf.Max(.1f,settings.moveSpeed):4.5f;
        System.IO.File.WriteAllText("WorldRouteAudit.txt",$"Maps: {TriangleWorldSetup.Paths.Length}\nApproaches: {routes}\nGuild to dungeon exterior distance: {outward:F1}\nDungeon to guild exterior distance: {inward:F1}\nEstimated movement seconds at speed {speed:F1}, excluding loading: {outward/speed:F1} outward, {inward/speed:F1} return\n");
        Debug.Log($"WORLD_ROUTES_AUDITED: {routes} approaches; guild-dungeon {outward:F1}/{inward:F1} units round trip legs.");
    }
}
