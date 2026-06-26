using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Objects.Models;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    public void OnChangingCamera(Scp079ChangingCameraEventArgs ev)
    {
        if (!ev.IsAllowed)
            return;
        
        if (!CameraRedirectObject.Dictionary.TryGetValue(ev.Camera.Base, out CameraRedirectObject redirect))
        {
            if (!CameraRoomFaker.Managers.TryGetValue(ev.Player, out CameraRoomFaker faker))
                return;

            faker.OnCameraChanged(ev.Camera.Base);
            return;
        }

        Scp079Camera? targetCamera = redirect.GetRandomTargetCamera();
        if (targetCamera)
            ev.Camera = Camera.Get(targetCamera);
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
}