using Exiled.Events.EventArgs.Interfaces;
using ProjectMER.Features.Serializable.Schematics;

namespace ProjectMER.Events.Arguments;

public class SchematicSpawningEventArgs : EventArgs, IDeniableEvent
{
    public SchematicSpawningEventArgs(SchematicObjectDataList data, string name, bool shouldBeOptimized)
    {
        Data = data;
        Name = name;
        IsAllowed = true;
        ShouldBeOptimized = shouldBeOptimized;
    }

    public SchematicObjectDataList Data { get; set; }

    public bool ShouldBeOptimized { get; }
    public string Name { get; }

    public bool IsAllowed { get; set; }
}