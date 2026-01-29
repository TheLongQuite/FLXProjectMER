using Exiled.API.Features;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectMER.Features.Extensions;

namespace ProjectMER.Features.Converters;

public static class SchematicValidator
{
    private const int BlockTypeEmpty = 0;
    private const int BlockTypePrimitive = 1;
    private const int BlockTypeLight = 2;
    private const int BlockTypePickup = 3;
    private const int BlockTypeWorkstation = 4;
    private const int BlockTypeLocker = 7;

    private const string CustomButtonKeyPrefix = "custombuttonkey";
    private const string DefaultButtonKey = "default";

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
                    if (blockType == null)
                        continue;

                    JObject? properties = block["Properties"] as JObject;

                    bool blockModified = blockType switch
                    {
                        BlockTypeLocker => ConvertLockerBlock(properties),
                        BlockTypePickup => ConvertPickupBlock(properties),
                        BlockTypeLight => ConvertLightBlock(properties),
                        BlockTypePrimitive => ConvertPrimitiveBlock(properties, block),
                        BlockTypeWorkstation => ConvertWorkstationBlock(properties),
                        _ => false
                    };

                    if (blockModified)
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
            Log.Error($"Failed to validate schematic {jsonPath}: {e}");
            return false;
        }
    }

    /// <summary>
    /// Конвертирует Pickup блок из старого формата в новый.
    /// Обрабатывает CustomItem, кнопки и ItemType.
    /// </summary>
    private static bool ConvertPickupBlock(JObject? properties)
    {
        if (properties == null)
            return false;

        bool modified = false;

        string? customItem = properties["CustomItem"]?.Value<string>();
        string? oldItemType = properties["ItemType"]?.Value<string>();

        string? newItemType;
        string buttonId = DefaultButtonKey;
        if (!string.IsNullOrEmpty(customItem))
        {
            if (customItem.StartsWith(CustomButtonKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                buttonId = ExtractButtonId(customItem);
                newItemType = !string.IsNullOrEmpty(oldItemType) ? oldItemType : "Coin";

                properties["Locked"] = false;
                modified = true;
            }
            else
            {
                newItemType = customItem;
                modified = true;
            }

            properties.Remove("CustomItem");
        }
        else
            newItemType = oldItemType ?? "Coin";

        if (properties["ItemType"] != null && properties["ItemType"] is { Type: JTokenType.Integer })
        {
            int itemTypeInt = properties["ItemType"].Value<int>();
            newItemType = ((ItemType)itemTypeInt).ToString();
            modified = true;
        }

        properties["ItemType"] = newItemType;
        if (properties["ButtonId"] == null)
        {
            properties["ButtonId"] = buttonId;
            modified = true;
        }

        if (properties["Uses"] != null)
        {
            properties.Remove("Uses");
            modified = true;
        }

        if (properties["Attachements"] != null)
        {
            properties.Remove("Attachements");
            modified = true;
        }

        if (properties["Locked"] == null)
        {
            properties["Locked"] = false;
            modified = true;
        }

        if (properties["Chance"] == null)
        {
            properties["Chance"] = 100.0f;
            modified = true;
        }

        return modified;
    }

    private static string ExtractButtonId(string customItemValue)
    {
        string[] separators = { "_", ":", "-" };

        foreach (string separator in separators)
        {
            int separatorIndex = customItemValue.IndexOf(separator, CustomButtonKeyPrefix.Length,
                StringComparison.OrdinalIgnoreCase);

            if (separatorIndex > 0)
                return customItemValue.Substring(separatorIndex + 1);
        }

        if (customItemValue.Length > CustomButtonKeyPrefix.Length)
            return customItemValue.Substring(CustomButtonKeyPrefix.Length);

        return DefaultButtonKey;
    }

    private static bool ConvertLightBlock(JObject? properties)
    {
        if (properties == null)
            return false;

        bool modified = false;

        if (properties["Shadows"] != null && properties["ShadowType"] == null)
        {
            bool shadows = Convert.ToBoolean(properties["Shadows"].Value<object>());
            properties["ShadowType"] = shadows ? 2 : 0;
            properties.Remove("Shadows");
            modified = true;
        }

        if (properties["LightType"] == null)
        {
            properties["LightType"] = 2;
            modified = true;
        }

        if (properties["ShadowType"] == null)
        {
            properties["ShadowType"] = 0;
            modified = true;
        }

        if (properties["Shape"] == null)
        {
            properties["Shape"] = 0;
            modified = true;
        }

        if (properties["SpotAngle"] == null)
        {
            properties["SpotAngle"] = 30.0f;
            modified = true;
        }

        if (properties["InnerSpotAngle"] == null)
        {
            properties["InnerSpotAngle"] = 21.8f;
            modified = true;
        }

        if (properties["ShadowStrength"] == null)
        {
            properties["ShadowStrength"] = 1.0f;
            modified = true;
        }

        return modified;
    }

    private static bool ConvertPrimitiveBlock(JObject? properties, JToken block)
    {
        if (properties == null)
            return false;

        bool modified = false;

        if (properties["PrimitiveFlags"] != null)
            return false;

        JToken? scaleToken = block["Scale"];
        byte primitiveFlags = 1;

        if (scaleToken != null)
        {
            float scaleX = 1.0f;

            if (scaleToken.Type == JTokenType.Object)
                scaleX = scaleToken["x"]?.Value<float>() ?? 1.0f;
            else if (scaleToken.Type == JTokenType.String)
            {
                string scaleStr = scaleToken.Value<string>() ?? "";
                scaleX = ParseScaleX(scaleStr);
            }

            if (scaleX >= 0f)
                primitiveFlags |= 2;
        }
        else
            primitiveFlags |= 2;

        properties["PrimitiveFlags"] = primitiveFlags;
        modified = true;

        return modified;
    }

    private static float ParseScaleX(string scaleStr)
    {
        try
        {
            scaleStr = scaleStr.Trim('(', ')', ' ');
            string[] parts = scaleStr.Split(',');
            if (parts.Length >= 1)
                return float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
        }

        return 1.0f;
    }

    private static bool ConvertWorkstationBlock(JObject? properties)
    {
        if (properties == null)
            return false;

        if (properties["IsInteractable"] != null)
            return false;

        properties["IsInteractable"] = true;
        return true;
    }

    private static bool ConvertLockerBlock(JObject? properties)
    {
        if (properties == null)
            return false;

        if (properties["Chambers"] == null && properties["AllowedRoleTypes"] == null)
            return false;

        if (properties["ChambersSettings"] != null)
            return false;

        ConvertLockerProperties(properties);
        return true;
    }

    private static void ConvertLockerProperties(JObject properties)
    {
        JToken? oldChambers = properties["Chambers"];
        int keycardPermissions = 0;

        if (properties["KeycardPermissions"] != null)
            keycardPermissions = properties["KeycardPermissions"]!.Value<int>();

        Dictionary<int, uint> allItems = new();
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

                        if (allItems.TryGetValue((int)itemType, out uint existing))
                            allItems[(int)itemType] = Math.Max(existing, count);
                        else
                            allItems[(int)itemType] = count;
                    }
                }

                JObject chamberSetting = new()
                {
                    ["AcceptableItems"] = new JArray(acceptableItems.Cast<object>().ToArray()), ["IsOpen"] = false,
                    ["RequiredPermissions"] = keycardPermissions
                };

                chambersSettings.Add(chamberSetting);
            }
        }

        JArray lootArray = new();
        foreach (KeyValuePair<int, uint> kvp in allItems)
        {
            int count = (int)kvp.Value;
            JObject lootEntry = new()
            {
                ["TargetItem"] = kvp.Key, ["RemainingUses"] = count, ["MaxPerChamber"] = count,
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

        foreach (string jsonFile in FileExtensions.GetAllSchematicJsonFiles())
        {
            string fileName = Path.GetFileNameWithoutExtension(jsonFile);

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