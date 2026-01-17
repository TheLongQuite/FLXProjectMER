using System.Text.RegularExpressions;
using Exiled.API.Features;
using LabApi.Events.Arguments.WarheadEvents;
using MEC;
using NorthwoodLib.Pools;
using ProjectMER.Configs;
using ProjectMER.Features;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    private static Config Config => ProjectMER.Singleton.Config!;

    public void OnServerWaitingForPlayersAction()
        => Timing.CallDelayed(0.1f, () => HandleActionList(Config.OnWaitingForPlayers));

    public void OnServerRoundStartedAction() => HandleActionList(Config.OnRoundStarted);
    public void OnServerLczDecontaminationStarted() => HandleActionList(Config.OnLczDecontaminationStarted);
    public void OnWarheadStarted(WarheadStartedEventArgs ev) => HandleActionList(Config.OnWarheadStarted);
    public void OnWarheadStopped(WarheadStoppedEventArgs ev) => HandleActionList(Config.OnWarheadStopped);
    public void OnWarheadDetonated(WarheadDetonatedEventArgs ev) => HandleActionList(Config.OnWarheadDetonated);

    private void HandleActionList(List<string> list)
    {
        foreach (string element in list)
        {
            string[] actionSplit = element.Split(':');
            string action = actionSplit[0];
            string argument = actionSplit[1];

            switch (action.ToLowerInvariant())
            {
                case "load":
                case "l":
                {
                    List<string> allMaps = ListPool<string>.Shared.Rent(Directory.GetFiles(ProjectMER.MapsDir)
                        .Select(Path.GetFileNameWithoutExtension));

                    HandleMapLoading(argument, allMaps);
                    ListPool<string>.Shared.Return(allMaps);
                    continue;
                }

                case "unload":
                case "unl":
                {
                    List<string> allMaps = ListPool<string>.Shared.Rent(MapUtils.LoadedMaps.Keys);
                    HandleMapUnloading(argument, allMaps);
                    ListPool<string>.Shared.Return(allMaps);
                    continue;
                }

                case "console":
                case "cs":
                {
                    Server.ExecuteCommand(argument);
                    continue;
                }

                default:
                {
                    Log.Error($"Unknown action: {action}");
                    continue;
                }
            }
        }
    }

    private void HandleMapLoading(string argument, List<string> allMaps)
    {
        string[] orSplit = argument.Split('|', '|');
        string[] andSplit = argument.Split(',');

        if (orSplit.Length > 1 || andSplit.Length > 1)
        {
            if (andSplit.Length > orSplit.Length)
                andSplit.ForEach(x => HandleMapLoading(x, allMaps));
            else
                HandleMapLoading(orSplit.RandomItem(), allMaps);

            return;
        }

        foreach (string mapName in allMaps)
        {
            if (Regex.IsMatch(mapName, WildCardToRegular(argument)))
                MapUtils.LoadMap(mapName);
        }
    }

    private void HandleMapUnloading(string argument, List<string> allMaps)
    {
        string[] orSplit = argument.Split('|', '|');
        string[] andSplit = argument.Split(',');

        if (orSplit.Length > 1 || andSplit.Length > 1)
        {
            if (andSplit.Length > orSplit.Length)
                andSplit.ForEach(x => HandleMapLoading(x, allMaps));
            else
                HandleMapLoading(orSplit.RandomItem(), allMaps);

            return;
        }

        foreach (string mapName in allMaps)
        {
            if (Regex.IsMatch(mapName, WildCardToRegular(argument)))
                MapUtils.UnloadMap(mapName);
        }
    }

    private static string WildCardToRegular(string value)
        => "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
}