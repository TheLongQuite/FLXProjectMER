using Exiled.API.Enums;
using Exiled.API.Features;
using MapGeneration;
using NorthwoodLib.Pools;
using ProjectMER.Features.Serializable;
using UnityEngine;

namespace ProjectMER.Features.Extensions;

public static class RoomExtensions
{
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