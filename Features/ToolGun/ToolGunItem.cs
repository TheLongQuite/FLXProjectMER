using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.API.Features.Items;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Lockers;
using ProjectMER.Features.Serializable.Schematics;
using UserSettings.ServerSpecific;
using Firearm = InventorySystem.Items.Firearms.Firearm;

namespace ProjectMER.Features.ToolGun;

public class ToolGunItem
{
    public static Dictionary<ushort, ToolGunItem> ItemDictionary { get; private set; } = [];

    public static Dictionary<ToolGunObjectType, Type> TypesDictionary { get; private set; } = new()
    {
        { ToolGunObjectType.Primitive, typeof(SerializablePrimitive) },
        { ToolGunObjectType.Light, typeof(SerializableLight) },
        { ToolGunObjectType.Door, typeof(SerializableDoor) },
        { ToolGunObjectType.Workstation, typeof(SerializableWorkstation) },
        { ToolGunObjectType.ItemSpawnpoint, typeof(SerializableItemSpawnpoint) },
        { ToolGunObjectType.PlayerSpawnpoint, typeof(SerializablePlayerSpawnpoint) },
        { ToolGunObjectType.Capybara, typeof(SerializableCapybara) },
        { ToolGunObjectType.Text, typeof(SerializableText) },
        { ToolGunObjectType.Schematic, typeof(SerializableSchematic) },
        { ToolGunObjectType.ShootingTarget, typeof(SerializableShootingTarget) },
        { ToolGunObjectType.Locker, typeof(SerializableLocker) },
        { ToolGunObjectType.Teleport, typeof(SerializableTeleport) },
        { ToolGunObjectType.Interactable, typeof(SerializableInteractable) },
        { ToolGunObjectType.Waypoint, typeof(SerializableWaypoint) },
        { ToolGunObjectType.RagdollSpawnpoint, typeof(SerializableRagdollSpawnPoint) },
        { ToolGunObjectType.RoomLight, typeof(SerializableRoomLight) },
        { ToolGunObjectType.Sound, typeof(SerializableSound) },
        { ToolGunObjectType.CameraRedirect, typeof(SerializableCameraRedirect) }
    };

    private ToolGunObjectType _selectedObjectToSpawn;

    public ToolGunObjectType SelectedObjectToSpawn
    {
        get => _selectedObjectToSpawn;
        set
        {
            _selectedObjectToSpawn = value;
            if (TypesDictionary.Count <= (int)_selectedObjectToSpawn)
            {
                _selectedObjectToSpawn = 0;
                return;
            }

            if (_selectedObjectToSpawn < 0)
            {
                _selectedObjectToSpawn = (ToolGunObjectType)(TypesDictionary.Count - 1);
                return;
            }
        }
    }

    public static bool TryAdd(Player player)
    {
        Item? item = player.AddItem(ItemType.GunCOM18);
        if (item == null)
            return false;

        Firearm toolgun = (Firearm)item.Base;
        toolgun.ApplyAttachmentsCode(454, false);
        if (!toolgun.TryGetModules(out MagazineModule magazineModule, out AutomaticActionModule automaticActionModule))
        {
            Log.Error("Modules not found. This error should never occur.");
            return false;
        }

        magazineModule.AmmoStored = 0;
        magazineModule.ServerResyncData();

        automaticActionModule.Cocked = true;
        automaticActionModule.ServerResync();

        player.AddAmmo(AmmoType.Nato9, 1);

        ItemDictionary.Add(toolgun.ItemSerial, new(toolgun));
        return true;
    }

    public static bool Remove(Player player)
    {
        foreach (Item item in player.Items)
        {
            if (ItemDictionary.ContainsKey(item.Serial))
            {
                ItemDictionary.Remove(item.Serial);
                player.RemoveItem(item);
                return true;
            }
        }

        return false;
    }

    public bool CreateMode => Firearm.IsEmittingLight && !AdsModule.AdsTarget;
    public bool DeleteMode => !Firearm.IsEmittingLight && !AdsModule.AdsTarget;
    public bool SelectMode => Firearm.IsEmittingLight && AdsModule.AdsTarget;

    public void Shot(Player player)
    {
        if (CreateMode)
        {
            ToolGunHandler.CreateObject(player, SelectedObjectToSpawn);
            return;
        }

        if (ToolGunHandler.TryGetMapObject(player, out MapEditorObject mapEditorObject) && DeleteMode)
        {
            ToolGunHandler.DeleteObject(mapEditorObject);
            return;
        }

        if (SelectMode)
            ToolGunHandler.SelectObject(player, mapEditorObject);
    }

    private ToolGunItem(Firearm firearm)
    {
        Firearm = firearm;
        if (!firearm.TryGetModule(out AdsModule))
            throw new("Module not found. This error should never occur.");
    }

    private readonly Firearm Firearm;
    private readonly IAdsModule AdsModule;
}
