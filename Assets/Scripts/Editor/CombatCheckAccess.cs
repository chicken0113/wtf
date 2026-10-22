using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using EverythingGrows;

// Isolated fixtures sometimes need a shot or enemy state. Keep that private-layout
// knowledge here instead of spreading it across gameplay checks or runtime APIs.
public static class CombatCheckAccess
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static object Read(object owner, string name)
    {
        var field = owner.GetType().GetField(name, Fields);
        if (field == null) throw new MissingFieldException(owner.GetType().Name, name);
        return field.GetValue(owner);
    }

    public static IList Enemies(OpenFieldCombat combat) => (IList)Read(Read(combat, "enemySystem"), "enemies");
    public static IList Shots(OpenFieldCombat combat) => (IList)Read(Read(combat, "projectiles"), "shots");
    public static void Shoot(OpenFieldCombat combat, Vector2 point, Vector2 velocity, float damage, bool hostile, float life = 2.5f)
    {
        var owner = Read(combat, "projectiles");
        var method = owner.GetType().GetMethod("Spawn", Fields);
        if (method == null) throw new MissingMethodException(owner.GetType().Name, "Spawn");
        method.Invoke(owner, new object[] { point, velocity, damage, hostile, life });
    }
}
