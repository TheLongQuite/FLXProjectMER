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
        if (Teleports.IsEmpty())
        {
            foreach (TargetTeleporter teleport in Base.TargetTeleporters)
            {
                if (teleport.Chance <= 0)
                {
                    Log.Error($"Телепорт с ID объекта {teleport.Id} имеет шанс меньше или равен нулю. Устанавливаю 1 как дефолтное значение");
                    teleport.Chance = 1;
                }
                
                Teleports.Add(teleport.Id, teleport.Chance);
            }
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
            Vector3 newPosition = target.transform.TransformPoint(localOffset);

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

    private void TeleportRigidbody(Rigidbody rb, TeleportObject target)
    {
        (Vector3 newPosition, Vector3 newVelocity, Vector3 newAngularVelocity) = CalculateTransformedPhysics(rb,
            transform, target.transform);

        rb.position = newPosition;
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