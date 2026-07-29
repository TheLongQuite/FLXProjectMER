using Exiled.API.Enums;
using Exiled.Events.EventArgs.Interfaces;
using ProjectMER.Features.Objects;

namespace ProjectMER.Events.Arguments;

/// <summary>
/// Contains all information before damaging schematic.
/// </summary>
public class SchematicDamagingEventArgs : IDeniableEvent
{
    public SchematicDamagingEventArgs(SchematicObject schematic, float damage, DamageType damageType)
    {
        Schematic = schematic;
        Damage = damage;
        DamageType = damageType;
    }

    public SchematicObject Schematic { get; }

    /// <summary>
    /// Gets or sets damage that will be applied on schematic.
    /// </summary>
    public float Damage { get; set; }

    /// <summary>
    /// Gets or sets damage type that is applied on schematic.
    /// </summary>
    public DamageType DamageType { get; set; }

    /// <summary>
    /// Gets or sets value, indicating will damage be applied on schematic or not.
    /// </summary>
    public bool IsAllowed { get; set; } = true;
}