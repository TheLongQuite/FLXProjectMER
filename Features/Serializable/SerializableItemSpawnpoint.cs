using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Pickups;
using MEC;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Interfaces;
using UnityEngine;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using Random = UnityEngine.Random;

namespace ProjectMER.Features.Serializable;

public class SerializableItemSpawnpoint : SerializableObject, IIndicatorDefinition
{
    public string ItemType { get; set; } = "KeycardJanitor";
    public string AttachmentsCode { get; set; } = "-1";
    public int SpawnChance { get; set; } = 100;
    public uint NumberOfItems { get; set; } = 1;
    public int NumberOfUses { get; set; } = 1;
    public bool UseGravity { get; set; } = true;
    public bool CanBePickedUp { get; set; } = true;
    public float Weight { get; set; } = -1;

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null,
        bool isForced = false, bool shouldBeOptimized = true)
    {
        if (!isForced && Random.Range(0, 101) > SpawnChance)
            return null;

        GameObject itemSpawnPoint = instance ?? new GameObject("ItemSpawnpoint");
        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;

        itemSpawnPoint.transform.SetPositionAndRotation(position, rotation);
        if (instance != null)
        {
            foreach (ItemPickupBase pickup in instance.GetComponentsInChildren<ItemPickupBase>())
            {
                EventHandlers.EventHandlers.PickupUsesLeft.Remove(pickup.Info.Serial);
                pickup.DestroySelf();
            }
        }

        if (uint.TryParse(ItemType, out uint customId) && CustomItem.TryGet(customId, out CustomItem ci))
        {
            for (int i = 0; i < NumberOfItems; i++)
            {
                Pickup pickup = ci.Spawn(position)!;
                pickup.Rotation = rotation;
                pickup.Base.transform.parent = itemSpawnPoint.transform;

                if (!UseGravity && pickup.Base.gameObject.TryGetComponent(out Rigidbody rb))
                    rb.isKinematic = true;

                if (!CanBePickedUp)
                    pickup.IsLocked = true;

                EventHandlers.EventHandlers.PickupUsesLeft.Add(pickup.Serial, NumberOfUses);
            }
        }
        else if (Enum.TryParse(ItemType, out ItemType parsedItem))
        {
            Log.Debug($"Spawning vanilla item {parsedItem} at {room?.Type ?? RoomType.Unknown}");
            for (int i = 0; i < NumberOfItems; i++)
            {
                Pickup pickup = Pickup.CreateAndSpawn(parsedItem, position, rotation);

                pickup.Scale = Scale;
                pickup.Base.transform.parent = itemSpawnPoint.transform;

                if (!UseGravity && pickup.Base.gameObject.TryGetComponent(out Rigidbody rb))
                    rb.isKinematic = true;

                if (!CanBePickedUp)
                    pickup.IsLocked = true;

                if (Weight != -1)
                    pickup.Weight = Weight;

                if (pickup is FirearmPickup firearmPickup)
                {
                    Timing.CallDelayed(0.01f, () =>
                    {
                        firearmPickup.Base.OnDistributed();
                        firearmPickup.Attachments = uint.TryParse(AttachmentsCode, out uint attachmentsCode)
                            ? attachmentsCode : AttachmentsUtils.GetRandomAttachmentsCode(firearmPickup.Type);

                        if (firearmPickup.Base.Template.TryGetModule(out MagazineModule magazineModule))
                            magazineModule.ServerResyncData();
                    });
                }
            }
        }
        else
            Log.Error($"Failed to parse item {ItemType} at {room?.Type ?? RoomType.Unknown}");

        return itemSpawnPoint.gameObject;
    }

    public GameObject SpawnOrUpdateIndicator(Room room, GameObject? instance = null)
    {
        PrimitiveObjectToy cube;

        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);

        if (instance == null)
        {
            cube = UnityEngine.Object.Instantiate(PrefabManager.PrimitiveObject);
            cube.NetworkPrimitiveType = PrimitiveType.Cube;
            cube.NetworkPrimitiveFlags = AdminToys.PrimitiveFlags.Visible;
            cube.NetworkMaterialColor = new(0f, 1f, 0f, 0.9f);
            cube.transform.localScale = Vector3.one * 0.25f;
        }
        else
            cube = instance.GetComponent<PrimitiveObjectToy>();

        cube.transform.SetPositionAndRotation(position, rotation);

        return cube.gameObject;
    }
}