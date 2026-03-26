using HarmonyLib;
using Interactables.Interobjects;
using MapGeneration;
using MapGeneration.StaticHelpers;
using ProjectMER.Features.Objects;
using UnityEngine;
using Object = System.Object;

namespace ProjectMER.Patches;

[HarmonyPatch(typeof(RoomUtils), nameof(RoomUtils.TryRaycastRoom))]
public class TryRaycastRoomFix
{
    public static bool Prefix(Vector3 pos, Vector3 dir, ref bool __result, out RoomIdentifier room)
    {
        if (Physics.Raycast(new Ray(pos, dir), out RaycastHit hitInfo, 
                15f, (int) RoomUtils.RoomDetectionMask) && hitInfo.collider != (Object) null)
        {
            Transform transform = hitInfo.collider.transform;
            
            if (transform.TryGetComponentInParent(out IRoomObject comp1))
            {
                room = comp1.OriginalRoom;
                if (room != null)
                    return true;

                if (!transform.TryGetComponentInParent(out room))
                    return true;

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

            if (transform.TryGetComponentInParent(out ElevatorChamber comp))
            {
                room = comp.CurrentRoom;
                __result = room != (Object) null;
                return false;
            }
        }
        
        room = null;
        __result = false;
        return false;    
    }
}