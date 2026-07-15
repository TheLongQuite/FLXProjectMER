using Exiled.API.Features;
using ProjectMER.Features.Converters;
using ProjectMER.Features.Extensions;

namespace ProjectMER.Commands.Map;

public static class ValidationManager
{
    private static int _total = 0;
    private static int _done = 0;
    private static string _current = "";

    public static string GetStatus()
    {
        if (_total == 0)
            return "Валидация не запущена.";

        double progress = _total > 0 ? (double)_done / _total * 100 : 0;
        return $"[VALIDATE] Прогресс: {_done}/{_total} ({progress:F1}%)\nТекущий: {_current}";
    }

    public static bool ValidateSpecificMap(string mapName, out string response)
    {
        _current = $"Карта: {mapName}";
        _total = 1;
        _done = 0;

        string? mapPath = FindMapPath(mapName);

        if (mapPath == null)
        {
            _total = 0;
            response = $"Карта '{mapName}' не найдена.";
            return false;
        }

        try
        {
            ValidationResult result = MapValidator.ValidateMap(mapName, mapPath);
            _done = 1;
            _total = 0;

            if (result.IsSuccess)
            {
                response = $"Карта '{mapName}' успешно провалидирована. Схематиков обработано: {
                    result.ConvertedSchematics.Count}";

                Log.Debug($"[VALIDATE] Карта {mapName} успешно обработана");
                return true;
            }

            // Добавляем путь в ответ
            response = $"Карта '{mapName}' провалидирована с ошибками:\nПуть: {mapPath}\n" +
                       string.Join("\n", result.Errors);

            foreach (string error in result.Errors)
                Log.Error($"[VALIDATE] {mapName}: {error}");

            // Логируем путь отдельной строкой для удобства
            Log.Error($"[VALIDATE] {mapName}: Путь к файлу: {mapPath}");

            return false;
        }
        catch (Exception e)
        {
            _total = 0;
            response = $"Ошибка при валидации карты '{mapName}': {e.Message}\nПуть: {mapPath}";
            Log.Error($"[VALIDATE] Ошибка при обработке карты {mapName}: {e.Message}");
            Log.Error($"[VALIDATE] {mapName}: Путь к файлу: {mapPath}");
            return false;
        }
    }

    private static string? FindMapPath(string mapName)
    {
        foreach (string mapPath in FileExtensions.GetAllMaps())
        {
            string fileName = Path.GetFileNameWithoutExtension(mapPath);

            if (string.Equals(fileName, mapName, StringComparison.OrdinalIgnoreCase))
                return mapPath;
        }

        return null;
    }

    public static void ValidateAllMaps()
    {
        string[] mapFiles = FileExtensions.GetAllMaps().ToArray();

        _total = mapFiles.Length;
        _done = 0;

        foreach (string mapPath in mapFiles)
        {
            string mapName = Path.GetFileNameWithoutExtension(mapPath);
            _current = $"Карта: {mapName}";

            try
            {
                ValidationResult result = MapValidator.ValidateMap(mapName, mapPath);

                if (result.IsSuccess)
                    Log.Debug($"[VALIDATE] Карта {mapName} успешно обработана");
                else
                {
                    foreach (string error in result.Errors)
                        Log.Error($"[VALIDATE] {mapName}: {error}");

                    // Добавляем путь к файлу при ошибке
                    Log.Error($"[VALIDATE] {mapName}: Путь к файлу: {mapPath}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"[VALIDATE] Ошибка при обработке карты {mapName}: {e.Message}");
                Log.Error($"[VALIDATE] {mapName}: Путь к файлу: {mapPath}");
            }

            _done++;
        }

        _total = 0;
    }

    public static void ValidateAllSchematics()
    {
        ValidationResult result = SchematicValidator.ValidateAllSchematics();

        Log.Debug($"[VALIDATE] Схематики: успешно {result.SuccessCount}, ошибок {result.FailedCount}");

        foreach (string error in result.Errors)
            Log.Error($"[VALIDATE] {error}");
    }

    public static void ValidateEverything()
    {
        Log.Debug("[VALIDATE] Начинаем полную валидацию...");
        ValidateAllMaps();
        ValidateAllSchematics();
        Log.Debug("[VALIDATE] Валидация завершена.");
    }
}