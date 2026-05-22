using Exiled.API.Features;
using HarmonyLib;
using MEC;
using ProjectMER.Configs;
using ProjectMER.Features;

namespace ProjectMER;

public class ProjectMER : Plugin<Config>
{
    private Harmony _harmony;
    private FileSystemWatcher _mapFileSystemWatcher;

    public static ProjectMER Singleton { get; private set; }

    /// <summary>
    /// Gets the folder path in which the maps are stored.
    /// </summary>
    public static string MapsDir { get; private set; }

    /// <summary>
    /// Gets the folder path in which the schematics are stored.
    /// </summary>
    public static string SchematicsDir { get; private set; }

    private EventHandlers.EventHandlers _ev;

    public override void OnEnabled()
    {
        _ev = new();
        Singleton = this;

        MapsDir = Path.Combine(Config.PluginFilesDir, "Maps");
        SchematicsDir = Path.Combine(Config.PluginFilesDir, "Schematics");

        if (!Directory.Exists(Config.PluginFilesDir))
        {
            Log.Warn("Plugin directory does not exist. Creating...");
            Directory.CreateDirectory(Config.PluginFilesDir);
        }

        if (!Directory.Exists(MapsDir))
        {
            Log.Warn("Maps directory does not exist. Creating...");
            Directory.CreateDirectory(MapsDir);
        }

        if (!Directory.Exists(SchematicsDir))
        {
            Log.Warn("Schematics directory does not exist. Creating...");
            Directory.CreateDirectory(SchematicsDir);
        }

        LabApi.Events.Handlers.WarheadEvents.Started += _ev.OnWarheadStarted;
        LabApi.Events.Handlers.WarheadEvents.Stopped += _ev.OnWarheadStopped;
        LabApi.Events.Handlers.WarheadEvents.Detonated += _ev.OnWarheadDetonated;
        LabApi.Events.Handlers.ServerEvents.LczDecontaminationStarted += _ev.OnServerLczDecontaminationStarted;
        LabApi.Events.Handlers.PlayerEvents.Spawned += _ev.OnPlayerSpawning;
        LabApi.Events.Handlers.PlayerEvents.InteractingShootingTarget += _ev.OnPlayerInteractingShootingTarget;
        LabApi.Events.Handlers.Scp079Events.ChangingCamera += _ev.OnChangingCamera;

        Exiled.Events.Handlers.Player.SearchingPickup += _ev.OnPlayerSearchingPickup;
        Exiled.Events.Handlers.Player.PickingUpItem += _ev.OnPlayerPickingUpItem;
        Exiled.Events.Handlers.Player.DryfiringWeapon += _ev.OnPlayerDryFiringWeapon;
        Exiled.Events.Handlers.Player.ReloadingWeapon += _ev.OnPlayerReloadingWeapon;
        Exiled.Events.Handlers.Player.DroppingItem += _ev.OnPlayerDroppingItem;

        Exiled.Events.Handlers.Server.RoundStarted += _ev.OnServerRoundStartedAction;
        Exiled.Events.Handlers.Server.RoundStarted += _ev.OnServerRoundStartedToolGun;

        Exiled.Events.Handlers.Server.WaitingForPlayers += _ev.OnServerWaitingForPlayersAction;
        Exiled.Events.Handlers.Server.WaitingForPlayers += _ev.OnServerWaitingForPlayersGeneric;

        _harmony = new($"michal78900.mapEditorReborn-{DateTime.Now.Ticks}");
        _harmony.PatchAll();

        if (Config!.EnableFileSystemWatcher)
        {
            _mapFileSystemWatcher = new(MapsDir)
            {
                NotifyFilter = NotifyFilters.LastWrite, Filter = "*.yml", EnableRaisingEvents = true
            };

            _mapFileSystemWatcher.Changed += OnMapFileChanged;

            Log.Debug("FileSystemWatcher enabled!");
        }
    }

    private void OnMapFileChanged(object _, FileSystemEventArgs ev)
    {
        string mapName = ev.Name.Split('.')[0];
        if (!MapUtils.LoadedMaps.ContainsKey(mapName))
            return;

        Timing.CallDelayed(0.01f, () =>
        {
            try
            {
                MapUtils.LoadMap(mapName);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        });
    }

    public override string Name => "ProjectMER";

    public override string Author => "Michal78900";
    public override Version Version => new(2025, 11, 2, 1);
}