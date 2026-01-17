using Exiled.API.Features;

namespace ProjectMER.Features.Extensions;

public static class FileExtensions
{
    public static List<string> GetAllMaps()
    {
        List<string> mapsPaths = new List<string>();
        foreach (var mapsDir in Directory.GetDirectories(ProjectMER.MapsDir))
        {
            Log.Info($"Found dir: {mapsDir}");
            List<string> files = Directory.GetFiles(mapsDir).Where(x => x.EndsWith(".yml")).ToList();
            Log.Info($"Found files" +
                     $": {mapsDir}");
            mapsPaths.AddRange(Directory.GetFiles(mapsDir).Where(x => x.EndsWith(".yml")));
        }
        
        Log.Info($"[{nameof(GetAllMaps)}] Total maps count: {mapsPaths.Count}");
        return mapsPaths;
    }
}