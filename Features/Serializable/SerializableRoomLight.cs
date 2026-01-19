using Exiled.API.Enums;
using Exiled.API.Features;
using ProjectMER.Features.Extensions;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ProjectMER.Features.Serializable;

public class SerializableRoomLight : SerializableObject
{
    /// <summary>
    /// Gets or sets the <see cref="RoomLightSerializable"/>'s color.
    /// </summary>
    public string Color { get; set; } = "red";

    /// <summary>
    /// Gets or sets the <see cref="RoomLightSerializable"/>'s color shift speed.
    /// <para>If set to 0, the light won't shift at all (static light).</para>
    /// </summary>
    public float ShiftSpeed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="RoomLightSerializable"/> should only work.
    /// <para>This applies when the Alpha Warhead is activated only.</para>
    /// </summary>
    public bool OnlyWarheadLight { get; set; } = false;

    [YamlIgnore]
    public override Vector3 Position { get; set; }

    [YamlIgnore]
    public override Vector3 Rotation { get; set; }

    [YamlIgnore]
    public override Vector3 Scale { get; set; }

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null, bool isForced = false)
    {
        Color color = Color.GetColorFromString();
        if (room is null)
        {
            if (!Enum.TryParse(Room, out RoomType roomType))
            {
                Log.Error($"Invalid room type {Room}");
                return null;
            }
            room = Exiled.API.Features.Room.Get(roomType);
        }
        
        if (OnlyWarheadLight && (Warhead.IsInProgress || Warhead.IsDetonated))
        {
            return base.SpawnOrUpdateObject(room, instance);
        }
        
        room.Color = color;
       
        return base.SpawnOrUpdateObject(room, instance, isForced);
    }
}