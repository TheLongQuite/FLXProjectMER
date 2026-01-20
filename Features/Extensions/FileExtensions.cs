using Exiled.API.Features;

namespace ProjectMER.Features.Extensions;

public static class FileExtensions
{
    public static List<string> GetAllMaps()
    {
        List<string> mapsPaths = new();
        foreach (string? mapsDir in Directory.GetDirectories(ProjectMER.MapsDir))
            mapsPaths.AddRange(Directory.GetFiles(mapsDir).Where(x => x.EndsWith(".yml")));

        return mapsPaths;
    }
    
    public static List<string> GetAllSchematicDirectories()
    {
        List<string> schematicPaths = new();

        if (!Directory.Exists(ProjectMER.SchematicsDir))
            return schematicPaths;

        SearchSchematicDirectories(ProjectMER.SchematicsDir, schematicPaths);

        Log.Debug($"[{nameof(GetAllSchematicDirectories)}] Total schematics count: {schematicPaths.Count}");

        return schematicPaths;
    }

    private static void SearchSchematicDirectories(string rootDir, List<string> results)
    {
        foreach (string dir in Directory.GetDirectories(rootDir))
        {
            string dirName = Path.GetFileName(dir);
            string expectedJson = Path.Combine(dir, $"{dirName}.json");

            if (File.Exists(expectedJson))
                results.Add(dir);

            SearchSchematicDirectories(dir, results);
        }
    }
    
    public static List<string> GetAllSchematicJsonFiles()
    {
        List<string> jsonFiles = new();

        foreach (string schematicDir in GetAllSchematicDirectories())
        {
            string dirName = Path.GetFileName(schematicDir);
            string jsonPath = Path.Combine(schematicDir, $"{dirName}.json");

            if (File.Exists(jsonPath))
                jsonFiles.Add(jsonPath);
        }

        return jsonFiles;
    }
}