using CommandSystem;
using Exiled.Permissions.Extensions;
using ProjectMER.Features;

namespace ProjectMER.Commands.Map;

public class Validate : ICommand
{
    public string Command => "validate";

    public string[] Aliases => ["v"];

    public string Description => "Validates and converts a map to the current format";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"mpr.{Command}"))
        {
            response = $"You don't have permission to execute this command. Required permission: mpr.{Command}";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "Usage: validate <map_name>\nThis will convert old locker formats and re-serialize the map.";
            return false;
        }

        string mapName = arguments.At(0);

        try
        {
            MapUtils.ValidateMap(mapName);
        }
        catch (Exception e)
        {
            response = $"Validation failed: {e.Message}";
            return false;
        }

        response = $"Map '{mapName}' has been validated and saved!\n";
        return true;
    }
}