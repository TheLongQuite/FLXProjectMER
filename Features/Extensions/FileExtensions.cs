using Exiled.API.Features;

namespace ProjectMER.Features.Extensions;

public static class FileExtensions
{
    public static List<string> GetAllMaps()
    {
        List<string> mapsPaths = new();
        foreach (string? mapsDir in Directory.GetDirectories(ProjectMER.MapsDir))
        {
            List<string> files = Directory.GetFiles(mapsDir).Where(x => x.EndsWith(".yml")).ToList();
            mapsPaths.AddRange(Directory.GetFiles(mapsDir).Where(x => x.EndsWith(".yml")));
        }

        Log.Info($"[{nameof(GetAllMaps)}] Total maps count: {mapsPaths.Count}");
        return mapsPaths;
    }

    /// <summary>
    /// Возвращает пути до всех файлов.json со схематиками
    /// </summary>
    public static List<string> GetAllSchematicDirectories()
    {
        List<string> schematicPaths = new();
        foreach (string? schematicDir in Directory.GetDirectories(ProjectMER.SchematicsDir))
            schematicPaths.AddRange(Directory.GetDirectories(schematicDir));

        Log.Debug($"[{nameof(GetAllSchematicDirectories)}] Total schematics count: {schematicPaths.Count}");

        return schematicPaths;
    }
}