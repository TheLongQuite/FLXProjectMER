using Exiled.Events.Features;
using ProjectMER.Events.Arguments;

namespace ProjectMER.Events.Handlers;

public static class Schematic
{
    public static Event<SchematicSpawningEventArgs> SchematicSpawning { get; set; } = new();

    public static Event<SchematicSpawnedEventArgs> SchematicSpawned { get; set; } = new();

    public static Event<ButtonInteractedEventArgs> ButtonInteracted { get; set; } = new();

    public static Event<SchematicDestroyedEventArgs> SchematicDestroyed { get; set; } = new();

    internal static void OnSchematicSpawning(SchematicSpawningEventArgs ev) => SchematicSpawning.InvokeSafely(ev);

    internal static void OnSchematicSpawned(SchematicSpawnedEventArgs ev) => SchematicSpawned.InvokeSafely(ev);

    internal static void OnButtonInteracted(ButtonInteractedEventArgs ev) => ButtonInteracted.InvokeSafely(ev);

    internal static void OnSchematicDestroyed(SchematicDestroyedEventArgs ev) => SchematicDestroyed.InvokeSafely(ev);
}