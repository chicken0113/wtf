using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;
public static class ProjectileConfigurationCheck
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run()
    {
        Check(Application.isBatchMode && Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase),"Isolated project only");
        ProjectSetupCheck.Run();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var definitions=new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"};var items=Array.ConvertAll(definitions,n=>AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+n+".asset"));
        var farm=new GameObject("Projectile configuration fixture").AddComponent<FarmGame>();farm.Configure(items);farm.Initialize(false);Check(farm.IsInitialized,farm.InitializationError);
        var c=farm.Combat;c.SetNightActive(true);farm.TeleportPlayer(new Vector2(18,-12));
        var bow=Resources.Load<PlantDefinition>("Equipment/Bow");var gun=Resources.Load<PlantDefinition>("Equipment/Gun");var type=MonsterType.Default(true);
        Check(ProjectileSettings.Distance(bow.attackRange,bow.projectileSpeed,bow.projectileLifetime)==8 && ProjectileSettings.Distance(gun.attackRange,gun.projectileSpeed,gun.projectileLifetime)==10,"Default weapon reach changed");
        Check(ProjectileSettings.Distance(type.projectileRange,type.projectileSpeed,type.projectileLifetime)==11.25f,"Default monster reach changed");
        var weapon=UnityEngine.Object.Instantiate(bow);weapon.attackRange=2;weapon.projectileSpeed=10;weapon.projectileLifetime=5;
        Check(c.Attack(weapon,farm.PlayerPosition+Vector2.left),"Range-limited attack failed");var shot=CombatCheckAccess.Shots(c)[0];var model=(SpriteRenderer)CombatCheckAccess.Read(shot,"model");Vector2 start=model.transform.position;
        c.TickCombat(.15f);Check(Mathf.Abs(Vector2.Distance(start,model.transform.position)-1.5f)<.001f,"Per-weapon speed ignored");c.TickCombat(.1f);Check(CombatCheckAccess.Shots(c).Count==0,"Range cap ignored");
        c.TickCombat(1);weapon.attackRange=20;weapon.projectileSpeed=4;weapon.projectileLifetime=.5f;
        Check(c.Attack(weapon,farm.PlayerPosition+Vector2.left),"Time-limited attack failed");shot=CombatCheckAccess.Shots(c)[0];model=(SpriteRenderer)CombatCheckAccess.Read(shot,"model");start=model.transform.position;
        c.TickCombat(.25f);Check(Mathf.Abs(Vector2.Distance(start,model.transform.position)-1)<.001f,"Second weapon speed ignored");c.TickCombat(.26f);Check(CombatCheckAccess.Shots(c).Count==0,"Lifetime cap ignored");
        c.TickCombat(1);weapon.attackRange=.1f;weapon.projectileSpeed=1000;weapon.projectileLifetime=1;
        Check(c.Attack(weapon,farm.PlayerPosition+Vector2.left),"Fast short-range attack failed");
        Check((float)CombatCheckAccess.Read(CombatCheckAccess.Shots(c)[0],"life")<.001f,"Minimum lifetime extends short-range fast shots");c.TickCombat(.1f);
        string original=JsonUtility.ToJson(type);
        try
        {
            type.projectileRange=1.5f;type.projectileSpeed=3;type.projectileLifetime=10;
            c.SpawnEnemy(farm.PlayerPosition+Vector2.right*2,true);float health=c.Health;
            c.TickCombat(1.01f);c.TickCombat(.49f);c.TickCombat(.02f);
            Check(CombatCheckAccess.Shots(c).Count==1,"Monster did not fire");shot=CombatCheckAccess.Shots(c)[0];
            Check(Mathf.Abs(((Vector2)CombatCheckAccess.Read(shot,"velocity")).magnitude-3)<.001f && (float)CombatCheckAccess.Read(shot,"life")<=.5f,"Monster range/speed settings ignored");
            c.TickCombat(.6f);Check(CombatCheckAccess.Shots(c).Count==0 && c.Health==health,"Monster projectile exceeded configured range");
        }
        finally{JsonUtility.FromJsonOverwrite(original,type);UnityEngine.Object.DestroyImmediate(weapon);}
        var report=new SetupValidationReport();var invalid=UnityEngine.Object.Instantiate(bow);invalid.projectileSpeed=0;invalid.projectileLifetime=float.NaN;
        SetupDataValidator.CheckItem(invalid,null,report,new System.Collections.Generic.HashSet<PlantDefinition>(),false);Check(report.HasErrors,"Invalid projectile settings accepted");UnityEngine.Object.DestroyImmediate(invalid);
        Debug.Log("PROJECTILE_CONFIGURATION_SUCCESS: unchanged default ranges, independent weapon speed, distance/time expiry, real monster firing with range cap, and invalid-setting diagnostics.");
    }
}
