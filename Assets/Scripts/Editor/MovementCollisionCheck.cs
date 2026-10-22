using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class MovementCollisionCheck
{
    static void Check(bool value,string message) { if(!value) throw new Exception(message); }
    public static void Verify() { Run(); HousePlantingCheck.Run(); }
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Run in an isolated project copy.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var data=new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}.Select(name=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+name+".asset")).ToArray();
        var farm=new GameObject("Collision test farm").AddComponent<FarmGame>(); farm.Configure(data); farm.Initialize(false);
        var wall=new GameObject("Wall").AddComponent<MapObstacle>(); wall.transform.position=new Vector2(18,8); wall.size=new Vector2(1,4);
        farm.TeleportPlayer(new Vector2(16,8)); farm.MovePlayer(new Vector2(22,8));
        Check(farm.PlayerPosition.x<17.251f && farm.PlayerPosition.x>17,"Fast movement crossed wall or stopped early");
        farm.TeleportPlayer(new Vector2(16,8)); farm.MovePlayer(new Vector2(20,10));
        Check(farm.PlayerPosition.x<17.251f && farm.PlayerPosition.y>9.99f,"Diagonal movement did not slide");
        farm.TeleportPlayer(new Vector2(17,8)); farm.Combat.Roll(Vector2.right); farm.Combat.TickCombat(.2f);
        Check(farm.PlayerPosition.x<17.251f,"Dodge tunneled through wall");
        wall.enabled=false; farm.MovePlayer(new Vector2(20,8)); Check(farm.PlayerPosition.x>19.99f,"Disabled obstacle still blocks"); wall.enabled=true;
        var collision=new MapCollision(farm);
        var path=collision.FindPath(new Vector2(16,8),new Vector2(20,8),.35f);
        Check(path.Count>0 && Vector2.Distance(path[path.Count-1],new Vector2(20,8))<.1f && path.Any(p=>p.y<5.65f || p.y>10.35f),"No route around wall");
        Vector2 previous=new Vector2(16,8);
        foreach(var next in path) { Check(collision.Trace(previous,next,.35f)>=1,"Navigation cut through a corner"); previous=next; }
        // Actual combat ticks: pursue around a wall, then prevent two enemies from stacking.
        farm.TeleportPlayer(new Vector2(16,8)); farm.Combat.SetNightActive(true); farm.Combat.SpawnEnemy(new Vector2(20,8),false);
        var chaser=farm.Combat.GetComponentsInChildren<SpriteRenderer>().Last(r=>r.name=="Chaser");
        bool detour=false;
        for(int i=0;i<200;i++) { farm.Combat.TickCombat(.05f); var p=(Vector2)chaser.transform.position; Check(collision.CanStand(p,.349f),"Enemy entered an obstacle"); detour|=p.y<5.66f || p.y>10.34f; }
        Check(detour && chaser.transform.position.x<17.5f,"Enemy failed to follow route");
        farm.Combat.SpawnEnemy(new Vector2(20,8),false); farm.Combat.SpawnEnemy(new Vector2(20,8),false);
        var pair=farm.Combat.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name=="Chaser").TakeLast(2).ToArray();
        for(int i=0;i<20;i++) farm.Combat.TickCombat(.05f);
        Check(Vector2.Distance(pair[0].transform.position,pair[1].transform.position)>.5f,"Enemies remained stacked");
        // A fast bullet must hit the wall before an actor behind it, in either direction.
        var combat=farm.Combat; farm.TeleportPlayer(new Vector2(16,8));
        float hp=combat.Health;
        CombatCheckAccess.Shoot(combat,new Vector2(20,8),new Vector2(-100,0),10f,true,2.5f); combat.TickCombat(.05f);
        Check(combat.Health==hp,"Enemy bullet crossed wall");
        var enemies=CombatCheckAccess.Enemies(combat);
        foreach(var enemy in enemies) enemy.GetType().GetField("health").SetValue(enemy,0f);
        combat.SpawnEnemy(new Vector2(20,8),false); var target=enemies[enemies.Count-1];
        var health=target.GetType().GetField("health");
        CombatCheckAccess.Shoot(combat,new Vector2(16,8),new Vector2(100,0),20f,false,2.5f); combat.TickCombat(.05f);
        Check((float)health.GetValue(target)==55f,"Player bullet crossed wall");
        wall.blocksProjectiles=false;
        CombatCheckAccess.Shoot(combat,new Vector2(16,8),new Vector2(100,0),20f,false,2.5f); combat.TickCombat(.05f);
        Check((float)health.GetValue(target)<55f,"Pass-through projectile setting failed");
        // Insert far target first: the nearer target must still receive the shot.
        wall.enabled=false; foreach(var enemy in enemies) enemy.GetType().GetField("health").SetValue(enemy,0f);
        combat.SpawnEnemy(new Vector2(20,8),false); var far=enemies[enemies.Count-1];
        combat.SpawnEnemy(new Vector2(18,8),false); var near=enemies[enemies.Count-1];
        CombatCheckAccess.Shoot(combat,new Vector2(16,8),new Vector2(100,0),20f,false,2.5f); combat.TickCombat(.05f);
        Check((float)health.GetValue(far)==55f && (float)health.GetValue(near)==35f,"Projectile did not hit nearest target");
        foreach(var enemy in enemies) enemy.GetType().GetField("health").SetValue(enemy,0f);
        wall.enabled=true; wall.blocksProjectiles=true; wall.size=new Vector2(.2f,4);
        farm.TeleportPlayer(new Vector2(17.5f,8)); combat.SpawnEnemy(new Vector2(18.5f,8),false);
        var behind=enemies[enemies.Count-1];
        var sword=Resources.Load<PlantDefinition>("Equipment/Sword");
        Check(combat.Attack(sword.harvestResult,new Vector2(18.5f,8)),"Melee test could not attack");
        Check((float)health.GetValue(behind)==55f,"Melee passed through wall");
        var building=new GameObject("Harvestable collision").AddComponent<FarmBuilding>();
        building.transform.SetParent(farm.transform); building.transform.position=new Vector2(22,11); building.footprint=Vector2.one; building.offset=Vector2.zero;
        Check(new MapCollision(farm).Trace(new Vector2(21,11),new Vector2(23,11),.25f)<1,"Building did not block movement");
        UnityEngine.Object.DestroyImmediate(building.gameObject);
        Check(new MapCollision(farm).Trace(new Vector2(21,11),new Vector2(23,11),.25f)>=1,"Removed building still blocked movement");
        MapObstacleInspector.GeneratePrefab();
        Debug.Log("MOVEMENT_COLLISION_SUCCESS: swept movement and dodge, sliding, obstacle toggle, navigation, separation, both projectile directions, projectile flags and nearest impact.");
    }
}
