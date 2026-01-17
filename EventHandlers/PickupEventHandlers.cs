using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Objects;
using AmmoPickup = Exiled.API.Features.Pickups.AmmoPickup;
using FirearmPickup = Exiled.API.Features.Pickups.FirearmPickup;
using Item = Exiled.API.Features.Items.Item;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    internal static readonly Dictionary<ushort, SchematicObject> ButtonPickups = [];
    internal static readonly Dictionary<ushort, int> PickupUsesLeft = [];

    public void OnPlayerSearchingPickup(SearchingPickupEventArgs ev)
    {
        if (!ButtonPickups.TryGetValue(ev.Pickup.Serial, out SchematicObject schematic))
            return;

        ev.IsAllowed = false;
        Schematic.OnButtonInteracted(new(ev.Pickup, ev.Player, schematic));
    }

    public void OnPlayerPickingUpItem(PickingUpItemEventArgs ev)
    {
        if (ev.Pickup is AmmoPickup ammo)
        {
            if (!ev.Pickup.Transform.TryGetComponentInParent(out MapEditorObject _))
                return;

            if (!PickupUsesLeft.ContainsKey(ev.Pickup.Serial))
                return;

            if (--PickupUsesLeft[ev.Pickup.Serial] == 0)
            {
                PickupUsesLeft.Remove(ev.Pickup.Serial);
                return;
            }

            ev.IsAllowed = false;
            ev.Pickup.InUse = false;
            ev.Player.AddAmmo(ammo.AmmoType, ammo.Base.NetworkSavedAmmo);
            return;
        }

        if (!ev.Pickup.Transform.TryGetComponentInParent(out MapEditorObject _))
            return;

        if (!PickupUsesLeft.ContainsKey(ev.Pickup.Serial))
            return;

        if (--PickupUsesLeft[ev.Pickup.Serial] == 0)
        {
            PickupUsesLeft.Remove(ev.Pickup.Serial);
            return;
        }

        ev.IsAllowed = false;
        ev.Pickup.InUse = false;

        Item item = ev.Player.AddItem(ev.Pickup, ItemAddReason.PickedUp);
        if (ev.Pickup is not FirearmPickup firearmPickup || item is not Firearm firearmItem)
            return;

        firearmItem.Base.ApplyAttachmentsCode(firearmPickup.Attachments, false);
        if (firearmItem.Base.TryGetModule(out MagazineModule magazineModule))
        {
            magazineModule.MagazineInserted = true;
            magazineModule.AmmoStored = magazineModule.AmmoMax;
            magazineModule.ServerResyncData();
        }
        else if (firearmItem.Base.TryGetModule(out CylinderAmmoModule cylinderAmmoModule))
        {
            cylinderAmmoModule.ServerModifyAmmo(cylinderAmmoModule.AmmoMax);
            cylinderAmmoModule.ServerResync();
        }
    }
}