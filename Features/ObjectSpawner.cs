using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Schematics;
using RelativePositioning;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ProjectMER.Features;

public static class ObjectSpawner
{
    public static PrimitiveObjectToy SpawnPrimitive(SerializablePrimitive serializablePrimitive)
    {
        GameObject gameObject = serializablePrimitive.SpawnOrUpdateObject();
        return gameObject.GetComponent<PrimitiveObjectToy>();
    }

    /// <summary>
    /// Spawns a door.
    /// </summary>
    /// <param name="door">The <see cref="SerializableDoor"/> which is used to spawn a door.</param>
    /// <returns>The spawned <see cref="Door"/>.</returns>
    public static Door? SpawnDoor(SerializableDoor door)
    {
        Room room = GetRandomRoom(door.RoomType);
        return room == null ? null : Door.Get(door.SpawnOrUpdateObject());
    }

    /// <summary>
    /// Gets a random <see cref="Room"/> from the <see cref="RoomType"/>.
    /// </summary>
    /// <param name="type">The <see cref="RoomType"/> from which the room should be chosen.</param>
    /// <returns>A random <see cref="Room"/> that has <see cref="Room.Type"/> of the argument.</returns>
    public static Room GetRandomRoom(RoomType type)
    {
        if (type == RoomType.Unknown)
            return null;

        return Room.Get(type);
    }

    public static SchematicObject SpawnSchematic(SerializableSchematic serializableSchematic, bool isOptimized = false,
        bool isStatic = true)
    {
        GameObject? gameObject = serializableSchematic.SpawnOrUpdateObject();
        if (gameObject == null)
            return null!;

        SchematicObject schematic = gameObject.GetComponent<SchematicObject>();
        schematic.ShouldBeOptimized = isOptimized;
        schematic.IsStatic = isStatic;
        return schematic;
    }

    public static SchematicObject SpawnSchematic(string schematicName, Vector3 position, bool isStatic = true)
        => SpawnSchematic(new() { SchematicName = schematicName, Position = position });

    public static SchematicObject SpawnSchematic(string schematicName, Vector3 position, Quaternion rotation,
        bool isStatic = true) => SpawnSchematic(new()
    {
        SchematicName = schematicName, Position = position, Rotation = rotation.eulerAngles
    });

    public static SchematicObject SpawnSchematic(string schematicName, Vector3 position, Vector3 eulerAngles,
        bool isStatic = true)
        => SpawnSchematic(new() { SchematicName = schematicName, Position = position, Rotation = eulerAngles });

    public static SchematicObject
        SpawnSchematic(string schematicName, Vector3 position, Quaternion rotation, Vector3 scale, bool isStatic = true)
        => SpawnSchematic(new()
        {
            SchematicName = schematicName, Position = position, Rotation = rotation.eulerAngles, Scale = scale
        });

    public static SchematicObject
        SpawnSchematic(string schematicName, Vector3 position, Vector3 eulerAngles, Vector3 scale, bool isStatic = true)
        => SpawnSchematic(new()
        {
            SchematicName = schematicName, Position = position, Rotation = eulerAngles, Scale = scale
        });

    //
    public static SchematicObject SpawnSchematicOptimized(string schematicName, Vector3 position)
        => SpawnSchematic(new() { SchematicName = schematicName, Position = position });

    public static SchematicObject SpawnSchematicOptimized(string schematicName, Vector3 position, Quaternion rotation)
        => SpawnSchematic(new()
        {
            SchematicName = schematicName, Position = position, Rotation = rotation.eulerAngles
        });

    public static SchematicObject SpawnSchematicOptimized(string schematicName, Vector3 position, Vector3 eulerAngles)
        => SpawnSchematic(new() { SchematicName = schematicName, Position = position, Rotation = eulerAngles });

    public static SchematicObject
        SpawnSchematicOptimized(string schematicName, Vector3 position, Quaternion rotation, Vector3 scale)
        => SpawnSchematic(new()
        {
            SchematicName = schematicName, Position = position, Rotation = rotation.eulerAngles, Scale = scale
        });

    public static SchematicObject
        SpawnSchematicOptimized(string schematicName, Vector3 position, Vector3 eulerAngles, Vector3 scale)
        => SpawnSchematic(new()
        {
            SchematicName = schematicName, Position = position, Rotation = eulerAngles, Scale = scale
        });

    public static bool TrySpawnSchematic(SerializableSchematic serializableSchematic, out SchematicObject schematic)
    {
        try
        {
            schematic = SpawnSchematic(serializableSchematic);
            return schematic != null;
        }
        catch (Exception)
        {
            schematic = null!;
            return false;
        }
    }

    public static bool TrySpawnSchematic(string schematicName, Vector3 position, out SchematicObject schematic)
        => TrySpawnSchematic(new() { SchematicName = schematicName, Position = position }, out schematic);

    public static bool TrySpawnSchematic(string schematicName, Vector3 position, Quaternion rotation,
        out SchematicObject schematic) => TrySpawnSchematic(
        new() { SchematicName = schematicName, Position = position, Rotation = rotation.eulerAngles },
        out schematic);

    public static bool TrySpawnSchematic(string schematicName, Vector3 position, Vector3 eulerAngles,
        out SchematicObject schematic) => TrySpawnSchematic(
        new() { SchematicName = schematicName, Position = position, Rotation = eulerAngles },
        out schematic);

    public static bool TrySpawnSchematic(string schematicName, Vector3 position, Quaternion rotation, Vector3 scale,
        out SchematicObject schematic)
        => TrySpawnSchematic(
            new()
            {
                SchematicName = schematicName, Position = position, Rotation = rotation.eulerAngles, Scale = scale
            }, out schematic);

    public static bool TrySpawnSchematic(string schematicName, Vector3 position, Vector3 eulerAngles, Vector3 scale,
        out SchematicObject schematic) => TrySpawnSchematic(
        new() { SchematicName = schematicName, Position = position, Rotation = eulerAngles, Scale = scale },
        out schematic);
}