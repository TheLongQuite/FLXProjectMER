using PlayerRoles.PlayableScps.Scp079.Cameras;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Utility;
using ProjectMER.Features.ToolGun;
using UnityEngine;
using Weighted_Randomizer;
using Camera = LabApi.Features.Wrappers.Camera;

namespace ProjectMER.Features.Objects;

public class CameraRedirectObject : MonoBehaviour
{
    public static readonly Dictionary<Camera, CameraRedirectObject> Dictionary = new();

    private Camera? _scpCamera;
    private List<TargetTeleporter> _targets;

    public void Init(SerializableCameraRedirect serializable, Camera camera)
    {
        _targets = serializable.TargetCameras;
        _scpCamera = camera;
        if (_scpCamera != null)
            Dictionary[_scpCamera] = this;
    }

    public void UpdateRedirect(SerializableCameraRedirect serializable) => _targets = serializable.TargetCameras;

    public Scp079Camera? GetRandomTargetCamera()
    {
        if (_targets.Count == 0)
            return null;

        StaticWeightedRandomizer<string> weighted = new();
        foreach (TargetTeleporter target in _targets)
        {
            int chance = target.Chance <= 0 ? 1 : target.Chance;
            weighted.Add(target.Id, chance);
        }

        string targetId = weighted.NextWithReplacement();
        if (!ToolGunHandler.TryGetObjectById(targetId, out MapEditorObject mapEditorObject))
            return null;

        Scp079Camera camera = mapEditorObject.GetComponent<Scp079Camera>();
        return camera;
    }

    private void OnDestroy()
    {
        if (_scpCamera != null)
            Dictionary.Remove(_scpCamera);
    }
}