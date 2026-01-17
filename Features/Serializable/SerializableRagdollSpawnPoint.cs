using PlayerRoles;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using YamlDotNet.Serialization;
using Object = UnityEngine.Object;
using Ragdoll = Exiled.API.Features.Ragdoll;
using Random = UnityEngine.Random;
using Room = Exiled.API.Features.Room;
using Server = Exiled.API.Features.Server;

namespace ProjectMER.Features.Serializable;

// TODO: Впихнуть
public class SerializableRagdollSpawnPoint : SerializableObject
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
    
    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null)
    {        
        if (Random.Range(0, 101) > SpawnChance)
            return null;
        
        if (instance != null)
            Object.Destroy(instance);
        
        // TODO: Рандомные Name. 
        RagdollData ragdollInfo;
        if (byte.TryParse(DeathReason, out byte deathReasonId) && deathReasonId <= 22)
            ragdollInfo = new RagdollData(Server.Host.ReferenceHub, new UniversalDamageHandler(-1f, DeathTranslations.TranslationsById[deathReasonId]), RoleType, new RelativePosition(Position), Quaternion.Euler(Rotation), Name, double.MaxValue);
        else
            ragdollInfo = new RagdollData(Server.Host.ReferenceHub, new CustomReasonDamageHandler(DeathReason), RoleType, new RelativePosition(Position), Quaternion.Euler(Rotation), Name, double.MaxValue);

        if (!Ragdoll.TryCreate(ragdollInfo, out Ragdoll ragdoll))
            return null;

        return ragdoll.GameObject;
    }
}