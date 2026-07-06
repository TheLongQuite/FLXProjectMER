using Exiled.API.Enums;
using Exiled.API.Features;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectMER.Features.Converters;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Schematics;
using UnityEngine;
using Utf8Json;
using YamlDotNet.Core;

namespace ProjectMER.Features;

public static class MapUtils
{
    public const string UntitledMapName = "Untitled";

    public static MapSchematic UntitledMap => LoadedMaps.GetOrAdd(UntitledMapName, () => new(UntitledMapName));

    public static Dictionary<string, MapSchematic> LoadedMaps { get; private set; } = [];

    public static void SaveMap(string mapName)
    {
        if (mapName == UntitledMapName)
            throw new InvalidOperationException("This map name is reserved for internal use!");

        MapSchematic map;

        if (LoadedMaps.TryGetValue(mapName, out map))
            map.Merge(UntitledMap);
        else if (TryGetMapData(mapName, out map))
            map.Merge(UntitledMap);
        else
            map = new MapSchematic(mapName).Merge(UntitledMap);

        string path = Path.Combine(ProjectMER.MapsDir, "UNSORTED");
        if (!Directory.Exists(path))
        {
            Log.Warn($"Map saving is not enabled. To enable it, create directory named 'UNSORTED' in your maps directory");
            return;
        }

        path = Path.Combine(path, $"{map.Name}.yml");

        string serialized = YamlParser.Serializer.Serialize(map);

        File.WriteAllText(path, serialized);
        map.IsDirty = false;

        UnloadMap(UntitledMapName);
        LoadMap(mapName);
    }

    public static Vector3 GetRelativePosition(Vector3 position, Room room)
        => room.Type == RoomType.Surface ? position : room.Transform.TransformPoint(position);

    public static void LoadMap(string mapName)
    {
        Log.Info($"[LoadMap] Начинаю загрузку карты: {mapName}");
        
        try
        {
            MapSchematic map = GetMapData(mapName);
            Log.Debug($"[LoadMap] Данные карты получены: {mapName}");
            
            UnloadMap(mapName);
            Log.Debug($"[LoadMap] Предыдущая версия выгружена: {mapName}");
            
            map.Reload();
            Log.Debug($"[LoadMap] Карта перезагружена: {mapName}");

            LoadedMaps.Add(mapName, map);
            Log.Info($"[LoadMap] Карта успешно загружена: {mapName}, объектов: {map.SpawnedObjects.Count}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Sequence contains no matching element"))
        {
            Log.Error($"[LoadMap] Ошибка 'Sequence contains no matching element' при загрузке карты '{mapName}'");
            Log.Error($"[LoadMap] Stack trace: {ex.StackTrace}");
            Log.Error($"[LoadMap] Room.List.Count = {Room.List.Count()}");
            throw new InvalidOperationException($"Не удалось загрузить карту '{mapName}': возможно, раунд ещё не начался или комнаты не инициализированы.", ex);
        }
        catch (Exception ex)
        {
            Log.Error($"[LoadMap] Неожиданная ошибка при загрузке карты '{mapName}': {ex.GetType().Name}: {ex.Message}");
            Log.Error($"[LoadMap] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public static bool UnloadMap(string mapName)
    {
        if (!LoadedMaps.ContainsKey(mapName))
            return false;

        foreach (MapEditorObject mapEditorObject in LoadedMaps[mapName].SpawnedObjects)
            mapEditorObject.Destroy();

        LoadedMaps.Remove(mapName);
        return true;
    }

    public static bool TryGetMapData(string mapName, out MapSchematic mapSchematic)
    {
        try
        {
            mapSchematic = GetMapData(mapName);
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug($"[TryGetMapData] Не удалось получить данные карты '{mapName}': {ex.Message}");
            mapSchematic = null!;
            return false;
        }
    }

    public static MapSchematic GetMapData(string mapName)
    {
        MapSchematic map;

        string? foundPath = null;
        List<string> allMaps = FileExtensions.GetAllMaps();
        
        Log.Debug($"[GetMapData] Поиск карты '{mapName}' среди {allMaps.Count} файлов");
        
        foreach (string? mapFile in allMaps)
        {
            string name = Path.GetFileName(mapFile);
            if (name != $"{mapName}.yml")
                continue;

            foundPath = mapFile;
            break;
        }

        if (foundPath == null)
        {
            string error = $"Failed to load map data: File {mapName}.yml does not exist!";
            Log.Error($"[GetMapData] {error}");
            Log.Debug($"[GetMapData] Доступные карты: {string.Join(", ", allMaps.Select(Path.GetFileNameWithoutExtension))}");
            throw new FileNotFoundException(error);
        }

        Log.Debug($"[GetMapData] Найден файл: {foundPath}");

        try
        {
            map = YamlParser.Deserializer.Deserialize<MapSchematic>(File.ReadAllText(foundPath));
            map.Name = mapName;
            Log.Debug($"[GetMapData] Карта десериализована успешно");
        }
        catch (YamlException e)
        {
            string error = $"Failed to load map data: File {mapName}.yml has YAML errors!\n{e.ToString().Split('\n')[0]}";
            Log.Error($"[GetMapData] {error}");
            throw new YamlException(error);
        }

        return map;
    }

    public static bool TryGetSchematicDataByName(string schematicName, out SchematicObjectDataList data)
    {
        try
        {
            data = GetSchematicDataByName(schematicName);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[TryGetSchematicDataByName] Не удалось получить схематик '{schematicName}': {ex.Message}\n{ex.StackTrace}");
            data = null!;
            return false;
        }
    }

    public static SchematicObjectDataList GetSchematicDataByName(string schematicName)
    {
        SchematicObjectDataList data;
        string? schematicDirPath = null;

        Log.Debug($"[GetSchematicDataByName] Поиск схематика: {schematicName}");

        foreach (string ownerDirectory in Directory.GetDirectories(ProjectMER.SchematicsDir))
        {
            foreach (string? schematicDirectory in Directory.GetDirectories(ownerDirectory))
            {
                string name = Path.GetFileName(schematicDirectory);
                if (name is null || name != schematicName)
                    continue;

                schematicDirPath = schematicDirectory;
                break;
            }

            if (schematicDirPath != null)
                break;
        }

        if (schematicDirPath == null)
        {
            string error = $"Failed to load schematic data: Directory {schematicName} does not exist!";
            Log.Error($"[GetSchematicDataByName] {error}");
            throw new DirectoryNotFoundException(error);
        }

        string schematicJsonPath = Path.Combine(schematicDirPath, $"{schematicName}.json");
        string misplacedSchematicJsonPath = schematicDirPath + ".json";

        if (!Directory.Exists(schematicDirPath))
        {
            if (File.Exists(misplacedSchematicJsonPath))
            {
                Directory.CreateDirectory(schematicDirPath);
                File.Move(misplacedSchematicJsonPath, schematicJsonPath);
                return GetSchematicDataByName(schematicName);
            }

            string error = $"Failed to load schematic data: Directory {schematicName} does not exist!";
            Log.Error($"[GetSchematicDataByName] {error}");
            throw new DirectoryNotFoundException(error);
        }

        if (!File.Exists(schematicJsonPath))
        {
            if (File.Exists(misplacedSchematicJsonPath))
            {
                File.Move(misplacedSchematicJsonPath, schematicJsonPath);
                return GetSchematicDataByName(schematicName);
            }

            string error = $"Failed to load schematic data: File {schematicName}.json does not exist!";
            Log.Error($"[GetSchematicDataByName] {error}");
            throw new FileNotFoundException(error);
        }

        string jsonText = File.ReadAllText(schematicJsonPath);

        try
        {
            data = Utf8Json.JsonSerializer.Deserialize<SchematicObjectDataList>(jsonText);
            data.Path = schematicDirPath;
            Log.Debug($"[GetSchematicDataByName] Схематик загружен: {schematicName}");

            bool needsSave = false;
            JObject jsonObject = JObject.Parse(jsonText);
            JArray blocks = (JArray)jsonObject["Blocks"];
            
            if (blocks != null)
            {
                foreach (JObject block in blocks)
                {
                    if (block["Guid"] == null)
                    {
                        block["Guid"] = System.Guid.NewGuid().ToString("N");
                        needsSave = true;
                    }
                }
            }

            if (needsSave)
            {
                try
                {
                    File.WriteAllText(schematicJsonPath, jsonObject.ToString(Formatting.Indented));
                    Log.Debug($"[GetSchematicDataByName] Авто-апгрейд схематика '{schematicName}' с добавлением GUID.");
                }
                catch (Exception ex)
                {
                    Log.Error($"[GetSchematicDataByName] Ошибка сохранения авто-апгрейда для '{schematicName}': {ex.Message}");
                }
            }
        }
        catch (Utf8Json.JsonParsingException e)
        {
            string error = $"Failed to load schematic data: File {schematicName}.json has JSON errors!\n{e.ToString().Split('\n')[0]}";
            Log.Error($"[GetSchematicDataByName] {error}");
            throw new Utf8Json.JsonParsingException(error);
        }

        return data;
    }

    public static string[] GetAvailableSchematicNames() => FileExtensions.GetAllSchematicJsonFiles()
        .Select(Path.GetFileNameWithoutExtension).Where(x => !x.Contains('-')).ToArray();

    public static string GetColoredMapName(string mapName)
    {
        if (mapName == UntitledMapName)
            return $"<color=grey><b><i>{UntitledMapName}</i></b></color>";

        bool isDirty = false;
        if (LoadedMaps.TryGetValue(mapName, out MapSchematic mapSchematic))
            isDirty = mapSchematic.IsDirty;

        return isDirty ? $"<i>{GetColoredString(mapName)}</i>" : GetColoredString(mapName);
    }

    public static string GetColoredString(string s)
    {
        uint value = Math.Min((uint)s.GetHashCode() / 255, 16777215);
        string colorHex = value.ToString("X6");
        return $"<color=#{colorHex}><b>{s}</b></color>";
    }
}