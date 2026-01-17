using Exiled.API.Features;
using LabApi.Events.Arguments.PlayerEvents;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.ToolGun;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    public void OnServerWaitingForPlayersGeneric()
    {
        PrefabManager.RegisterPrefabs();

        MapUtils.LoadedMaps.Clear();
        ToolGunItem.ItemDictionary.Clear();
        ToolGunHandler.PlayerSelectedObjectDict.Clear();
        ButtonPickups.Clear();
        PickupUsesLeft.Clear();
    }

    public void OnPlayerSpawning(PlayerSpawningEventArgs ev)
    {
        if (!ev.UseSpawnPoint)
            return;

        List<MapEditorObject> list = [];
        foreach (MapSchematic map in MapUtils.LoadedMaps.Values)
        {
            foreach (KeyValuePair<string, SerializablePlayerSpawnpoint> spawnpoint in map.PlayerSpawnpoints)
            {
                if (!spawnpoint.Value.Roles.Contains(ev.Role.RoleTypeId))
                    continue;

                list.AddRange(map.SpawnedObjects.Where(x => x.Id == spawnpoint.Key));
            }
        }

        if (list.Count == 0)
            return;

        MapEditorObject randomElement = list[UnityEngine.Random.Range(0, list.Count)];

        ev.SpawnLocation = randomElement.transform.position;
        Timing.CallDelayed(0.05f, () =>
        {
            try
            {
                ev.Player.LookRotation = randomElement.transform.eulerAngles;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        });
    }

    public void OnPlayerInteractingShootingTarget(PlayerInteractingShootingTargetEventArgs ev)
    {
        if (ev.ShootingTarget.GameObject.TryGetComponent(out MapEditorObject _))
            ev.IsAllowed = false;
    }
}