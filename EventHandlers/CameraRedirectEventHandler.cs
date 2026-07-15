using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Objects.Models;
using Object = UnityEngine.Object;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    public void OnChangedCamera(Scp079ChangedCameraEventArgs ev)
    {
        Scp079CurrentCameraSync cameraSync = ((PlayerRoles.PlayableScps.Scp079.Scp079Role)ev.Player.RoleBase)
            ._curCamSync;
        if (CameraRedirectObject.Dictionary.TryGetValue(ev.Camera.Base, out CameraRedirectObject redirect))
        {
            Scp079Camera? targetCamera = redirect.GetRandomTargetCamera();
            if (targetCamera)
                cameraSync.CurrentCamera = Camera.Get(targetCamera).Base;
        }
        
        if (!CameraRoomFaker.Managers.TryGetValue(ev.Player, out CameraRoomFaker faker))
            return;

        faker.OnCameraChanged(cameraSync.CurrentCamera);
    }

    public void OnSpawned(SpawnedEventArgs ev)
    {
        if (ev.Player.Role is not Scp079Role scp079Role)
        {
            CameraRoomFaker.Managers.Remove(ev.Player);
            return;
        }

        if (CameraRoomFaker.Managers.ContainsKey(ev.Player))
            return;

        CameraRoomFaker manager = new(ev.Player);
        CameraRoomFaker.Managers[ev.Player] = manager;
        Timing.CallDelayed(0.5f, () => manager.OnCameraChanged(scp079Role.Camera.Base));
    }

    public void OnServerWaitingForPlayersRedirect()
    {
        foreach (CameraRedirectObject obj in CameraRedirectObject.Dictionary.Values)
            Object.Destroy(obj);

        CameraRedirectObject.Dictionary.Clear();
        CameraRoomFaker.Managers.Clear();
    }
}