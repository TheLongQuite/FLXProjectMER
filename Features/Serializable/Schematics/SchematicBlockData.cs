using AdminToys;
using AudioSystem;
using AudioSystem.Models;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Toys;
using Exiled.CustomItems.API.Features;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration;
using MapGeneration.Distributors;
using Newtonsoft.Json.Linq;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Lockers;
using ProjectMER.Features.Serializable.Utility;
using RelativePositioning;
using UnityEngine;
using CameraType = ProjectMER.Features.Enums.CameraType;
using LightSourceToy = AdminToys.LightSourceToy;
using Object = UnityEngine.Object;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using TextToy = AdminToys.TextToy;
using WaypointToy = AdminToys.WaypointToy;

namespace ProjectMER.Features.Serializable.Schematics;

public class SchematicBlockData
{
    private const string DefaultButtonKey = "default";

    public virtual string Name { get; set; }

    public virtual int ObjectId { get; set; }

    public virtual int ParentId { get; set; }

    public virtual string AnimatorName { get; set; }

    public virtual Vector3 Position { get; set; }

    public virtual Vector3 Rotation { get; set; }

    public virtual Vector3 Scale { get; set; }

    public virtual BlockType BlockType { get; set; }

    public virtual Dictionary<string, object> Properties { get; set; }

    public virtual string Guid { get; set; }

    public GameObject Create(SchematicObject schematicObject, Transform parentTransform)
    {
        GameObject gameObject;
        
        try
        {
            gameObject = BlockType switch
            {
                BlockType.Empty => CreateEmpty(),
                BlockType.Locker => CreateLocker(),
                BlockType.Primitive => CreatePrimitive(),
                BlockType.Light => CreateLight(),
                BlockType.Pickup => CreatePickup(),
                BlockType.Workstation => CreateWorkstation(),
                BlockType.Text => CreateText(),
                BlockType.InteractableToy => CreateInteractableToy(schematicObject),
                BlockType.Camera => CreateScp079Camera(schematicObject),
                BlockType.Teleport => CreateTeleport(),
                BlockType.Waypoint => CreateWaypoint(),
                BlockType.Sound => CreateSound(),
                _ => CreateEmpty(true)
            };
        }
        catch (Exception ex)
        {
            Log.Error($"[SchematicBlockData.Create] Ошибка создания блока '{Name}' типа {BlockType}: {ex.Message}");
            Log.Debug($"[SchematicBlockData.Create] Stack trace: {ex.StackTrace}");
            gameObject = CreateEmpty(true);
        }

        gameObject.name = Name;

        Transform transform = gameObject.transform;
        transform.SetParent(parentTransform);
        transform.SetLocalPositionAndRotation(Position, Quaternion.Euler(Rotation));

        transform.localScale = BlockType switch
        {
            BlockType.Empty when Scale == Vector3.zero => Vector3.one,
            _ => Scale
        };
        
        if (BlockType == BlockType.Waypoint)
        {
            gameObject.GetComponent<WaypointToy>().NetworkBoundsSize = Scale;
        }

        if (gameObject.TryGetComponent(out AdminToyBase adminToyBase))
        {
            if (Properties != null && Properties.TryGetValue("Static", out object isStatic) &&
                Convert.ToBoolean(isStatic))
                adminToyBase.NetworkIsStatic = true;
            else
                adminToyBase.NetworkMovementSmoothing = 60;
        }

        if (gameObject.TryGetComponent(out StructurePositionSync structurePositionSync))
        {
            structurePositionSync.Network_position = transform.position;
            structurePositionSync.Network_rotationY = (sbyte)Mathf.RoundToInt(transform.eulerAngles.y / 5.625f);
        }
        
        if (Properties != null && Properties.TryGetValue("FollowWaypoint", out object followObj) && Convert.ToBoolean(followObj))
            gameObject.AddComponent<TransformWaypointFollower>();

        return gameObject;
    }

    private GameObject CreateLocker()
    {
        LockerType lockerType = LockerType.Unknown;
    
        if (Properties != null && Properties.TryGetValue("LockerType", out object lockerTypeProperty))
        {
            try
            {
                lockerType = (LockerType)Convert.ToInt32(lockerTypeProperty);
            }
            catch (Exception ex)
            {
                Log.Error($"[CreateLocker] Ошибка парсинга LockerType '{lockerTypeProperty}': {ex.Message}");
            }
        }

        if (lockerType == LockerType.Unknown)
        {
            Log.Warn($"[CreateLocker] LockerType не указан или Unknown для блока '{Name}'. Использую Misc.");
            lockerType = LockerType.Misc;
        }

        Locker locker;
        try
        {
            locker = Object.Instantiate(SerializableLocker.GetLockerObjectByType(lockerType));
        }
        catch (Exception ex)
        {
            Log.Error($"[CreateLocker] Ошибка создания локера типа {lockerType}: {ex.Message}");
            locker = Object.Instantiate(PrefabManager.LockerMisc);
        }

        if (Properties != null)
        {
            if (Properties.TryGetValue("ChambersSettings", out object chambersProperty) &&
                chambersProperty is List<object> chambersList)
            {
                try
                {
                    ApplyChambersSettings(locker, chambersList);
                }
                catch (Exception ex)
                {
                    Log.Error($"[CreateLocker] Ошибка применения ChambersSettings: {ex.Message}");
                }
            }

            if (Properties.TryGetValue("Loot", out object lootProperty) && lootProperty is List<object> lootList)
            {
                try
                {
                    ApplyLootSettings(locker, lootList);
                }
                catch (Exception ex)
                {
                    Log.Error($"[CreateLocker] Ошибка применения Loot: {ex.Message}");
                }
            }
        }

        return locker.gameObject;
    }

    private void ApplyChambersSettings(Locker locker, List<object> chambersList)
    {
        for (int i = 0; i < chambersList.Count && i < locker.Chambers.Length; i++)
        {
            if (chambersList[i] is not Dictionary<string, object> chamberData)
                continue;

            LockerChamber chamber = locker.Chambers[i];

            if (chamberData.TryGetValue("IsOpen", out object isOpen))
                chamber.SetDoor(Convert.ToBoolean(isOpen), null);

            if (chamberData.TryGetValue("RequiredPermissions", out object permissions))
                chamber.RequiredPermissions = (DoorPermissionFlags)Convert.ToInt32(permissions);

            if (chamberData.TryGetValue("AcceptableItems", out object acceptableItems) &&
                acceptableItems is List<object> itemsList)
            {
                chamber.AcceptableItems = itemsList
                    .Select(item => (ItemType)Convert.ToInt32(item))
                    .ToArray();
            }
        }
    }

    private void ApplyLootSettings(Locker locker, List<object> lootList)
    {
        List<LockerLoot> lockerLootEntries = new();

        foreach (object lootEntry in lootList)
        {
            if (lootEntry is not Dictionary<string, object> lootData)
                continue;

            LockerLoot loot = new()
            {
                TargetItem = lootData.TryGetValue("TargetItem", out object targetItem)
                    ? (ItemType)Convert.ToInt32(targetItem)
                    : ItemType.None,
                RemainingUses = lootData.TryGetValue("RemainingUses", out object remainingUses)
                    ? Convert.ToInt32(remainingUses)
                    : 1,
                MaxPerChamber = lootData.TryGetValue("MaxPerChamber", out object maxPerChamber)
                    ? Convert.ToInt32(maxPerChamber)
                    : 1,
                ProbabilityPoints = lootData.TryGetValue("ProbabilityPoints", out object probabilityPoints)
                    ? Convert.ToInt32(probabilityPoints)
                    : 100,
                MinPerChamber = lootData.TryGetValue("MinPerChamber", out object minPerChamber)
                    ? Convert.ToInt32(minPerChamber)
                    : 1
            };

            lockerLootEntries.Add(loot);
        }

        if (lockerLootEntries.Count > 0)
            locker.Loot = lockerLootEntries.ToArray();
    }

    private GameObject CreateEmpty(bool fallback = false)
    {
        if (fallback)
            Log.Warn($"{BlockType} is not yet implemented. Object will be an empty GameObject instead.");

        PrimitiveObjectToy primitive = Object.Instantiate(PrefabManager.PrimitiveObject);
        primitive.NetworkPrimitiveFlags = PrimitiveFlags.None;

        return primitive.gameObject;
    }

    private GameObject CreatePrimitive()
    {
        PrimitiveObjectToy primitive = Object.Instantiate(PrefabManager.PrimitiveObject);

        primitive.NetworkPrimitiveType = (PrimitiveType)Convert.ToInt32(Properties["PrimitiveType"]);
        primitive.NetworkMaterialColor = Properties["Color"].ToString().GetColorFromString();

        PrimitiveFlags primitiveFlags;
        if (Properties.TryGetValue("PrimitiveFlags", out object flags))
            primitiveFlags = (PrimitiveFlags)Convert.ToByte(flags);
        else
        {
            primitiveFlags = PrimitiveFlags.Visible;
            if (Scale.x >= 0f)
                primitiveFlags |= PrimitiveFlags.Collidable;
        }

        primitive.NetworkPrimitiveFlags = primitiveFlags;

        return primitive.gameObject;
    }

    private GameObject CreateLight()
    {
        LightSourceToy light = Object.Instantiate(PrefabManager.LightSource);

        light.NetworkLightType = Properties.TryGetValue("LightType", out object lightType)
            ? (LightType)Convert.ToInt32(lightType) : LightType.Point;

        light.NetworkLightColor = Properties["Color"].ToString().GetColorFromString();
        light.NetworkLightIntensity = Convert.ToSingle(Properties["Intensity"]);
        light.NetworkLightRange = Convert.ToSingle(Properties["Range"]);

        if (Properties.TryGetValue("Shadows", out object shadows))
        {
            light.NetworkShadowType = Convert.ToBoolean(shadows) ? LightShadows.Soft : LightShadows.None;
        }
        else
        {
            light.NetworkShadowType = (LightShadows)Convert.ToInt32(Properties["ShadowType"]);
            light.NetworkLightShape = (LightShape)Convert.ToInt32(Properties["Shape"]);
            light.NetworkSpotAngle = Convert.ToSingle(Properties["SpotAngle"]);
            light.NetworkInnerSpotAngle = Convert.ToSingle(Properties["InnerSpotAngle"]);
            light.NetworkShadowStrength = Convert.ToSingle(Properties["ShadowStrength"]);
        }

        return light.gameObject;
    }

    private GameObject CreatePickup()
    {
        if (Properties.TryGetValue("Chance", out object? chanceProperty) &&
            UnityEngine.Random.Range(0, 101) > Convert.ToSingle(chanceProperty))
            return new("Empty Pickup");

        string itemTypeStr = Convert.ToString(Properties["ItemType"]);
        Pickup pickup;
        if (uint.TryParse(itemTypeStr, out uint customId) && CustomItem.TryGet(customId, out CustomItem customItem))
            pickup = customItem.Spawn(Vector3.zero)!;
        else if (Enum.TryParse(itemTypeStr, out ItemType itemType) && Enum.IsDefined(typeof(ItemType), itemType))
            pickup = Pickup.Create(itemType);
        else
        {
            Log.Error($"Предмета с айди [{itemTypeStr}] не существует");
            return new("Empty Pickup");
        }

        pickup.Position = Vector3.zero;

        if (Properties.TryGetValue("Locked", out object? lockedProperty))
            pickup.IsLocked = bool.TryParse(lockedProperty?.ToString(), out bool locked) && locked;

        string buttonId = Convert.ToString(Properties["ButtonId"]);
        if (buttonId != DefaultButtonKey)
        {
            if (pickup.IsLocked)
                Log.Error($"Pickup with button '{buttonId}' is locked. Buttons won't work on locked pickups.");

            EventHandlers.EventHandlers.ButtonPickups.Add(pickup.Serial, buttonId);
        }

        return pickup.GameObject;
    }

    private GameObject CreateWorkstation()
    {
        WorkstationController workstation = Object.Instantiate(PrefabManager.Workstation);
        workstation.NetworkStatus = (byte)(Properties.TryGetValue("IsInteractable", out object isInteractable) &&
                                           Convert.ToBoolean(isInteractable) ? 0 : 4);

        return workstation.gameObject;
    }

    private GameObject CreateText()
    {
        TextToy text = Object.Instantiate(PrefabManager.Text);

        text.TextFormat = Convert.ToString(Properties["Text"]);
        text.DisplaySize = Properties["DisplaySize"].ToVector2() * 20f;

        return text.gameObject;
    }

    private GameObject CreateScp079Camera(SchematicObject schematicObject)
    {
        CameraType cameraType = Properties.TryGetValue("CameraType", out object ct)
            ? (CameraType)Convert.ToInt32(ct) : CameraType.Lcz;

        Scp079CameraToy cameraToy = cameraType switch
        {
            CameraType.Hcz => Object.Instantiate(PrefabManager.CameraHcz),
            CameraType.Lcz => Object.Instantiate(PrefabManager.CameraLcz),
            CameraType.Ez => Object.Instantiate(PrefabManager.CameraEz),
            CameraType.Sz => Object.Instantiate(PrefabManager.CameraSz),
            CameraType.EzArm => Object.Instantiate(PrefabManager.CameraEzArm),
            _ => Object.Instantiate(PrefabManager.CameraLcz),
        };
        
        string label = Properties.TryGetValue("Label", out object lbl) ? Convert.ToString(lbl) : "REDIRECT";
        cameraToy.NetworkLabel = label;
        cameraToy.NetworkRoom = schematicObject.Room?.Identifier;

        string uniqueId = Properties.TryGetValue("UniqueId", out object uid) ? Convert.ToString(uid) : 
            System.Guid.NewGuid().ToString("N").Substring(0, 9);

        MapEditorObject mapEditorObject = cameraToy.gameObject.AddComponent<MapEditorObject>();
        mapEditorObject.Id = uniqueId;
        
        List<TargetTeleporter> targetCameras = new();
        List<Dictionary<string, object>> targetsData = "TargetCameras".GetListOfDicts(Properties);
    
        foreach (Dictionary<string, object> targetData in targetsData)
        {
            targetCameras.Add(new TargetTeleporter
            {
                Id = Convert.ToString(targetData["Id"]),
                Chance = targetData.TryGetValue("Chance", out object ch) ? Convert.ToInt32(ch) : 100,
            });
        }

        mapEditorObject.Base = new SerializableCameraRedirect
        {
            ObjectId = uniqueId,
            TargetCameras = targetCameras,
            Label = label,
            CameraType = cameraType
        };
        
        if (targetCameras.Count > 0)
        {
            CameraToy adminCamera = AdminToy.Get<CameraToy>(cameraToy);
            cameraToy.gameObject.AddComponent<CameraRedirectObject>().Init(targetCameras, adminCamera);
        }

        return cameraToy.gameObject;
    }
    
    private GameObject CreateInteractableToy(SchematicObject schematicObject)
    {
        InvisibleInteractableToy interactable = Object.Instantiate(PrefabManager.Interactable);

        InvisibleInteractableToy.ColliderShape shape = Properties.TryGetValue("Shape", out object sh)
            ? (InvisibleInteractableToy.ColliderShape)Convert.ToInt32(sh) : InvisibleInteractableToy.ColliderShape.Box;
        float duration = Properties.TryGetValue("InteractionDuration", out object dur)
            ? Convert.ToSingle(dur) : 0f;
        bool isLocked = Properties.TryGetValue("IsLocked", out object lk) && Convert.ToBoolean(lk);

        interactable.NetworkShape = shape;
        interactable.NetworkInteractionDuration = duration;
        interactable.NetworkIsLocked = isLocked;

        Log.Info($"Создаём InteractableToy в рамках схематика с именем: {schematicObject.Name}");
        if (!Properties.TryGetValue("VisualPrimitive", out object visObj) || visObj == null)
            return interactable.gameObject;

        Dictionary<string, object>? visProps = visObj switch
        {
            JObject jObj => jObj.ToObject<Dictionary<string, object>>(),
            Dictionary<string, object> dict => dict,
            _ => null
        };
        
        if (visProps == null)
        {
            Log.Warn($"[CreateInteractableToy] Failed to parse VisualPrimitive for {Name}");
            return interactable.gameObject;
        }

        PrimitiveObjectToy primitive = Object.Instantiate(PrefabManager.PrimitiveObject);
        primitive.transform.SetParent(interactable.transform,false);
        primitive.transform.localPosition = Vector3.zero;
        primitive.transform.localRotation = Quaternion.identity;
        primitive.transform.localScale = Vector3.one; 

        if (visProps.TryGetValue("PrimitiveType", out object pt))
            primitive.NetworkPrimitiveType = (PrimitiveType)Convert.ToInt32(pt);

        if (visProps.TryGetValue("Color", out object col))
            primitive.NetworkMaterialColor = col.ToString().GetColorFromString();
        
        if (visProps.TryGetValue("PrimitiveFlags", out object pf))
            primitive.NetworkPrimitiveFlags = (PrimitiveFlags)Convert.ToByte(pf);
        else
            primitive.NetworkPrimitiveFlags = PrimitiveFlags.Visible;
        
        return interactable.gameObject;
    }
    
    private GameObject CreateTeleport()
    {
        GameObject gameObject = new GameObject("Teleport");

        BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        string uniqueId = Properties.TryGetValue("UniqueId", out object uid) ? Convert.ToString(uid) 
            : System.Guid.NewGuid().ToString("N").Substring(0, 9);

        MapEditorObject mapEditorObject = gameObject.AddComponent<MapEditorObject>();
        mapEditorObject.Id = uniqueId;

        float cooldown = Properties.TryGetValue("Cooldown", out object cd) ? Convert.ToSingle(cd) : 5f;
        int teleportSoundId = Properties.TryGetValue("TeleportSoundId", out object sid) ? Convert.ToInt32(sid) : -1;
        TeleportFlags teleportFlags = Properties.TryGetValue("TeleportFlags", out object tf) ? (TeleportFlags)Convert.ToInt32(tf) : TeleportFlags.Player;
        LockOnEvent lockOnEvent = Properties.TryGetValue("LockOnEvent", out object loe) ? (LockOnEvent)Convert.ToInt32(loe) : LockOnEvent.None;

        List<string> allowedRoles = "AllowedRoles".GetStringList(Properties);

        List<TargetTeleporter> targetTeleporters = new();
        List<Dictionary<string, object>> targetsData = "TargetTeleporters".GetListOfDicts(Properties);
        foreach (Dictionary<string, object> targetData in targetsData)
        {
            targetTeleporters.Add(new TargetTeleporter
            {
                Id = Convert.ToString(targetData["Id"]),
                Chance = targetData.TryGetValue("Chance", out object ch) ? Convert.ToInt32(ch) : 100,
            });
        }

        TeleportObject teleportObject = gameObject.AddComponent<TeleportObject>();
        teleportObject.InitForSchematic(targetTeleporters, allowedRoles, cooldown, teleportSoundId, teleportFlags, lockOnEvent);

        return gameObject;

    }

    private GameObject CreateWaypoint()
    {
        WaypointToy waypoint = Object.Instantiate(PrefabManager.Waypoint);
        waypoint.NetworkPriority = byte.MaxValue;

        return waypoint.gameObject;
    }
    
    private GameObject CreateSound()
    {
        short id = Methods.PlayAudio(
            $"{Convert.ToString(Properties["SoundName"])}.ogg", Vector3.zero, 
            (float)Convert.ToDouble(Properties["Radius"]), 
            (float)Convert.ToDouble(Properties["MinRadius"]),
            Convert.ToByte(Properties["Volume"]), 
            "AUDIO", 
            Convert.ToBoolean(Properties["Loop"]));
        
        BetterAudioBase audio = Methods.GetAudio(id);
        audio.BroadcastTransform = audio.InternalPlayer.gameObject.transform;
        audio.BroadcastPosition = null;

        return audio.InternalPlayer.gameObject;
    }
}