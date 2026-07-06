using ProjectMER.Events.Arguments.Interfaces;
using ProjectMER.Features.Objects;

namespace ProjectMER.Events.Arguments;

public class SchematicSpawnedEventArgs : EventArgs, ISchematicEvent
{
    public SchematicSpawnedEventArgs(SchematicObject schematic, string name, bool shouldBeOptimized)
    {
        Schematic = schematic;
        Name = name;
        ShouldBeOptimized = shouldBeOptimized;
    }

    public SchematicObject Schematic { get; }
    public bool ShouldBeOptimized { get; }
    public string Name { get; }
}