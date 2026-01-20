using CommandSystem;
using Exiled.Permissions.Extensions;
using ProjectMER.Features.Converters;
using ProjectMER.Features.Extensions;

namespace ProjectMER.Commands.Map;

public class Validate : ICommand
{
    public string Command => "validate";

    public string[] Aliases => ["v"];

    public string Description => "Validates maps and/or schematics, converting old formats";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission("mpr.validate"))
        {
            response = "You don't have permission to execute this command.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "Usage:\n" +
                       "  validate <map_name> - Validate map + its schematics\n" +
                       "  validate --all - Validate everything";

            return false;
        }

        string arg = arguments.At(0);

        try
        {
            return arg switch
            {
                "--all" or "-a" => ValidateAll(out response),
                _ => ValidateSingleMap(arg, out response)
            };
        }
        catch (Exception e)
        {
            response = $"Validation failed: {e.Message}";
            return false;
        }
    }

    private bool ValidateSingleMap(string mapName, out string response)
    {
        string? foundPath = null;
        foreach (string mapFile in FileExtensions.GetAllMaps())
        {
            if (Path.GetFileName(mapFile) == $"{mapName}.yml")
            {
                foundPath = mapFile;
                break;
            }
        }

        if (foundPath == null)
        {
            response = $"Map '{mapName}' not found";
            return false;
        }

        ValidationResult result = MapValidator.ValidateMap(mapName, foundPath);
        if (result.IsSuccess)
        {
            response = $"Map '{mapName}' validated successfully!\n" +
                       $"Schematics processed: {result.ConvertedSchematics.Count}";

            if (result.ConvertedSchematics.Count > 0)
                response += $"\n- {string.Join("\n- ", result.ConvertedSchematics)}";

            return true;
        }

        response = $"Map '{mapName}' validation completed with errors:\n" +
                   string.Join("\n", result.Errors);

        return false;
    }

    private bool ValidateAll(out string response)
    {
        ValidationResult mapsResult = MapValidator.ValidateAllMaps();
        ValidationResult schematicsResult = SchematicValidator.ValidateAllSchematics();

        int totalMaps = mapsResult.SuccessCount + mapsResult.FailedCount;
        int totalSchematics = schematicsResult.SuccessCount + schematicsResult.FailedCount;

        response = $"Full validation complete:\n" +
                   $"Maps: {mapsResult.SuccessCount}/{totalMaps} success\n" +
                   $"Schematics: {schematicsResult.SuccessCount}/{totalSchematics} success\n" +
                   $"Schematics from maps: {mapsResult.ConvertedSchematics.Count}";

        List<string> allErrors = mapsResult.Errors.Concat(schematicsResult.Errors).ToList();
        if (allErrors.Count <= 0)
            return mapsResult.IsSuccess && schematicsResult.IsSuccess;

        response += "\n\nErrors:";
        response += allErrors.Aggregate(response, (current, error) => current + $"\n- {error}");
        return mapsResult.IsSuccess && schematicsResult.IsSuccess;
    }
}