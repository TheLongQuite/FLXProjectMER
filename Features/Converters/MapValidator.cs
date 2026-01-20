using Exiled.API.Enums;
using Exiled.Loader;
using Interactables.Interobjects.DoorUtils;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Lockers;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;
using YamlDotNet.Core;

namespace ProjectMER.Features.Converters;

public static class MapValidator
{
    public static ValidationResult ValidateMap(string mapName, string mapPath)
    {
        ValidationResult result = new();

        string content = File.ReadAllText(mapPath);
        bool needsLockerConversion = content.Contains("chambers:") &&
                                     (content.Contains("allowed_role_types:") ||
                                      content.Contains("keycard_permissions:"));

        MapSchematic map;
        try
        {
            map = needsLockerConversion ?
                ConvertOldMapFormat(content, mapName) : YamlParser.Deserializer.Deserialize<MapSchematic>(content);

            map.Name = mapName;
        }
        catch (YamlException e)
        {
            result.FailedCount++;
            result.Errors.Add($"Map YAML error: {e.Message}");
            return result;
        }

        HashSet<string> processedSchematics = new();

        foreach (SerializableTeleport teleport in map.Teleports)
            teleport.AllowedRoles = ["all"];

        foreach (SerializableSchematic schematic in map.Schematics)
        {
            string schematicName = schematic.SchematicName;

            if (!processedSchematics.Add(schematicName))
                continue;

            if (!SchematicValidator.ValidateSchematicByName(schematicName))
                continue;

            result.SuccessCount++;
            result.ConvertedSchematics.Add(schematicName);
        }

        try
        {
            string serialized = YamlParser.Serializer.Serialize(map);
            File.WriteAllText(mapPath, serialized);
            result.SuccessCount++;
        }
        catch (Exception e)
        {
            result.FailedCount++;
            result.Errors.Add($"Failed to save map: {e.Message}");
        }

        return result;
    }

    private static MapSchematic ConvertOldMapFormat(string content, string mapName)
    {
        OldMapSchematic oldOldMap = YamlParser.Deserializer.Deserialize<OldMapSchematic>(content);

        MapSchematic newMap = new(mapName)
        {
            Doors = oldOldMap.Doors, WorkStations = oldOldMap.WorkStations,
            ItemSpawnPoints = oldOldMap.ItemSpawnPoints, PlayerSpawnPoints = oldOldMap.PlayerSpawnPoints,
            RagdollSpawnPoints = oldOldMap.RagdollSpawnPoints, ShootingTargets = oldOldMap.ShootingTargets,
            Primitives = oldOldMap.Primitives, LightSources = oldOldMap.LightSources,
            RoomLights = oldOldMap.RoomLights, Teleports = oldOldMap.Teleports, Schematics = oldOldMap.Schematics,
            Capybaras = oldOldMap.Capybaras, Texts = oldOldMap.Texts, Interactables = oldOldMap.Interactables,
            Scp079Cameras = oldOldMap.Scp079Cameras, Waypoints = oldOldMap.Waypoints, Lockers = []
        };

        foreach (OldLockerFormat oldLocker in oldOldMap.Lockers)
        {
            SerializableLocker converted = ConvertLocker(oldLocker);
            newMap.Lockers.Add(converted);
        }

        return newMap;
    }

    private static SerializableLocker ConvertLocker(OldLockerFormat oldLocker)
    {
        SerializableLocker newLocker = new()
        {
            LockerType = oldLocker.LockerType, Position = oldLocker.Position, Rotation = oldLocker.Rotation,
            Scale = oldLocker.Scale, RoomType = oldLocker.RoomType, Index = oldLocker.Index,
            ObjectId = Guid.NewGuid().ToString("N").Substring(0, 9), ChambersSettings = [], Loot = []
        };

        Dictionary<ItemType, uint> allItems = new();

        foreach (KeyValuePair<int, List<OldLockerItem>> chamberEntry in oldLocker.Chambers)
        {
            List<OldLockerItem> items = chamberEntry.Value;
            List<ItemType> acceptableItems = [];

            foreach (OldLockerItem item in items)
            {
                if (string.IsNullOrEmpty(item.Item))
                    continue;

                if (!Enum.TryParse(item.Item, out ItemType itemType))
                    continue;

                acceptableItems.Add(itemType);

                if (allItems.TryGetValue(itemType, out uint existing))
                    allItems[itemType] = Math.Max(existing, item.Count);
                else
                    allItems[itemType] = item.Count;
            }

            SerializableLockerChamber chamberSettings = new()
            {
                AcceptableItems = acceptableItems, IsOpen = false,
                RequiredPermissions = oldLocker.KeycardPermissions
            };

            newLocker.ChambersSettings.Add(chamberSettings);
        }

        foreach (KeyValuePair<ItemType, uint> kvp in allItems)
        {
            int count = (int)kvp.Value;

            SerializableLockerLoot loot = new()
            {
                TargetItem = kvp.Key, RemainingUses = count, MaxPerChamber = count, ProbabilityPoints = count,
                MinPerChamber = count
            };

            newLocker.Loot.Add(loot);
        }

        return newLocker;
    }
}

public class OldMapSchematic
{
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
    public List<SerializableSchematic> Schematics { get; set; } = [];
    public List<SerializableCapybara> Capybaras { get; set; } = [];
    public List<SerializableText> Texts { get; set; } = [];
    public List<SerializableInteractable> Interactables { get; set; } = [];
    public List<SerializableScp079Camera> Scp079Cameras { get; set; } = [];
    public List<SerializableWaypoint> Waypoints { get; set; } = [];
    public List<OldLockerFormat> Lockers { get; set; } = [];
}

public class OldLockerFormat
{
    public LockerType LockerType { get; set; }

    public Vector3 Position { get; set; }

    public Vector3 Rotation { get; set; }

    public Vector3 Scale { get; set; }

    public RoomType RoomType { get; set; }

    public int Index { get; set; } = -1;

    public Dictionary<int, List<OldLockerItem>>? Chambers { get; set; }

    public DoorPermissionFlags KeycardPermissions { get; set; }
}

public class OldLockerItem
{
    public string Item { get; set; } = string.Empty;

    public uint Count { get; set; } = 1;
}