using ProjectMER.Events.Arguments.Interfaces;
using ProjectMER.Features.Objects;

namespace ProjectMER.Events.Arguments;

public class SchematicSpawnedEventArgs : EventArgs, ISchematicEvent
{
    public SchematicSpawnedEventArgs(SchematicObject schematic, string name, bool isEventBased)
    {
        Schematic = schematic;
        Name = name;
        IsEventBased = isEventBased;
    }

    public SchematicObject Schematic { get; }
    public bool IsEventBased { get; }
    public string Name { get; }
}