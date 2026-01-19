using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Scp106;
using FLXLib.Extensions;
using Mirror;
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
                Teleports.Add(teleport.Id, teleport.Chance);
        }

        foreach (TeleportObject teleportObject in FindObjectsByType<TeleportObject>(FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            if (teleportObject._mapEditorObject.Id != Teleports.NextWithReplacement())
                continue;

            return teleportObject;
        }

        return null;
    }

    public void OnTriggerEnter(Collider other)
    {
        Player? player = Player.Get(other.gameObject);
        if (player is null)
            return;

        if (ObjectAndNextUseTime.TryGetValue(other.gameObject, out DateTime time) && time > DateTime.Now)
            return;

        if (player.IsConnected && !Base.AllowedRoles.Contains(player.GetCustomOrBasicRole()))
            return;

        bool flag =
            (!Map.IsLczDecontaminated || !Base.LockOnEvent.HasFlagFast(LockOnEvent.LightDecontaminated)) &&
            (!Warhead.IsDetonated || !Base.LockOnEvent.HasFlagFast(LockOnEvent.WarheadDetonated));

        if (!flag)
            return;

        string objectTag = other.GetComponentInParent<NetworkIdentity>()?.gameObject.tag;
        if (objectTag == null)
            return;

        if (objectTag == "Player" && !Base.TeleportFlags.HasFlagFast(TeleportFlags.Player) ||
            objectTag == "Projectile" && !Base.TeleportFlags.HasFlagFast(TeleportFlags.ActiveGrenade) ||
            objectTag == "Pickup" && !Base.TeleportFlags.HasFlagFast(TeleportFlags.Pickup))
            return;

        TeleportObject? target = GetRandomTarget();
        if (target == null)
            return;

        DateTime dateTime = DateTime.Now.AddSeconds(Base.Cooldown);
        ObjectAndNextUseTime[other.gameObject] = dateTime;
        target.ObjectAndNextUseTime[other.gameObject] = dateTime;

        player.Position = target.gameObject.transform.position;
        player.Rotation = Quaternion.Euler(target.gameObject.transform.eulerAngles);
        
        TeleportingEventArgs ev = new(this, target, player, gameObject, player.Position, player.Rotation, Base.TeleportSoundId);
        Teleport.OnTeleporting(ev);
        
        int teleportSoundId = Base.TeleportSoundId;
        if (teleportSoundId != -1)
        {
            Log.Assert(teleportSoundId >= 0 && teleportSoundId <= 31,
                $"The teleport sound id must be between 0 and 31. It is currently {teleportSoundId} for teleport with [{
                    Base.TargetTeleporters}] targets.");

            MirrorExtensions.SendFakeTargetRpc(player, ReferenceHub._hostHub.networkIdentity,
                typeof(AmbientSoundPlayer), "RpcPlaySound", teleportSoundId);
        }
    }
}