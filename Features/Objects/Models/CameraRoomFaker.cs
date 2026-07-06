using AdminToys;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;
using Camera = Exiled.API.Features.Camera;

namespace ProjectMER.Features.Objects.Models;

public class CameraRoomFaker(Player player)
{
    public static readonly Dictionary<Player, CameraRoomFaker> Managers = new();

    private HashSet<Scp079CameraToy> _fakedCameras = [];

    public void OnCameraChanged(Scp079Camera newCamera)
    {
        RoomIdentifier currentRoom = newCamera.Room;
        Vector3 currentPos = newCamera.Position;

        HashSet<Scp079CameraToy> currentlyVisibleToys = new();
        foreach (Scp079Camera cam in Scp079InteractableBase.AllInstances.OfType<Scp079Camera>())
        {
            if (!cam.IsToy || cam == newCamera)
                continue;

            if (Vector3.Distance(currentPos, cam.Position) > 25f)
                continue;

            Log.Debug("Камера близко, рассматриваем");
            Scp079CameraToy toy = cam.GetComponent<Scp079CameraToy>();
            if (toy == null)
                continue;

            currentlyVisibleToys.Add(toy);
            Log.Debug("Камера игрушка, фейкуем");
            player.SendFakeSyncVar(toy.netIdentity, typeof(Scp079CameraToy), "NetworkRoom", currentRoom);
        }
        
        foreach (Scp079CameraToy toy in _fakedCameras.Except(currentlyVisibleToys))
            player.SendFakeSyncVar(toy.netIdentity, typeof(Scp079CameraToy), "NetworkRoom", toy.Room);

        _fakedCameras = currentlyVisibleToys;
    }
}