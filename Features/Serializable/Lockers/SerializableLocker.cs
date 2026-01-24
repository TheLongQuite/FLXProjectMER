using Exiled.API.Enums;
using Exiled.API.Features;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using MEC;
using Mirror;
using ProjectMER.Features.Extensions;
using UnityEngine;
using LockerChamber = LabApi.Features.Wrappers.LockerChamber;

namespace ProjectMER.Features.Serializable.Lockers;

public class SerializableLocker : SerializableObject
{
    public LockerType LockerType { get; set; } = LockerType.Scp500Pedestal;
    public List<SerializableLockerChamber> ChambersSettings { get; set; } = [];
    public List<SerializableLockerLoot> Loot { get; set; } = [];

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null,
        bool isForced = false)
    {
        Locker locker = instance == null ? UnityEngine.Object.Instantiate(GetLockerObjectByType(LockerType))
            : instance.GetComponent<Locker>();

        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;

        locker.transform.SetPositionAndRotation(position, rotation);
        locker.transform.localScale = Scale;

        if (locker.TryGetComponent(out StructurePositionSync structurePositionSync))
        {
            structurePositionSync.Network_position = locker.transform.position;
            structurePositionSync.Network_rotationY =
                (sbyte)Mathf.RoundToInt(locker.transform.rotation.eulerAngles.y / 5.625f);
        }

        LabApi.Features.Wrappers.Locker labApiLocker = LabApi.Features.Wrappers.Locker.Get(locker);
        if (LockerType != _prevType)
            SetDefaultSettings(labApiLocker);

        labApiLocker.ClearLockerLoot();
        foreach (SerializableLockerLoot loot in Loot)
        {
            labApiLocker.AddLockerLoot(loot.TargetItem, loot.RemainingUses, loot.ProbabilityPoints, loot.MinPerChamber,
                loot.MaxPerChamber);
        }

        int i = 0;
        labApiLocker.ClearAllChambers();
        foreach (LockerChamber chamber in labApiLocker.Chambers)
        {
            if (i > ChambersSettings.Count - 1)
                break;

            chamber.AcceptableItems = ChambersSettings[i].AcceptableItems.ToArray();
            chamber.RequiredPermissions = ChambersSettings[i].RequiredPermissions;
            i++;
        }

        _prevType = LockerType;
        NetworkServer.UnSpawn(locker.gameObject);
        NetworkServer.Spawn(locker.gameObject);

        Timing.CallDelayed(0.25f, () =>
        {
            foreach (ItemPickupBase itemPickupBase in locker.GetComponentsInChildren<ItemPickupBase>())
            {
                if (itemPickupBase.TryGetComponent(out Rigidbody rigidbody))
                    rigidbody.isKinematic = false;
            }

            int i = 0;
            foreach (LockerChamber chamber in labApiLocker.Chambers)
            {
                if (i > ChambersSettings.Count - 1)
                    break;

                chamber.IsOpen = ChambersSettings[i].IsOpen;
                i++;
            }
        });

        return locker.gameObject;
    }

    private void SetDefaultSettings(LabApi.Features.Wrappers.Locker labApiLocker)
    {
        Loot.Clear();
        ChambersSettings.Clear();

        foreach (LockerLoot loot in labApiLocker.Loot)
        {
            Loot.Add(new(loot.TargetItem, loot.RemainingUses, loot.MaxPerChamber, loot.ProbabilityPoints,
                loot.MinPerChamber));
        }

        foreach (LockerChamber chamber in labApiLocker.Chambers)
            ChambersSettings.Add(new(chamber.AcceptableItems, chamber.IsOpen, chamber.RequiredPermissions));
    }

    public static Locker GetLockerObjectByType(LockerType lockerType)
    {
        Locker? prefab = lockerType switch
        {
            LockerType.Scp500Pedestal => PrefabManager.PedestalScp500,
            LockerType.LargeGun => PrefabManager.LockerLargeGun,
            LockerType.RifleRack => PrefabManager.LockerRifleRack,
            LockerType.Misc => PrefabManager.LockerMisc,
            LockerType.Medkit => PrefabManager.LockerRegularMedkit,
            LockerType.Adrenaline => PrefabManager.LockerAdrenalineMedkit,
            LockerType.Scp018Pedestal => PrefabManager.PedestalScp018,
            LockerType.Scp207Pedestal => PrefabManager.PedstalScp207,
            LockerType.Scp244Pedestal => PrefabManager.PedestalScp244,
            LockerType.Scp268Pedestal => PrefabManager.PedestalScp268,
            LockerType.Scp1853Pedestal => PrefabManager.PedstalScp1853,
            LockerType.Scp2176Pedestal => PrefabManager.PedestalScp2176,
            LockerType.Scp1576Pedestal => PrefabManager.PedestalScp1576,
            LockerType.AntiScp207Pedestal => PrefabManager.PedestalAntiScp207,
            LockerType.Scp1344Pedestal => PrefabManager.PedestalScp1344,
            LockerType.ScpPedestal => PrefabManager.PedestalScp500,
            LockerType.ExperimentalWeapon => PrefabManager.LockerExperimentalWeapon,
            LockerType.Unknown => null,
            _ => null
        };

        if (prefab == null)
        {
            Log.Error($"[GetLockerObjectByType] Неизвестный тип локера: {lockerType}. Использую Scp500Pedestal как fallback.");
            return PrefabManager.PedestalScp500;
        }

        return prefab;
    }

    public override bool RequiresReloading => true;

    public LockerType _prevType = LockerType.Unknown;
}