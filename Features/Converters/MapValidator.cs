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
    public static MapSchematic ValidateAndConvert(string yamlContent, string mapName)
    {
        MapSchematicWithOldLockers oldMap;
        try
        {
            oldMap = Loader.Deserializer.Deserialize<MapSchematicWithOldLockers>(yamlContent);
        }
        catch (YamlException e)
        {
            throw new YamlException($"Failed to parse map: {e.Message}");
        }
        
        MapSchematic newMap = new(mapName)
        {
            Doors = oldMap.Doors,
            WorkStations = oldMap.WorkStations,
            ItemSpawnPoints = oldMap.ItemSpawnPoints,
            PlayerSpawnPoints = oldMap.PlayerSpawnPoints,
            RagdollSpawnPoints = oldMap.RagdollSpawnPoints,
            ShootingTargets = oldMap.ShootingTargets,
            Primitives = oldMap.Primitives,
            LightSources = oldMap.LightSources,
            RoomLights = oldMap.RoomLights,
            Teleports = oldMap.Teleports,
            Schematics = oldMap.Schematics,
            Capybaras = oldMap.Capybaras,
            Texts = oldMap.Texts,
            Interactables = oldMap.Interactables,
            Scp079Cameras = oldMap.Scp079Cameras,
            Waypoints = oldMap.Waypoints,
            Lockers = []
        };
        
        foreach (OldLockerFormat oldLocker in oldMap.Lockers)
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
            LockerType = oldLocker.LockerType,
            Position = oldLocker.Position,
            Rotation = oldLocker.Rotation,
            Scale = oldLocker.Scale,
            RoomType = oldLocker.RoomType,
            Index = oldLocker.Index,
            ObjectId = oldLocker.ObjectId,
            ChambersSettings = [],
            Loot = []
        };
        
        Dictionary<ItemType, uint> allItems = new();
        foreach (KeyValuePair<int, List<OldLockerItem>> chamberEntry in oldLocker.Chambers)
        {
            List<OldLockerItem> items = chamberEntry.Value;
            List<ItemType> acceptableItems = [];

            foreach (OldLockerItem item in items.Where(item => !string.IsNullOrEmpty(item.Item)))
            {
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
                AcceptableItems = acceptableItems,
                IsOpen = false,
                RequiredPermissions = oldLocker.KeycardPermissions
            };

            newLocker.ChambersSettings.Add(chamberSettings);
        }
        
        foreach (KeyValuePair<ItemType, uint> kvp in allItems)
        {
            int count = (int)kvp.Value;
            
            SerializableLockerLoot loot = new()
            {
                TargetItem = kvp.Key,
                RemainingUses = count,
                MaxPerChamber = count,
                ProbabilityPoints = count,
                MinPerChamber = count
            };

            newLocker.Loot.Add(loot);
        }

        return newLocker;
    }
}

public class MapSchematicWithOldLockers
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
    public string ObjectId { get; set; } = "Id";
    
    public Dictionary<int, List<OldLockerItem>> Chambers { get; set; }
    public DoorPermissionFlags KeycardPermissions { get; set; }
    
    public List<string> AllowedRoleTypes { get; set; }
    
    public bool ShuffleChambers { get; set; }
    
    public ushort OpenedChambers { get; set; }
    
    public bool InteractLock { get; set; }
    
    public float Chance { get; set; }
}

public class OldLockerItem
{
    public string Item { get; set; } = string.Empty;
    
    public uint Count { get; set; } = 1;
    
    public List<string> Attachments { get; set; }
    
    public int Chance { get; set; }
}