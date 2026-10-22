using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class CombatRefactorCheck
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        if (!Application.isBatchMode || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use an isolated temporary project for combat checks.");
        MovementCollisionCheck.Run();
        HeartHarvestCheck.Run();
        CheckLifecycle();
        Debug.Log("COMBAT_REFACTOR_SUCCESS: existing collision/heart checks, enemy respawn and interruption, hostile shot cleanup, training exclusion, restored HP/kills/immunity and avoidable enemy shots.");
    }

    private static void CheckLifecycle()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var definitions = new[] { "Hoe", "WateringCan", "Stone", "Cloud", "InventorySlot" }
            .Select(name => AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/" + name + ".asset")).ToArray();
        var farm = new GameObject("Combat lifecycle fixture").AddComponent<FarmGame>();
        farm.Configure(definitions);
        farm.Initialize(false);
        var combat = farm.Combat;
        combat.SetNightActive(true);
        farm.TeleportPlayer(new Vector2(19, -12));
        combat.SpawnEnemy(new Vector2(20, -12), false);
        var enemies = CombatCheckAccess.Enemies(combat);
        var enemy = enemies[enemies.Count - 1];
        var model = (SpriteRenderer)CombatCheckAccess.Read(enemy, "model");
        var warning = (SpriteRenderer)CombatCheckAccess.Read(enemy, "warning");
        var weapon = ScriptableObject.CreateInstance<PlantDefinition>();
        weapon.effect = GrowthEffect.Weapon;
        weapon.damage = 1000;
        weapon.attackRange = 3;
        weapon.attackInterval = .1f;
        Check(combat.Attack(weapon, model.transform.position) && !model.enabled && combat.Kills == 1, "Normal death did not count exactly once");
        combat.TickCombat(26);
        Check(!model.enabled && combat.Kills == 1, "Enemy respawned beside player or death counted twice");
        farm.TeleportPlayer(new Vector2(12, -12));
        combat.TickCombat(.1f);
        Check(model.enabled && (float)CombatCheckAccess.Read(enemy, "health") == 55, "Distant enemy did not respawn");
        farm.TeleportPlayer(new Vector2(19, -12));
        combat.TickCombat(1.1f);
        combat.TickCombat(.1f);
        Check(warning.enabled && (float)CombatCheckAccess.Read(enemy, "windup") > 0, "Enemy did not prepare an attack");
        weapon.damage = 1;
        Check(combat.Attack(weapon, model.transform.position) && !warning.enabled && (float)CombatCheckAccess.Read(enemy, "windup") == 0, "Hit did not interrupt attack");
        Vector3 hitPosition = model.transform.position;
        combat.TickCombat(.04f);
        Check(model.transform.position == hitPosition, "Staggered enemy moved immediately");

        CombatCheckAccess.Shoot(combat, new Vector2(22, 13), Vector2.left, 10, false);
        CombatCheckAccess.Shoot(combat, new Vector2(22, 13), Vector2.left, 10, true);
        combat.SetNightActive(false);
        var shots = CombatCheckAccess.Shots(combat);
        Check(shots.Cast<object>().All(shot => !(bool)CombatCheckAccess.Read(shot, "hostile")) && shots.Count == 1, "Schedule switch removed player shot or kept hostile shot");
        combat.SpawnEnemy(new Vector2(-10, -12), false, null, true);
        var dummy = enemies[enemies.Count - 1];
        var dummyModel = (SpriteRenderer)CombatCheckAccess.Read(dummy, "model");
        farm.TeleportPlayer(new Vector2(-11, -12));
        combat.TickCombat(.2f);
        int kills = combat.Kills;
        weapon.damage = 1000;
        Check(combat.Attack(weapon, dummyModel.transform.position) && !dummyModel.enabled && combat.Kills == kills, "Dummy gave a kill reward");
        combat.TickCombat(3.1f);
        Check(dummyModel.enabled && combat.Kills == kills, "Nearby dummy did not return without rewards");

        combat.RestorePlayerState(50, 1, 7, false);
        combat.DamagePlayer(10);
        Check(combat.Health == 50 && combat.MaxHealth == 80 && combat.Kills == 7, "Restore lost health/heart/kill state or arrival immunity");
        combat.TickCombat(1.1f);
        combat.DamagePlayer(10);
        Check(combat.Health == 40, "Restored immunity never expired");
        combat.SetNightActive(true);
        combat.SpawnEnemy(new Vector2(20, 10), true);
        farm.TeleportPlayer(new Vector2(16, 10));
        combat.TickCombat(1.1f);
        combat.TickCombat(.51f);
        Check(shots.Cast<object>().Any(shot => (bool)CombatCheckAccess.Read(shot, "hostile")), "Spitter did not fire its prepared shot");
        float before = combat.Health;
        farm.TeleportPlayer(new Vector2(16, 8));
        combat.TickCombat(.5f);
        Check(combat.Health == before, "Enemy shot followed the moved player");
        UnityEngine.Object.DestroyImmediate(weapon);
    }
}
