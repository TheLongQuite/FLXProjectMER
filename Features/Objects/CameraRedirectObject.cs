using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using ProjectMER.Features.Serializable;
using ProjectMER.Features.Serializable.Utility;
using ProjectMER.Features.ToolGun;
using UnityEngine;
using Weighted_Randomizer;
using Camera = Exiled.API.Features.Toys.CameraToy;
using Room = Exiled.API.Features.Room;

namespace ProjectMER.Features.Objects;

public class CameraRedirectObject : MonoBehaviour
{
    public static readonly Dictionary<Scp079Camera, CameraRedirectObject> Dictionary = new();

    private Scp079Camera? _scpCamera;
    private List<TargetTeleporter> _targets;

    public void Init(List<TargetTeleporter> targets, Camera camera)
    {
        _targets = targets;
        _scpCamera = camera.Base.Camera;
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
        if (Enum.TryParse(targetId, out RoomType type))
            return Room.Get(type).Cameras.GetRandomValue()?.Base;

        if (!MapEditorObject.DictionaryById.TryGetValue(targetId, out MapEditorObject mapEditorObject))
            return null;

        return mapEditorObject.TryGetComponent(out Scp079Camera? cam) ? cam : null;
    }
}