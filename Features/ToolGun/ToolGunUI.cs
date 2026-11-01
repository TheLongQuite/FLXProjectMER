using System.Reflection;
using System.Text;
using Exiled.API.Features;
using NorthwoodLib.Pools;
using ProjectMER.Features.Enums;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace ProjectMER.Features.ToolGun;

public static class ToolGunUI
{
    public static string GetHintHUD(Player player)
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        
        sb.Append("<font=\"LiberationSans SDF\"><size=50%>");

        if (ToolGunHandler.TryGetSelectedMapObject(player, out MapEditorObject mapEditorObject) &&
            mapEditorObject != null)
        {
            object instance = mapEditorObject.GetType().GetField("Base").GetValue(mapEditorObject);
            List<PropertyInfo> properties = instance.GetType().GetProperties().ToList();

            sb.Append($"MapName: {MapUtils.GetColoredMapName(mapEditorObject.MapName)}\n");
            sb.Append($"ID: {MapUtils.GetColoredString(mapEditorObject.Id)}");

            foreach (string property in properties.GetColoredProperties(instance))
                sb.Append('\n').Append(property);
        }

        if (player.CurrentItem.IsToolGun(out ToolGunItem toolGun))
        {
            if (mapEditorObject != null)
                sb.Append("\n\n");

            sb.Append(GetToolGunModeString(player, toolGun)).Append('\n');
            sb.Append($"{player.Position:F3}\n");
            sb.Append(GetRoomString(player));
        }

        sb.Append("</size></font>");

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private static string GetToolGunModeString(Player player, ToolGunItem toolGun)
    {
        if (toolGun.CreateMode)
        {
            string output;
            if (toolGun.SelectedObjectToSpawn == ToolGunObjectType.Schematic)
            {
                if (ServerSpecificSettingsSync.TryGetSettingOfUser(player.ReferenceHub, 0,
                        out SSDropdownSetting dropdownSetting) &&
                    dropdownSetting.TryGetSyncSelectionText(out string schematicName))
                    output = schematicName.ToUpper();
                else
                    output = "Please select schematic in options";
            }
            else
                output = toolGun.SelectedObjectToSpawn.ToString().ToUpper();

            return $"<color=green>CREATE</color>\n<color=yellow>{output}</color>";
        }

        string name = " ";
        if (ToolGunHandler.Raycast(player, out RaycastHit hit))
        {
            if (hit.transform.TryGetComponentInParent(out MapEditorObject mapEditorObject))
            {
                if (mapEditorObject is IndicatorObject indicatorObject)
                    mapEditorObject = IndicatorObject.Dictionary[indicatorObject];

                if (mapEditorObject.gameObject.TryGetComponent(out SchematicObject schematicObject))
                    name = schematicObject.Name.ToUpper();
                else
                    name = mapEditorObject.Base.ToString().Split('.').Last().Replace("Serializable", "").ToUpper();
            }
        }

        if (toolGun.DeleteMode)
            return $"<color=red>DELETE</color>\n<color=yellow>{name}</color>";

        if (toolGun.SelectMode)
            return $"<color=yellow>SELECT</color>\n<color=yellow>{name}</color>";

        return "\n ";
    }

    private static string GetRoomString(Player player)
    {
        Room room = RoomExtensions.GetRoomAtPosition(player.CameraTransform.transform.position);
        List<Room> list = ListPool<Room>.Shared.Rent(Room.List.Where(x => x != null &&
                                                                          x.Zone == room.Zone &&
                                                                          x.RoomShape == room.RoomShape &&
                                                                          x.Type == room.Type));

        string roomString;
        if (list.Count == 1)
            roomString = room.GetRoomStringId();
        else
            roomString = $"{room.GetRoomStringId()} ({list.IndexOf(room)}) ({list.Count})";

        ListPool<Room>.Shared.Return(list);
        return roomString;
    }
}