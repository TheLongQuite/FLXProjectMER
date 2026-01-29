using Exiled.Events.EventArgs.Interfaces;
using ProjectMER.Features.Serializable.Schematics;

namespace ProjectMER.Events.Arguments;

public class SchematicSpawningEventArgs : EventArgs, IDeniableEvent
{
    public SchematicSpawningEventArgs(SchematicObjectDataList data, string name, bool isEventBased)
    {
        Data = data;
        Name = name;
        IsAllowed = true;
        IsEventBased = isEventBased;
    }

    public SchematicObjectDataList Data { get; set; }

    public bool IsEventBased { get; }
    public string Name { get; }

    public bool IsAllowed { get; set; }
}