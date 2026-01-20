using Exiled.API.Features;
using Exiled.Loader;
using Interactables.Interobjects.DoorUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectMER.Features.Extensions;

namespace ProjectMER.Features.Converters;

public static class SchematicValidator
{
    private const int LockerBlockType = 7;

    public static bool ValidateSchematicByName(string schematicName)
    {
        string? schematicPath = FindSchematicPath(schematicName);

        if (schematicPath == null)
            return false;
        
        return ValidateAndConvert(schematicPath, out _);
    }

    public static string? FindSchematicPath(string schematicName)
    {
        foreach (string schematicDir in FileExtensions.GetAllSchematicDirectories())
        {
            string dirName = Path.GetFileName(schematicDir);

            if (!string.Equals(dirName, schematicName, StringComparison.OrdinalIgnoreCase))
                continue;

            string jsonPath = Path.Combine(schematicDir, $"{schematicName}.json");

            if (File.Exists(jsonPath))
                return jsonPath;
        }

        return null;
    }

    public static bool ValidateAndConvert(string jsonPath, out string? response)
    {
        response = null;

        try
        {
            string content = File.ReadAllText(jsonPath);
            JObject root = JObject.Parse(content);
            bool modified = false;

            if (root["Blocks"] is JArray blocks)
            {
                foreach (JToken block in blocks)
                {
                    if (block == null)
                        continue;

                    int? blockType = block["BlockType"]?.Value<int>();
                    if (blockType != LockerBlockType)
                        continue;

                    JToken? properties = block["Properties"];
                    if (properties == null)
                        continue;

                    if (properties["Chambers"] == null || properties["AllowedRoleTypes"] == null)
                        continue;

                    ConvertLockerProperties((JObject)properties);
                    modified = true;
                }
            }

            if (!modified)
                return true;

            string newContent = root.ToString(Formatting.Indented);
            File.WriteAllText(jsonPath, newContent);
            Log.Info($"Schematic converted: {Path.GetFileName(jsonPath)}");
            return true;
        }
        catch (Exception e)
        {
            response = e.Message;
            return false;
        }
    }

    private static void ConvertLockerProperties(JObject properties)
    {
        JToken? oldChambers = properties["Chambers"];
        DoorPermissionFlags keycardPermissions = DoorPermissionFlags.None;

        if (properties["KeycardPermissions"] != null)
            keycardPermissions = (DoorPermissionFlags)properties["KeycardPermissions"]!.Value<int>();

        Dictionary<ItemType, uint> allItems = new();
        JArray chambersSettings = new();

        if (oldChambers is JObject chambersObj)
        {
            List<int> sortedKeys = chambersObj.Properties()
                .Select(p => int.Parse(p.Name))
                .OrderBy(k => k)
                .ToList();

            foreach (int key in sortedKeys)
            {
                JToken? chamberItems = chambersObj[key.ToString()];
                List<int> acceptableItems = new();

                if (chamberItems is JArray itemsArray)
                {
                    foreach (JToken itemToken in itemsArray)
                    {
                        if (itemToken == null)
                            continue;

                        string? itemName = itemToken["Item"]?.Value<string>();
                        if (string.IsNullOrEmpty(itemName))
                            continue;

                        if (!Enum.TryParse<ItemType>(itemName, out ItemType itemType))
                            continue;

                        acceptableItems.Add((int)itemType);

                        uint count = itemToken["Count"]?.Value<uint>() ?? 1;

                        if (allItems.TryGetValue(itemType, out uint existing))
                            allItems[itemType] = Math.Max(existing, count);
                        else
                            allItems[itemType] = count;
                    }
                }

                JObject chamberSetting = new()
                {
                    ["AcceptableItems"] = new JArray(acceptableItems.Cast<object>().ToArray()), ["IsOpen"] = false,
                    ["RequiredPermissions"] = (int)keycardPermissions
                };

                chambersSettings.Add(chamberSetting);
            }
        }

        JArray lootArray = new();
        foreach (KeyValuePair<ItemType, uint> kvp in allItems)
        {
            int count = (int)kvp.Value;
            JObject lootEntry = new()
            {
                ["TargetItem"] = (int)kvp.Key, ["RemainingUses"] = count, ["MaxPerChamber"] = count,
                ["ProbabilityPoints"] = count, ["MinPerChamber"] = count
            };

            lootArray.Add(lootEntry);
        }

        properties.Remove("Chambers");
        properties.Remove("AllowedRoleTypes");
        properties.Remove("ShuffleChambers");
        properties.Remove("KeycardPermissions");
        properties.Remove("OpenedChambers");
        properties.Remove("InteractLock");
        properties.Remove("Chance");

        properties["ChambersSettings"] = chambersSettings;
        properties["Loot"] = lootArray;
    }

    public static ValidationResult ValidateAllSchematics()
    {
        ValidationResult result = new();

        if (!Directory.Exists(ProjectMER.SchematicsDir))
        {
            result.Errors.Add($"Directory not found: {ProjectMER.SchematicsDir}");
            return result;
        }

        // Используем новый метод для получения всех JSON файлов
        foreach (string jsonFile in FileExtensions.GetAllSchematicJsonFiles())
        {
            string fileName = Path.GetFileNameWithoutExtension(jsonFile);

            // Пропускаем служебные файлы (например compiled-xxx.json)
            if (fileName.Contains('-'))
                continue;

            if (ValidateAndConvert(jsonFile, out string? error))
            {
                result.SuccessCount++;
                result.ConvertedSchematics.Add(fileName);
            }
            else
            {
                result.FailedCount++;
                result.Errors.Add($"{fileName}: {error}");
            }
        }

        return result;
    }
}

public class ValidationResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> ConvertedSchematics { get; set; } = new();
    public bool IsSuccess => FailedCount == 0;
}