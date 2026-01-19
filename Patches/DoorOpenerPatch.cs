using Exiled.API.Extensions;
using HarmonyLib;
using Interactables.Interobjects.DoorUtils;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;

namespace ProjectMER.Patches;

/// <summary>
/// Pathes the <see cref="DoorEventOpenerExtension.Trigger(DoorEventOpenerExtension.OpenerEventType)"/> to prevent selected doors from opening, when the Alpha Warhead is activated.
/// </summary>
[HarmonyPatch(typeof(DoorEventOpenerExtension), nameof(DoorEventOpenerExtension.Trigger))]
internal static class DoorOpenerPatch
{
    private static void Postfix(DoorEventOpenerExtension __instance,
        ref DoorEventOpenerExtension.OpenerEventType eventType)
    {
        if (!__instance.TargetDoor.TryGetComponent(out MapEditorObject doorObjectComponent) ||
            doorObjectComponent.Base is not SerializableDoor serializableDoor)
            return;

        if ((eventType != DoorEventOpenerExtension.OpenerEventType.DeconFinish ||
             serializableDoor.LockOnEvent.HasFlagFast(LockOnEvent.LightDecontaminated)) &&
            (eventType != DoorEventOpenerExtension.OpenerEventType.WarheadStart ||
             serializableDoor.LockOnEvent.HasFlagFast(LockOnEvent.WarheadDetonated)))
            return;

        __instance.TargetDoor.NetworkTargetState = false;
        __instance.TargetDoor.ServerChangeLock(eventType == DoorEventOpenerExtension.OpenerEventType.DeconFinish
            ? DoorLockReason.DecontLockdown
            : DoorLockReason.Warhead, false);
    }
}