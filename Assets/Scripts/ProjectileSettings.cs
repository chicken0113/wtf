using UnityEngine;
namespace EverythingGrows
{
    // Constant-speed shots expire at the first of their distance and time limits.
    public static class ProjectileSettings
    {
        public static float Duration(float range,float speed,float maxLifetime) => Mathf.Min(maxLifetime,range/speed);
        public static float Distance(float range,float speed,float maxLifetime) => speed*Duration(range,speed,maxLifetime);
    }
}
