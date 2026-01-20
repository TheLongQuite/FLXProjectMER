using Exiled.API.Enums;
using Exiled.API.Features;
using NorthwoodLib.Pools;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectMER.Features.Extensions;

public static class RoomExtensions
{
    /// <summary>
    /// Умно уничтожает комнату с камерами 079 или без
    /// </summary>
    /// <param name="room">Комната, которую удаляем</param>
    public static void DestroyRoom(this Room room)
    {
        foreach (Component? component in room.gameObject.GetComponentsInChildren<Component>())
        {
            try
            {
                if (component.name.Contains("SCP-079") || component.name.Contains("CCTV"))
                {
                    Log.Debug($"Prevent from destroying: {component.name} {component.tag} {component.GetType().FullName
                    }");

                    continue;
                }

                if (component.GetComponentsInParent<Component>()
                    .Any(c => c.name.Contains("SCP-079") || c.name.Contains("CCTV")))
                {
                    Log.Debug($"Prevent from destroying: {component.name} {component.tag} {component.GetType().FullName
                    }");

                    continue;
                }

                Log.Debug($"Destroying component: {component.name} {component.tag} {component.GetType().FullName}");

                Object.Destroy(component);
            }
            catch (Exception e)
            {
                Log.Debug($"catch error: {e}");
            }
        }
    }

    public static Room GetRoomAtPosition(Vector3 position)
    {
        Room room = Room.Get(position);
        return room ?? Room.List.First(x => x != null && x.Type == RoomType.Surface);
    }

    public static string GetRoomStringId(this Room room) => $"{room.Zone}_{room.RoomShape}_{room.Type}";

    public static int GetRoomIndex(this Room room)
    {
        List<Room> list = ListPool<Room>.Shared.Rent(Room.List.Where(x
            => x != null && x.Zone == room.Zone && x.RoomShape == room.RoomShape && x.Type == room.Type));

        int index = list.IndexOf(room);
        ListPool<Room>.Shared.Return(list);
        return index;
    }

    public static Vector3 GetAbsolutePosition(this Room? room, Vector3 position)
    {
        if (room is null || room.Type == RoomType.Surface)
            return position;

        return room.Transform.TransformPoint(position);
    }

    public static Quaternion GetAbsoluteRotation(this Room? room, Vector3 eulerAngles)
    {
        if (room is null || room.Type == RoomType.Surface)
            return Quaternion.Euler(eulerAngles);

        return room.Transform.rotation * Quaternion.Euler(eulerAngles);
    }
}