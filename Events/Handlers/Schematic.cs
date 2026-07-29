using Exiled.Events.Features;
using ProjectMER.Events.Arguments;

namespace ProjectMER.Events.Handlers;

public static class Schematic
{
    public static Event<SchematicSpawningEventArgs> SchematicSpawning { get; set; } = new();

    public static Event<SchematicSpawnedEventArgs> SchematicSpawned { get; set; } = new();
    public static Event<SchematicDamagingEventArgs> SchematicDamaging { get; set; } = new();
    public static Event<ButtonInteractedEventArgs> ButtonInteracted { get; set; } = new();

    public static Event<SchematicDestroyedEventArgs> SchematicDestroyed { get; set; } = new();

    public static void OnSchematicSpawning(SchematicSpawningEventArgs ev) => SchematicSpawning.InvokeSafely(ev);

    public static void OnSchematicSpawned(SchematicSpawnedEventArgs ev) => SchematicSpawned.InvokeSafely(ev);

    public static void OnButtonInteracted(ButtonInteractedEventArgs ev) => ButtonInteracted.InvokeSafely(ev);

    public static void OnSchematicDestroyed(SchematicDestroyedEventArgs ev) => SchematicDestroyed.InvokeSafely(ev);

    public static void OnSchematicDamaging(SchematicDamagingEventArgs ev) => SchematicDamaging.InvokeSafely(ev);
}