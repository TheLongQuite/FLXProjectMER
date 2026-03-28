using HarmonyLib;
using Interactables.Interobjects;
using MapGeneration;
using MapGeneration.StaticHelpers;
using ProjectMER.Features.Objects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectMER.Patches;

[HarmonyPatch(typeof(RoomUtils), nameof(RoomUtils.TryRaycastRoom))]
public class TryRaycastRoomFix
{
    public static bool Prefix(Vector3 pos, Vector3 dir, ref bool __result, out RoomIdentifier room)
    {
        if (Physics.Raycast(new Ray(pos, dir), out RaycastHit hitInfo,
                15f, (int)RoomUtils.RoomDetectionMask) && (Object)hitInfo.collider != (Object)null)
        {
            Transform transform = hitInfo.collider.transform;

            if (transform.TryGetComponentInParent(out IRoomObject comp1) && comp1.OriginalRoom != null)
            {
                room = comp1.OriginalRoom;
                __result = true;
                return false;
            }

            if (transform.TryGetComponentInParent(out room))
            {
                __result = true;
                return false;
            }

            if (transform.TryGetComponentInParent(out MapEditorObject mapEditorObject) && mapEditorObject.CurrentRoom)
            {
                room = mapEditorObject.CurrentRoom.Identifier;
                __result = true;
                return false;
            }

            if (transform.TryGetComponentInParent(out ElevatorChamber comp2))
            {
                room = comp2.CurrentRoom;
                __result = (Object)room != (Object)null;
                return false;
            }
        }

        room = null;
        __result = false;
        return false;
    }
}