using System.Text;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using NorthwoodLib.Pools;
using ProjectMER.Features;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Serializable;
using Utils.NonAllocLINQ;

namespace ProjectMER.Commands.Utility;

/// <summary>
/// Command used for listing all saved maps and schematics.
/// </summary>
public class List : ICommand
{
    /// <inheritdoc/>
    public string Command => "list";

    /// <inheritdoc/>
    public string[] Aliases { get; } = ["li", "ls"];

    /// <inheritdoc/>
    public string Description => "Shows the list of all available maps.";

    /// <inheritdoc/>
    public bool SanitizeResponse => false;

    /// <inheritdoc/>
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"mpr.{Command}"))
        {
            response = $"You don't have permission to execute this command. Required permission: mpr.{Command}";
            return false;
        }

        StringBuilder builder = StringBuilderPool.Shared.Rent();

        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("<color=green><b>Список карт:</b></color>");

        List<MapStatus> mapStatuses = ListPool<MapStatus>.Shared.Rent();

        foreach (string filePath in FileExtensions.GetAllMaps())
        {
            string owner = Directory.GetParent(filePath)?.Name ?? "NO OWNER";
            mapStatuses.AddIfNotContains(new(Path.GetFileNameWithoutExtension(filePath), owner));   
        }

        foreach (string loaderMapName in MapUtils.LoadedMaps.Keys)
        {
            string owner = Directory.GetParent(loaderMapName)?.Name ?? "NO OWNER";
            mapStatuses.AddIfNotContains(new(Path.GetFileNameWithoutExtension(loaderMapName), owner));   
        }

        foreach (var kvp in mapStatuses.OrderByDescending(x => x.IsLoaded).ThenByDescending(x => x.IsDirty).GroupBy(x => x.Owner))
        {
            builder.AppendLine($"- <color=yellow>{kvp.Key}</color>");
            foreach (var mapStatus in kvp) 
                builder.AppendLine($"- {mapStatus}");
        }

        ListPool<MapStatus>.Shared.Return(mapStatuses);

        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("<color=orange><b>Список схематик:</b></color>");

        foreach (var kvp in GroupByOwnerSchematics(FileExtensions.GetAllSchematicDirectories(), true))
        {
            builder.AppendLine($"- <color=yellow>{kvp.Key}</color>");
            foreach (var file in kvp.Value) 
                builder.AppendLine($"<size=15>   - {file}</size>");
        }
        
        response = StringBuilderPool.Shared.ToStringReturn(builder);
        return true;
    }
    
    /// <summary>
    /// Берёт список файлов и сортирует их в словарь из папок-владельцев этих файлов и самих файлов
    /// </summary>
    private Dictionary<string, List<string>> GroupByOwnerSchematics(List<string> files, bool isSchematic = false)
    {
        Log.Debug($"[{nameof(GroupByOwnerSchematics)}] Input files: {files.Count}");
        Dictionary<string, List<string>> toReturn = new Dictionary<string, List<string>>();
        foreach (var file in files)
        {
            string owner = (isSchematic ? Directory.GetParent(file)?.Name : Path.GetFileName(Path.GetDirectoryName(file))) ?? "ERR";
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (toReturn.TryGetValue(owner, out List<string> owns))
            {
                owns.Add(fileName);
            }
            else
            {
                toReturn[owner] = new List<string>() { fileName };
            }
        }

        Log.Debug($"[{nameof(GroupByOwnerSchematics)}] Output groups: {toReturn.Count}");

        return toReturn;
    }

    private readonly struct MapStatus
    {
        public MapStatus(string mapName, string owner)
        {
            Owner = owner;
            MapName = mapName;
            IsLoaded = MapUtils.LoadedMaps.TryGetValue(MapName, out MapSchematic map);
            IsDirty = map != null && map.IsDirty;
        }

        public readonly string Owner;
        public readonly string MapName;
        public readonly bool IsLoaded;
        public readonly bool IsDirty;

        public readonly override string ToString()
        {
            StringBuilder sb = StringBuilderPool.Shared.Rent(MapUtils.GetColoredMapName(MapName));
            sb.Append($" [<b>{Owner}</b>]");

            if (IsLoaded && !IsDirty)
                sb.Append("<color=green>(loaded, saved)</color>");
            else if (IsLoaded && IsDirty)
                sb.Append("<color=red>(loaded, not saved)</color>");
            else
                sb.Append("<color=grey>(unloaded)</color>");

            return StringBuilderPool.Shared.ToStringReturn(sb);
        }
    }
}