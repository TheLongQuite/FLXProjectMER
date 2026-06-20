using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features.Toys;
using Mirror;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Interfaces;
using ProjectMER.Features.Objects;
using ProjectMER.Features.Serializable.Utility;
using UnityEngine;
using CameraToy = Exiled.API.Features.Toys.CameraToy;
using CameraType = ProjectMER.Features.Enums.CameraType;
using Object = UnityEngine.Object;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using Room = Exiled.API.Features.Room;

namespace ProjectMER.Features.Serializable;

public class SerializableCameraRedirect : SerializableObject, IIndicatorDefinition
{
    public List<TargetTeleporter> TargetCameras { get; set; } = [];
    public CameraType CameraType { get; set; } = CameraType.Lcz;
    public string Label { get; set; } = "CustomCamera";

    private Scp079CameraToy CameraPrefab
    {
        get
        {
            Scp079CameraToy prefab = CameraType switch
            {
                CameraType.Lcz => PrefabManager.CameraLcz,
                CameraType.Hcz => PrefabManager.CameraHcz,
                CameraType.Ez => PrefabManager.CameraEz,
                CameraType.EzArm => PrefabManager.CameraEzArm,
                CameraType.Sz => PrefabManager.CameraSz,
                _ => throw new InvalidOperationException()
            };

            return prefab;
        }
    }
    
    public GameObject SpawnOrUpdateObject(Room room, GameObject? instance = null,
        bool isForced = false, bool manuallySpawned = false)
    {
        Scp079CameraToy cameraToy;
        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;
        
        if (instance == null)
        {
            cameraToy = Object.Instantiate(CameraPrefab);
            cameraToy.Label = Label;
            cameraToy.gameObject.AddComponent<CameraRedirectObject>().Init(TargetCameras, 
                AdminToy.Get<CameraToy>(cameraToy));
        }
        else
            cameraToy = instance.GetComponent<Scp079CameraToy>();

        cameraToy.transform.SetPositionAndRotation(position, rotation);
        cameraToy.transform.localScale = Scale;
        cameraToy.NetworkScale = cameraToy.transform.localScale;
        
        cameraToy.NetworkMovementSmoothing = 60;
        cameraToy.NetworkLabel = Label;
        cameraToy.NetworkRoom =
            room == null ? Room.Get(RoomType.Surface).Identifier : room.Identifier;
        
        if (instance.TryGetComponent(out CameraRedirectObject redirectObj))
            redirectObj.UpdateRedirect(this);
        
        if (instance == null)
            NetworkServer.Spawn(cameraToy.gameObject);

        return cameraToy.gameObject;
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
    
    public override bool RequiresReloading => CameraType != _prevType || base.RequiresReloading;
    
    internal CameraType _prevType;
}
