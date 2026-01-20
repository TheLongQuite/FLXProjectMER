using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Toys;
using InventorySystem.Items.Firearms.Attachments;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Lockers;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;
using Object = UnityEngine.Object;

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

    /// <summary>
    /// Spawns a workstation.
    /// </summary>
    /// <param name="workStation">The <see cref="SerializableWorkstation"/> to spawn.</param>
    /// <param name="forcedPosition">Used to force exact object position.</param>
    /// <param name="forcedRotation">Used to force exact object rotation.</param>
    /// <param name="forcedScale">Used to force exact object scale.</param>
    /// <returns>The spawned <see cref="Workstation"/>.</returns>
    public static Workstation? SpawnWorkstation(SerializableWorkstation workStation,
        Vector3? forcedPosition = null, Quaternion? forcedRotation = null, Vector3? forcedScale = null)
    {
        Room room = GetRandomRoom(workStation.RoomType);
        return room == null ? null
            : Workstation.Get(workStation.SpawnOrUpdateObject().GetComponent<WorkstationController>());
    }

    /// <summary>
    /// Spawns a ItemSpawnPoint.
    /// </summary>
    /// <param name="itemSpawnPoint">The <see cref="SerializableItemSpawnpoint"/> to spawn.</param>
    /// <param name="forcedPosition">Used to force exact object position.</param>
    /// <param name="forcedRotation">Used to force exact object rotation.</param>
    /// <param name="forcedScale">Used to force exact object scale.</param>
    /// <returns>The spawned <see cref="GameObject"/>.</returns>
    public static GameObject? SpawnItemSpawnPoint(SerializableItemSpawnpoint itemSpawnPoint,
        Vector3? forcedPosition = null, Quaternion? forcedRotation = null, Vector3? forcedScale = null)
    {
        Room room = GetRandomRoom(itemSpawnPoint.RoomType);
        return room == null ? null : itemSpawnPoint.SpawnOrUpdateObject();
    }

    /// <summary>
    /// Spawns a PlayerSpawnPoint.
    /// </summary>
    /// <param name="playerSpawnPoint">The <see cref="SerializablePlayerSpawnpoint"/> to spawn.</param>
    /// <param name="forcedPosition">Used to force exact object position.</param>
    /// <returns>The spawned <see cref="playerSpawnPoint"/>.</returns>
    public static GameObject? SpawnPlayerSpawnPoint(SerializablePlayerSpawnpoint playerSpawnPoint,
        Vector3? forcedPosition = null)
    {
        Room room = GetRandomRoom(playerSpawnPoint.RoomType);
        return room == null ? null : playerSpawnPoint.SpawnOrUpdateObject();
    }

    /// <summary>
    /// Spawns a RagdollSpawnPoint.
    /// </summary>
    /// <param name="ragdollSpawnPoint">The <see cref="SerializableRagdollSpawnPoint"/> to spawn.</param>
    /// <param name="forcedPosition">Used to force exact object position.</param>
    /// <param name="forcedRotation">Used to force exact object rotation.</param>
    /// <returns>The spawned <see cref="GameObject"/>.</returns>
    public static GameObject? SpawnRagdollSpawnPoint(SerializableRagdollSpawnPoint ragdollSpawnPoint,
        Vector3? forcedPosition = null, Quaternion? forcedRotation = null)
    {
        Room room = GetRandomRoom(ragdollSpawnPoint.RoomType);
        return room == null ? null : ragdollSpawnPoint.SpawnOrUpdateObject();
    }

    /// <summary>
    /// Spawns a ShootingTarget.
    /// </summary>
    /// <param name="shootingTarget">The <see cref="SerializableShootingTarget"/> to spawn.</param>
    /// <param name="forcedPosition">Used to force exact object position.</param>
    /// <param name="forcedRotation">Used to force exact object rotation.</param>
    /// <param name="forcedScale">Used to force exact object scale.</param>
    /// <returns>The spawned <see cref="ShootingTargetToy"/>.</returns>
    public static ShootingTargetToy? SpawnShootingTarget(SerializableShootingTarget shootingTarget,
        Vector3? forcedPosition = null, Quaternion? forcedRotation = null, Vector3? forcedScale = null)
    {
        Room room = GetRandomRoom(shootingTarget.RoomType);
        return room == null ? null
            : AdminToy.Get<ShootingTargetToy>(shootingTarget.SpawnOrUpdateObject().GetComponent<ShootingTarget>());
    }

    /// <summary>
    /// Spawns a <see cref="SerializablePrimitive"/>.
    /// </summary>
    /// <param name="primitiveObject">The <see cref="SerializablePrimitive"/> to spawn.</param>
    /// <param name="forcedPosition">Used to force exact object position.</param>
    /// <param name="forcedRotation">Used to force exact object rotation.</param>
    /// <param name="forcedScale">Used to force exact object scale.</param>
    /// <returns>The spawned <see cref="Primitive"/>.</returns>
    public static Primitive? SpawnPrimitive(SerializablePrimitive primitiveObject, Vector3? forcedPosition = null,
        Quaternion? forcedRotation = null, Vector3? forcedScale = null)
    {
        Room room = GetRandomRoom(primitiveObject.RoomType);
        return room == null ? null
            : AdminToy.Get<Primitive>(primitiveObject.SpawnOrUpdateObject().GetComponent<PrimitiveObjectToy>());
    }

    /// <summary>
    /// Spawns a <see cref="SerializableLight"/>.
    /// </summary>
    /// <param name="lightSourceObject">The <see cref="SerializableLight"/> to spawn.</param>
    /// <param name="forcedPosition">The specified position.</param>
    /// <returns>The spawned <see cref="Light"/>.</returns>
    public static Light? SpawnLightSource(SerializableLight lightSourceObject, Vector3? forcedPosition = null)
    {
        Room room = GetRandomRoom(lightSourceObject.RoomType);
        return room == null ? null
            : AdminToy.Get<Light>(lightSourceObject.SpawnOrUpdateObject().GetComponent<LightSourceToy>());
    }

    /// <summary>
    /// Spawns a Teleporter.
    /// </summary>
    /// <param name="teleport">The <see cref="SerializableTeleport"/> to spawn.</param>
    /// <returns>The spawned <see cref="MapEditorObject"/>.</returns>
    public static TeleportObject? SpawnTeleport(SerializableTeleport teleport)
    {
        Room room = GetRandomRoom(teleport.RoomType);
        return room == null ? null : teleport.SpawnOrUpdateObject()?.GetComponent<TeleportObject>();
    }

    public static Locker? SpawnLocker(SerializableLocker locker, Vector3? forcedPosition = null,
        Quaternion? forcedRotation = null, Vector3? forcedScale = null)
    {
        Room room = GetRandomRoom(locker.RoomType);
        return room == null ? null
            : Locker.Get(locker.SpawnOrUpdateObject()?.GetComponent<MapGeneration.Distributors.Locker>());
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