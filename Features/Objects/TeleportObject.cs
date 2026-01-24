using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Pickups.Projectiles;
using FLXLib.Extensions;
using InventorySystem.Items.Pickups;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Utility;
using UnityEngine;
using Weighted_Randomizer;
using TeleportingEventArgs = ProjectMER.Events.Arguments.TeleportingEventArgs;

namespace ProjectMER.Features.Objects;

public class TeleportObject : MonoBehaviour
{
    private const float WallOffset = 0.3f;
    private const float RaycastMargin = 0.1f;
    
    private static readonly int CollisionMask = LayerMask.GetMask("Glass", "Door", "Fence", "Default");

    private void Start()
    {
        _mapEditorObject = GetComponent<MapEditorObject>();
        Base = (SerializableTeleport)_mapEditorObject.Base;
        Teleports = [];
        ObjectAndNextUseTime = new();
    }

    public SerializableTeleport Base;
    private MapEditorObject _mapEditorObject;

    public Dictionary<GameObject, DateTime> ObjectAndNextUseTime;

    public StaticWeightedRandomizer<string> Teleports;

    public TeleportObject? GetRandomTarget()
    {
        Teleports.Clear();
        if (Base.TargetTeleporters.IsEmpty())
            return null;

        foreach (TargetTeleporter teleport in Base.TargetTeleporters)
        {
            if (teleport.Chance <= 0)
            {
                Log.Error($"Телепорт с ID объекта {teleport.Id} имеет шанс меньше или равен нулю. Устанавливаю 1 как дефолтное значение");
                teleport.Chance = 1;
            }
                
            Teleports.Add(teleport.Id, teleport.Chance);
        }
        
        string teleporterId = Teleports.NextWithReplacement();
        foreach (TeleportObject teleportObject in FindObjectsByType<TeleportObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (teleportObject._mapEditorObject.Id != teleporterId)
                continue;

            return teleportObject;
        }

        return null;
    }

    public void OnTriggerEnter(Collider other)
    {
        GameObject targetObject = other.gameObject;

        if (ObjectAndNextUseTime.TryGetValue(other.gameObject, out DateTime time) && time > DateTime.Now)
            return;

        bool flag =
            (!Map.IsLczDecontaminated || !Base.LockOnEvent.HasFlagFast(LockOnEvent.LightDecontaminated)) &&
            (!Warhead.IsDetonated || !Base.LockOnEvent.HasFlagFast(LockOnEvent.WarheadDetonated));

        if (!flag)
            return;

        TeleportObject? target = GetRandomTarget();
        if (target == null)
            return;

        if (targetObject.TryGetComponent(out ReferenceHub hub) && Base.TeleportFlags.HasFlagFast(TeleportFlags.Player))
        {
            Player? player = Player.Get(hub);
            if (player.IsConnected && !Base.AllowedRoles.Contains(player.GetCustomOrBasicRole())
                                   && !Base.AllowedRoles.Contains("all", StringComparison.OrdinalIgnoreCase))
                return;

            Vector3 localOffset = transform.InverseTransformPoint(player.Position);
            localOffset.z = -localOffset.z;
            localOffset.y = 0f;
            Vector3 rawPosition = target.transform.TransformPoint(localOffset);
            rawPosition.y = target.transform.position.y;
            
            Vector3 newPosition = GetSafePosition(target.transform.position, rawPosition);

            float relativeYaw = player.Rotation.eulerAngles.y - transform.eulerAngles.y;
            float newYaw = target.transform.eulerAngles.y + 180f + relativeYaw;

            player.Position = newPosition;
            player.Rotation = Quaternion.Euler(0f, newYaw, 0f);

            int teleportSoundId = Base.TeleportSoundId;
            if (teleportSoundId is >= 0 and <= 31)
            {
                MirrorExtensions.SendFakeTargetRpc(player, ReferenceHub._hostHub.networkIdentity,
                    typeof(AmbientSoundPlayer), "RpcPlaySound", teleportSoundId);
            }
        }
        else if (targetObject.TryGetComponent(out ItemPickupBase projectilePickupBase))
        {
            Pickup pickup = Pickup.Get(projectilePickupBase);
            if (pickup is not Projectile && !Base.TeleportFlags.HasFlagFast(TeleportFlags.Pickup) ||
                pickup is Projectile && !Base.TeleportFlags.HasFlagFast(TeleportFlags.ActiveGrenade))
                return;

            TeleportRigidbody(pickup.Rigidbody, target);
        }
        else
            return;

        DateTime dateTime = DateTime.Now.AddSeconds(Base.Cooldown);
        ObjectAndNextUseTime[other.gameObject] = dateTime;
        target.ObjectAndNextUseTime[other.gameObject] = dateTime;

        TeleportingEventArgs ev = new(this, target, targetObject,
            targetObject.transform.position, targetObject.transform.rotation,
            Base.TeleportSoundId);

        Teleport.OnTeleporting(ev);
    }
    
    private Vector3 GetSafePosition(Vector3 portalCenter, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - portalCenter;
        float distance = direction.magnitude;
        
        if (distance < 0.01f)
            return portalCenter;

        Vector3 normalizedDirection = direction.normalized;
        
        if (Physics.Raycast(portalCenter, normalizedDirection, out RaycastHit hit, distance + RaycastMargin, CollisionMask))
        {
            Vector3 safePosition = hit.point - normalizedDirection * WallOffset;
            return safePosition;
        }
        
        return targetPosition;
    }
    
    private Vector3 GetSafePosition(Vector3 portalCenter, Vector3 targetPosition, float objectRadius)
    {
        Vector3 direction = targetPosition - portalCenter;
        float distance = direction.magnitude;
        
        if (distance < 0.01f)
            return portalCenter;

        Vector3 normalizedDirection = direction.normalized;
        
        if (Physics.SphereCast(portalCenter, objectRadius, normalizedDirection, out RaycastHit hit, distance + RaycastMargin, CollisionMask))
            return hit.point - normalizedDirection * (WallOffset + objectRadius);

        return targetPosition;
    }

    private void TeleportRigidbody(Rigidbody rb, TeleportObject target)
    {
        (Vector3 rawPosition, Vector3 newVelocity, Vector3 newAngularVelocity) = CalculateTransformedPhysics(rb,
            transform, target.transform);
        
        float objectRadius = 0.1f;
        if (rb.TryGetComponent(out Collider col))
        {
            objectRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
        }

        Vector3 safePosition = GetSafePosition(target.transform.position, rawPosition, objectRadius);

        rb.position = safePosition;
        rb.velocity = newVelocity;
        rb.angularVelocity = newAngularVelocity;
    }

    private (Vector3 position, Vector3 velocity, Vector3 angularVelocity) CalculateTransformedPhysics(Rigidbody rb,
        Transform sourcePortal, Transform targetPortal)
    {
        Vector3 localVelocity = sourcePortal.InverseTransformDirection(rb.velocity);
        localVelocity.z = -localVelocity.z;
        Vector3 newVelocity = targetPortal.TransformDirection(localVelocity);

        Vector3 localAngularVelocity = sourcePortal.InverseTransformDirection(rb.angularVelocity);
        localAngularVelocity.z = -localAngularVelocity.z;
        Vector3 newAngularVelocity = targetPortal.TransformDirection(localAngularVelocity);

        Vector3 localOffset = sourcePortal.InverseTransformPoint(rb.position);
        localOffset.z = -localOffset.z;
        Vector3 newPosition = targetPortal.TransformPoint(localOffset);

        return (newPosition, newVelocity, newAngularVelocity);
    }
}