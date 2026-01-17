using PlayerRoles;
using UnityEngine;
using YamlDotNet.Serialization;

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
}