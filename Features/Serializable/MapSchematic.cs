using System.Collections;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using NorthwoodLib.Pools;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Lockers;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;

namespace ProjectMER.Features.Serializable;

public class MapSchematic
{
    public MapSchematic()
    {
    }

    public MapSchematic(string mapName) => Name = mapName;

    public string Name;

    public bool IsDirty;

    private readonly record struct ListAccessor(Func<IList> GetList, Type ElementType);

    private ListAccessor[]? _listAccessors;

    private ListAccessor[] ListAccessors => _listAccessors ??=
    [
        new(() => Primitives, typeof(SerializablePrimitive)), new(() => LightSources, typeof(SerializableLight)),
        new(() => Doors, typeof(SerializableDoor)), new(() => WorkStations, typeof(SerializableWorkstation)),
        new(() => ItemSpawnPoints, typeof(SerializableItemSpawnpoint)),
        new(() => PlayerSpawnPoints, typeof(SerializablePlayerSpawnpoint)),
        new(() => Capybaras, typeof(SerializableCapybara)), new(() => Texts, typeof(SerializableText)),
        new(() => Interactables, typeof(SerializableInteractable)),
        new(() => Schematics, typeof(SerializableSchematic)),
        new(() => Scp079Cameras, typeof(SerializableScp079Camera)),
        new(() => ShootingTargets, typeof(SerializableShootingTarget)),
        new(() => Teleports, typeof(SerializableTeleport)), new(() => Lockers, typeof(SerializableLocker)),
        new(() => Waypoints, typeof(SerializableWaypoint)), new(() => RoomLights, typeof(SerializableRoomLight))
    ];

    public List<SerializableDoor> Doors { get; set; } = [];

    public List<SerializableWorkstation> WorkStations { get; set; } = [];

    public List<SerializableItemSpawnpoint> ItemSpawnPoints { get; set; } = [];

    public List<SerializablePlayerSpawnpoint> PlayerSpawnPoints { get; set; } = [];
    public List<SerializableRagdollSpawnPoint> RagdollSpawnPoints { get; set; } = [];
    public List<SerializableShootingTarget> ShootingTargets { get; set; } = [];

    public List<SerializablePrimitive> Primitives { get; set; } = [];

    public List<SerializableLight> LightSources { get; set; } = [];

    public List<SerializableRoomLight> RoomLights { get; set; } = [];
    public List<SerializableTeleport> Teleports { get; set; } = [];

    public List<SerializableLocker> Lockers { get; set; } = [];

    public List<SerializableSchematic> Schematics { get; set; } = [];
    public List<SerializableCapybara> Capybaras { get; set; } = [];

    public List<SerializableText> Texts { get; set; } = [];

    public List<SerializableInteractable> Interactables { get; set; } = [];

    public List<SerializableScp079Camera> Scp079Cameras { get; set; } = [];

    public List<SerializableWaypoint> Waypoints { get; set; } = [];

    public List<MapEditorObject> SpawnedObjects = [];

    public MapSchematic Merge(MapSchematic other)
    {
        Primitives.AddRange(other.Primitives);
        LightSources.AddRange(other.LightSources);
        Doors.AddRange(other.Doors);
        WorkStations.AddRange(other.WorkStations);
        ItemSpawnPoints.AddRange(other.ItemSpawnPoints);
        PlayerSpawnPoints.AddRange(other.PlayerSpawnPoints);
        Capybaras.AddRange(other.Capybaras);
        Texts.AddRange(other.Texts);
        Interactables.AddRange(other.Interactables);
        Schematics.AddRange(other.Schematics);
        Scp079Cameras.AddRange(other.Scp079Cameras);
        ShootingTargets.AddRange(other.ShootingTargets);
        Teleports.AddRange(other.Teleports);
        Lockers.AddRange(other.Lockers);
        Waypoints.AddRange(other.Waypoints);
        RoomLights.AddRange(other.RoomLights);
        RagdollSpawnPoints.AddRange(other.RagdollSpawnPoints);

        return this;
    }

    public void Reload()
    {
        foreach (MapEditorObject mapEditorObject in SpawnedObjects)
            mapEditorObject.Destroy();

        SpawnedObjects.Clear();

        LightSources.ForEach(SpawnObject);
        Primitives.ForEach(SpawnObject);

        Doors.ForEach(obj =>
        {
            Door? vanillaDoor = Door.Get(obj.ObjectId);
            if (vanillaDoor != null)
            {
                obj.SetupDoor(vanillaDoor.Base);
                return;
            }

            SpawnObject(obj);
        });

        WorkStations.ForEach(SpawnObject);
        PlayerSpawnPoints.ForEach(SpawnObject);
        ItemSpawnPoints.ForEach(SpawnObject);

        Capybaras.ForEach(SpawnObject);
        Texts.ForEach(SpawnObject);
        Interactables.ForEach(SpawnObject);

        Schematics.ForEach(SpawnObject);
        Scp079Cameras.ForEach(SpawnObject);
        ShootingTargets.ForEach(SpawnObject);
        Teleports.ForEach(SpawnObject);

        Lockers.ForEach(obj =>
        {
            obj._prevType = obj.LockerType;
            SpawnObject(obj);
        });

        RoomLights.ForEach(SpawnObject);
        Waypoints.ForEach(SpawnObject);
        RagdollSpawnPoints.ForEach(SpawnObject);
    }

    public void SpawnObject<T>(T serializableObject) where T : SerializableObject
    {
        List<Room> rooms = ListPool<Room>.Shared.Rent();
        rooms.AddRange(Room.Get(x => x.Type == serializableObject.RoomType));

        if (rooms.Count == 0)
        {
            Log.Warn($"[SpawnObject] Не найдено комнат для объекта {serializableObject.ObjectId} (Room: '{
                serializableObject.RoomType}')");

            return;
        }

        foreach (Room room in rooms)
        {
            if (serializableObject.Index >= 0 && serializableObject.Index != room.GetRoomIndex())
                continue;

            GameObject? gameObject = serializableObject.SpawnOrUpdateObject(room);
            if (gameObject == null)
            {
                Log.Warn($"[SpawnObject] SpawnOrUpdateObject вернул null для {serializableObject.ObjectId}");
                continue;
            }

            MapEditorObject mapEditorObject =
                gameObject.AddComponent<MapEditorObject>()
                    .Init(serializableObject, Name, serializableObject.ObjectId, room);

            SpawnedObjects.Add(mapEditorObject);
        }

        ListPool<Room>.Shared.Return(rooms);
    }

    public void DestroyObject(string id)
    {
        foreach (MapEditorObject mapEditorObject in SpawnedObjects.ToList())
        {
            if (mapEditorObject.Id != id)
                continue;

            SpawnedObjects.Remove(mapEditorObject);
            mapEditorObject.Destroy();
        }
    }

    public bool TryAddElement(SerializableObject obj)
    {
        Type objType = obj.GetType();
        foreach (ListAccessor accessor in ListAccessors)
        {
            if (!accessor.ElementType.IsAssignableFrom(objType))
                continue;

            IList? list = accessor.GetList();
            foreach (SerializableObject existing in list)
            {
                if (existing.ObjectId == obj.ObjectId)
                    return false;
            }

            list.Add(obj);
            IsDirty = true;
            return true;
        }

        return false;
    }

    public bool TryRemoveElement(string id)
    {
        foreach (ListAccessor accessor in ListAccessors)
        {
            IList? list = accessor.GetList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not SerializableObject obj || obj.ObjectId != id)
                    continue;

                list.RemoveAt(i);
                IsDirty = true;
                return true;
            }
        }

        return false;
    }
}