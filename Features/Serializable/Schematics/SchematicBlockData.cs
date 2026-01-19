using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Distributors;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Lockers;
using UnityEngine;
using LightSourceToy = AdminToys.LightSourceToy;
using Object = UnityEngine.Object;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using TextToy = AdminToys.TextToy;
using WaypointToy = AdminToys.WaypointToy;

namespace ProjectMER.Features.Serializable.Schematics;

public class SchematicBlockData
{
    public virtual string Name { get; set; }

    public virtual int ObjectId { get; set; }

    public virtual int ParentId { get; set; }

    public virtual string AnimatorName { get; set; }

    public virtual Vector3 Position { get; set; }

    public virtual Vector3 Rotation { get; set; }

    public virtual Vector3 Scale { get; set; }

    public virtual BlockType BlockType { get; set; }

    public virtual Dictionary<string, object> Properties { get; set; }

    public GameObject Create(SchematicObject schematicObject, Transform parentTransform)
    {
        GameObject gameObject = BlockType switch
        {
            BlockType.Empty => CreateEmpty(),
            BlockType.Locker => CreateLocker(),
            BlockType.Primitive => CreatePrimitive(),
            BlockType.Light => CreateLight(),
            BlockType.Pickup => CreatePickup(schematicObject),
            BlockType.Workstation => CreateWorkstation(),
            BlockType.Text => CreateText(),
            BlockType.Interactable => CreateInteractable(),
            BlockType.Waypoint => CreateWaypoint(),
            _ => CreateEmpty(true)
        };

        gameObject.name = Name;

        Transform transform = gameObject.transform;
        transform.SetParent(parentTransform);
        transform.SetLocalPositionAndRotation(Position, Quaternion.Euler(Rotation));

        transform.localScale = BlockType switch
        {
            BlockType.Empty when Scale == Vector3.zero => Vector3.one,
            BlockType.Waypoint => Scale * SerializableWaypoint.ScaleMultiplier,
            _ => Scale
        };

        if (gameObject.TryGetComponent(out AdminToyBase adminToyBase))
        {
            if (Properties != null && Properties.TryGetValue("Static", out object isStatic) &&
                Convert.ToBoolean(isStatic))
                adminToyBase.NetworkIsStatic = true;
            else
                adminToyBase.NetworkMovementSmoothing = 60;
        }

        return gameObject;
    }

    private GameObject CreateLocker()
    {
        LockerType lockerType = Properties.TryGetValue("LockerType", out object lockerTypeProperty)
            ? (LockerType)Convert.ToInt32(lockerTypeProperty)
            : LockerType.Unknown;

        Locker locker = Object.Instantiate(SerializableLocker.GetLockerObjectByType(lockerType));

        if (Properties.TryGetValue("ChambersSettings", out object chambersProperty) &&
            chambersProperty is List<object> chambersList)
            ApplyChambersSettings(locker, chambersList);

        if (Properties.TryGetValue("Loot", out object lootProperty) && lootProperty is List<object> lootList)
            ApplyLootSettings(locker, lootList);

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

        PrimitiveObjectToy primitive = GameObject.Instantiate(PrefabManager.PrimitiveObject);
        primitive.NetworkPrimitiveFlags = PrimitiveFlags.None;

        return primitive.gameObject;
    }

    private GameObject CreatePrimitive()
    {
        PrimitiveObjectToy primitive = GameObject.Instantiate(PrefabManager.PrimitiveObject);

        primitive.NetworkPrimitiveType = (PrimitiveType)Convert.ToInt32(Properties["PrimitiveType"]);
        primitive.NetworkMaterialColor = Properties["Color"].ToString().GetColorFromString();

        PrimitiveFlags primitiveFlags;
        if (Properties.TryGetValue("PrimitiveFlags", out object flags))
            primitiveFlags = (PrimitiveFlags)Convert.ToByte(flags);
        else
        {
            // Backward compatibility
            primitiveFlags = PrimitiveFlags.Visible;
            if (Scale.x >= 0f)
                primitiveFlags |= PrimitiveFlags.Collidable;
        }

        primitive.NetworkPrimitiveFlags = primitiveFlags;

        return primitive.gameObject;
    }

    private GameObject CreateLight()
    {
        LightSourceToy light = GameObject.Instantiate(PrefabManager.LightSource);

        light.NetworkLightType = Properties.TryGetValue("LightType", out object lightType)
            ? (LightType)Convert.ToInt32(lightType) : LightType.Point;

        light.NetworkLightColor = Properties["Color"].ToString().GetColorFromString();
        light.NetworkLightIntensity = Convert.ToSingle(Properties["Intensity"]);
        light.NetworkLightRange = Convert.ToSingle(Properties["Range"]);

        if (Properties.TryGetValue("Shadows", out object shadows))
        {
            // Backward compatibility
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

    private GameObject CreatePickup(SchematicObject schematicObject)
    {
        if (Properties.TryGetValue("Chance", out object property) &&
            UnityEngine.Random.Range(0, 101) > Convert.ToSingle(property))
            return new("Empty Pickup");

        Pickup pickup = Pickup.Create((ItemType)Convert.ToInt32(Properties["ItemType"]));
        pickup.Position = Vector3.zero;
        if (Properties.ContainsKey("Locked"))
            EventHandlers.EventHandlers.ButtonPickups.Add(pickup.Serial, schematicObject);

        return pickup.GameObject;
    }

    private GameObject CreateWorkstation()
    {
        WorkstationController workstation = GameObject.Instantiate(PrefabManager.Workstation);
        workstation.NetworkStatus = (byte)(Properties.TryGetValue("IsInteractable", out object isInteractable) &&
                                           Convert.ToBoolean(isInteractable) ? 0 : 4);

        return workstation.gameObject;
    }

    private GameObject CreateText()
    {
        TextToy text = GameObject.Instantiate(PrefabManager.Text);

        text.TextFormat = Convert.ToString(Properties["Text"]);
        text.DisplaySize = Properties["DisplaySize"].ToVector2() * 20f;

        return text.gameObject;
    }

    private GameObject CreateInteractable()
    {
        InvisibleInteractableToy interactable = GameObject.Instantiate(PrefabManager.Interactable);
        interactable.NetworkShape = (InvisibleInteractableToy.ColliderShape)Convert.ToInt32(Properties["Shape"]);
        interactable.NetworkInteractionDuration = Convert.ToSingle(Properties["InteractionDuration"]);
        interactable.NetworkIsLocked =
            Properties.TryGetValue("IsLocked", out object isLocked) && Convert.ToBoolean(isLocked);

        return interactable.gameObject;
    }

    private GameObject CreateWaypoint()
    {
        WaypointToy waypoint = GameObject.Instantiate(PrefabManager.Waypoint);
        waypoint.NetworkPriority = byte.MaxValue;

        return waypoint.gameObject;
    }
}