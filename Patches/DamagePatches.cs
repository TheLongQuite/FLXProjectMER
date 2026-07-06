using AdvancedMERTools.Components;
using HarmonyLib;
using InventorySystem.Items.ThrowableProjectiles;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using UnityEngine;

namespace ProjectMER.Patches;

[HarmonyPatch(typeof(Scp096HitHandler), nameof(Scp096HitHandler.DamageSphere))]
public static class Scp096DamagePatch
{
    [HarmonyPostfix]
    public static void Postfix(Scp096HitHandler __instance, Vector3 position, float radius)
    {
        Scp096Role? role = __instance._scpRole;
        float damage = __instance._humanTargetDamage;
        Scp096DamageHandler.AttackType attackType = __instance._damageType;
        
        Collider[] hits = Physics.OverlapSphere(position, radius, LayerMask.GetMask("Hitbox"));
        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<HealthObjectProxy>(out HealthObjectProxy? proxy))
                continue;

            Scp096DamageHandler handler = new(role, damage, attackType);
            proxy.Damage(damage, handler, position);
        }
    }
}

[HarmonyPatch(typeof(Scp939Motor), nameof(Scp939Motor.OverlapCapsule))]
public static class Scp939OverlapPatch
{
    [HarmonyPostfix]
    public static void Postfix(Scp939Motor __instance, Vector3 point1, Vector3 point2)
    {
        Scp939Role role = __instance._role;
        int count = Physics.OverlapCapsuleNonAlloc(point1, point2, 0.6f, Scp939Motor.Detections, Scp939Motor.Mask);
        for (int i = 0; i < count; i++)
        {
            if (!Scp939Motor.Detections[i].TryGetComponent<HealthObjectProxy>(out HealthObjectProxy? proxy))
                continue;

            Scp939DamageHandler handler = new(role, 120f, Scp939DamageType.LungeTarget);
            proxy.Damage(120f, handler, point1);
        }
    }
}

[HarmonyPatch(typeof(Scp018Projectile), nameof(Scp018Projectile.RegisterBounce))]
public static class Scp018BouncePatch
{
    [HarmonyPostfix]
    public static void Postfix(Scp018Projectile __instance, Vector3 point)
    {
        Collider[] hits = Physics.OverlapSphere(point, __instance._bounceHitregRadius, LayerMask.GetMask("Hitbox"));
        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent<HealthObjectProxy>(out HealthObjectProxy? proxy))
                continue;

            Scp018DamageHandler handler = new(__instance, __instance.CurrentDamage, __instance.IgnoreFriendlyFire);
            proxy.Damage(__instance.CurrentDamage, handler, point);
        }
    }
}