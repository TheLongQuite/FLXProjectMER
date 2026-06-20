using LabApi.Events.Arguments.Scp079Events;
using LabApi.Features.Wrappers;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using ProjectMER.Features.Objects;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    public void OnChangingCamera(Scp079ChangingCameraEventArgs ev)
    {
        if (!CameraRedirectObject.Dictionary.TryGetValue(ev.Camera.Base, out CameraRedirectObject redirect))
            return;

        Scp079Camera? targetCamera = redirect.GetRandomTargetCamera();
        if (targetCamera)
            ev.Camera = Camera.Get(targetCamera);
    }
}