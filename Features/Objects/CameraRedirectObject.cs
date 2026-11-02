using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079;
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

    /// <summary>
    /// Цель «UP»: ближайшая ванильная камера своей комнаты выше камеры-перехода — подъём из тех. туннеля
    /// в комнату над ним. Комната берётся у самой камеры, поэтому работает и в схематиках с index: -1.
    /// </summary>
    public const string UpTargetId = "UP";

    private Scp079Camera? _scpCamera;
    private List<TargetTeleporter> _targets;

    /// <summary>
    /// Камеры своего экземпляра схематика по UniqueId. Схематик с index: -1 стоит в нескольких комнатах,
    /// а в общем <see cref="MapEditorObject.DictionaryById"/> остаётся только последний экземпляр камеры.
    /// </summary>
    private readonly Dictionary<string, Scp079Camera> _schematicCameras = new();

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
        if (targetId == UpTargetId)
            return GetUpperRoomCamera();

        if (Enum.TryParse(targetId, out RoomType type))
            return Room.Get(type).Cameras.GetRandomValue()?.Base;

        if (TryGetSchematicCamera(targetId, out Scp079Camera? sibling))
            return sibling;

        if (!MapEditorObject.DictionaryById.TryGetValue(targetId, out MapEditorObject mapEditorObject))
            return null;

        return mapEditorObject.TryGetComponent(out Scp079Camera? cam) ? cam : null;
    }

    private bool TryGetSchematicCamera(string targetId, out Scp079Camera? camera)
    {
        if (_schematicCameras.TryGetValue(targetId, out camera) && camera != null)
            return true;

        camera = null;
        SchematicObject? schematic = GetComponentInParent<SchematicObject>();
        if (schematic == null)
            return false;

        foreach (MapEditorObject mapEditorObject in schematic.GetComponentsInChildren<MapEditorObject>())
        {
            if (mapEditorObject.Id != targetId || !mapEditorObject.TryGetComponent(out Scp079Camera found))
                continue;

            _schematicCameras[targetId] = found;
            camera = found;
            return true;
        }

        return false;
    }

    private Scp079Camera? GetUpperRoomCamera()
    {
        if (_scpCamera == null)
            return null;

        RoomIdentifier room = _scpCamera.Room;
        Vector3 position = _scpCamera.Position;
        Scp079Camera? best = null;
        float bestDistance = float.MaxValue;
        foreach (Scp079InteractableBase interactable in Scp079InteractableBase.AllInstances)
        {
            if (interactable is not Scp079Camera { IsToy: false } cam || cam.Room != room || cam.Position.y < position.y)
                continue;

            float distance = (cam.Position - position).sqrMagnitude;
            if (distance >= bestDistance)
                continue;

            best = cam;
            bestDistance = distance;
        }

        return best;
    }
}