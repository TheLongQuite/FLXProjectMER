using AdminToys;
using LabApi.Features.Wrappers;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Interfaces;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Utility;
using UnityEngine;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using Room = Exiled.API.Features.Room;

namespace ProjectMER.Features.Serializable;

public class SerializableCameraRedirect : SerializableObject, IIndicatorDefinition
{
    public List<TargetTeleporter> TargetCameras { get; set; } = [];
    public string Label { get; set; } = "REDIRECT";

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null,
        bool isForced = false, bool manuallySpawned = false)
    {
        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;

        if (instance == null)
        {
            CameraToy cameraToy = CameraToy.Create(position, rotation);
            cameraToy.Label = $"{Label}_{ObjectId}";
            cameraToy.GameObject.AddComponent<CameraRedirectObject>().Init(this, cameraToy.Camera);
            return cameraToy.GameObject;
        }

        Scp079CameraToy baseToy = instance.GetComponent<Scp079CameraToy>();
        if (!baseToy)
            return instance;

        baseToy.transform.SetPositionAndRotation(position, rotation);
        if (instance.TryGetComponent(out CameraRedirectObject redirectObj))
            redirectObj.UpdateRedirect(this);

        return instance;
    }

    public GameObject SpawnOrUpdateIndicator(Room room, GameObject? instance = null)
    {
        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);

        PrimitiveObjectToy indicator;
        if (instance == null)
        {
            indicator = UnityEngine.Object.Instantiate(PrefabManager.PrimitiveObject);
            indicator.NetworkPrimitiveFlags = PrimitiveFlags.Visible;
            indicator.NetworkPrimitiveType = PrimitiveType.Cube;
            indicator.name = "CameraRedirectIndicator";
        }
        else
        {
            indicator = instance.GetComponent<PrimitiveObjectToy>();
        }

        indicator.transform.SetPositionAndRotation(position, rotation);
        indicator.transform.localScale = Vector3.one * 0.3f;
        indicator.NetworkMaterialColor = TargetCameras.Count > 0
            ? new Color(0.5f, 0f, 1f, 0.6f)
            : new Color(1f, 1f, 1f, 0.25f);

        return indicator.gameObject;
    }
}
