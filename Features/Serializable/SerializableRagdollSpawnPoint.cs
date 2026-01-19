using AdminToys;
using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Interfaces;
using RelativePositioning;
using UnityEngine;
using YamlDotNet.Serialization;
using Object = UnityEngine.Object;
using Ragdoll = Exiled.API.Features.Ragdoll;
using Random = UnityEngine.Random;
using Room = Exiled.API.Features.Room;
using Server = Exiled.API.Features.Server;

namespace ProjectMER.Features.Serializable;

public class SerializableRagdollSpawnPoint : SerializableObject, IIndicatorDefinition
{
    /// <summary>
    /// Gets or sets the name of the ragdoll to spawned.
    /// <para>If this is empty, a random name will be choosen from <see cref="MapSchematic.RagdollRoleNames"/>.</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the RoleType of the ragdoll to spawn.
    /// </summary>
    public RoleTypeId RoleType { get; set; } = RoleTypeId.ClassD;

    /// <summary>
    /// Gets or sets the death reason of the ragdoll to spawn.
    /// </summary>
    public string DeathReason { get; set; } = "None";

    /// <summary>
    /// Gets or sets the spawn chance of the ragdoll.
    /// </summary>
    public int SpawnChance { get; set; } = 100;

    [YamlIgnore]
    public override Vector3 Scale { get; set; }
    
    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null, bool isForced = false)
    {        
        if (!isForced && Random.Range(0, 101) > SpawnChance)
            return null;
        
        if (instance != null)
            Object.Destroy(instance);
        
        RagdollData ragdollInfo;
        if (byte.TryParse(DeathReason, out byte deathReasonId) && deathReasonId <= 22)
            ragdollInfo = new RagdollData(Server.Host.ReferenceHub, new UniversalDamageHandler(-1f, DeathTranslations.TranslationsById[deathReasonId]), RoleType, new RelativePosition(Position), Quaternion.Euler(Rotation), Name, double.MaxValue);
        else
            ragdollInfo = new RagdollData(Server.Host.ReferenceHub, new CustomReasonDamageHandler(DeathReason), RoleType, new RelativePosition(Position), Quaternion.Euler(Rotation), Name, double.MaxValue);

        if (!Ragdoll.TryCreate(ragdollInfo, out Ragdoll ragdoll))
            return null;

        return ragdoll.GameObject;
    }

    public GameObject SpawnOrUpdateIndicator(Room room, GameObject? instance = null)
    {
        PrimitiveObjectToy cube;

        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);

        if (instance == null)
        {
            cube = Object.Instantiate(PrefabManager.PrimitiveObject);
            cube.NetworkPrimitiveType = PrimitiveType.Cube;
            cube.NetworkPrimitiveFlags = PrimitiveFlags.Visible;
            cube.NetworkMaterialColor = new(0f, 1f, 0.5f, 0.9f);
            cube.transform.localScale = Vector3.one * 0.25f;
        }
        else
            cube = instance.GetComponent<PrimitiveObjectToy>();

        cube.transform.SetPositionAndRotation(position, rotation);

        return cube.gameObject;
    }
}