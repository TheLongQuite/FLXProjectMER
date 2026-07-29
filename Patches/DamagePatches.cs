using System.Collections.Generic;
using HarmonyLib;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using ProjectMER.Features.Components;
using UnityEngine;

namespace ProjectMER.Patches;

[HarmonyPatch(typeof(Scp096HitHandler), nameof(Scp096HitHandler.ProcessHits))]
public static class Scp096ProcessHitsPatch
{
    [HarmonyPostfix]
    public static void Postfix(Scp096HitHandler __instance, int count)
    {
        HashSet<uint> processedNetIds = new();
        
        for (int i = 0; i < count; i++)
        {
            Collider hit = Scp096HitHandler.Hits[i];
            DamageableComponent dmgComp = hit.GetComponentInParent<DamageableComponent>();
            
            if (dmgComp != null)
            {
                if (processedNetIds.Add(dmgComp.NetworkId))
                    __instance.DealDamage(dmgComp, __instance._humanTargetDamage);
            }
        }
    }
}

[HarmonyPatch(typeof(Scp939Motor), nameof(Scp939Motor.OverlapCapsule))]
public static class Scp939OverlapPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Scp939Motor __instance, Vector3 point1, Vector3 point2)
    {
        int count = Physics.OverlapCapsuleNonAlloc(point1, point2, 0.6f, Scp939Motor.Detections, (int)Scp939Motor.Mask);
        
        HashSet<uint> processedNetIds = new();
        for (int i = 0; i < count; i++)
        {
            Collider hit = Scp939Motor.Detections[i];
            if (hit.TryGetComponent<HitboxIdentity>(out var hid))
            {
                if (processedNetIds.Add(hid.NetworkId))
                    __instance.ProcessHitboxCollision(hid);
                
                continue;
            }
            
            if (hit.TryGetComponent<BreakableWindow>(out var window))
            {
                if (processedNetIds.Add(window.NetworkId))
                    __instance.ProcessWindowCollision(window);
                
                continue;
            }

            DamageableComponent dmgComp = hit.GetComponentInParent<DamageableComponent>();
            if (dmgComp != null)
            {
                if (processedNetIds.Add(dmgComp.NetworkId))
                 dmgComp.Damage(120f, new Scp939DamageHandler(__instance._role, 120f, Scp939DamageType.LungeTarget), point1);
            }
        }
        
        return false;
    }
}

[HarmonyPatch(typeof(Scp018Projectile), nameof(Scp018Projectile.RegisterBounce))]
public static class Scp018BouncePatch
{
    [HarmonyPrefix]
    public static bool Prefix(Scp018Projectile __instance, float velocity, Vector3 point)
    {
        __instance._lastVelocity = velocity;
        __instance._bypassBounceSoundCooldown = true;
        __instance.MakeCollisionSound(Mathf.Max(10f, velocity * velocity));
        
        if (!NetworkServer.active)
            return false;

        int bounceMask = (int)Scp018Projectile.BounceHitregMask | LayerMask.GetMask("Hitbox");
        int count = Physics.OverlapSphereNonAlloc(point, __instance._bounceHitregRadius, Scp018Projectile.HitregDetections, bounceMask);
        
        HashSet<uint> processedNetIds = new();

        for (int i = 0; i < count; i++)
        {
            Collider hitregDetection = Scp018Projectile.HitregDetections[i];
            
            if (hitregDetection.TryGetComponent<BreakableWindow>(out var window))
            {
                window.Damage(__instance.CurrentDamage, new Scp018DamageHandler(__instance, __instance.CurrentDamage, __instance.IgnoreFriendlyFire), point);
            }
            else if (hitregDetection.TryGetComponent<InteractableCollider>(out var interactable) && interactable.Target is IDamageableDoor door)
            {
                door.ServerDamage(__instance.CurrentDamage * __instance._doorDamageMultiplier, DoorDamageType.Grenade, __instance.PreviousOwner);
            }
            
            DamageableComponent dmgComp = hitregDetection.GetComponentInParent<DamageableComponent>();
            if (dmgComp != null && processedNetIds.Add(dmgComp.NetworkId))
            {
                dmgComp.Damage(__instance.CurrentDamage, new Scp018DamageHandler(__instance, __instance.CurrentDamage, __instance.IgnoreFriendlyFire), point);
            }
        }
        
        __instance._damagedPlayersSinceLastBounce.Clear();
        
        return false;
    }
}