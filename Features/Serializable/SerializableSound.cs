using AdminToys;
using Exiled.API.Features;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Interfaces;
using ProjectMER.Features.Objects;
using UnityEngine;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;

namespace ProjectMER.Features.Serializable;

public class SerializableSound : SerializableObject, IIndicatorDefinition
{
    public string SoundName { get; set; } = "";
    public byte Volume { get; set; } = 100;
    public float Radius { get; set; } = 30f;
    public float MinRadius { get; set; } = 1f;
    public bool Loop { get; set; } = true;

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null,
        bool isForced = false, bool manuallySpawned = false)
    {
        GameObject gameObject = instance ?? new GameObject("Sound");
        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;
        gameObject.transform.SetLocalPositionAndRotation(position, rotation);

        if (!instance)
            gameObject.AddComponent<SoundObject>().Init(this);
        else if (gameObject.TryGetComponent(out SoundObject soundObj))
            soundObj.UpdateSound(this);

        return gameObject;
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
            indicator.NetworkPrimitiveType = PrimitiveType.Sphere;
            indicator.name = "SoundIndicator";
        }
        else
        {
            indicator = instance.GetComponent<PrimitiveObjectToy>();
        }

        indicator.transform.SetPositionAndRotation(position, rotation);
        indicator.transform.localScale = Vector3.one * 0.5f;
        indicator.NetworkMaterialColor = new Color(1f, 0.5f, 0f, 0.5f);

        return indicator.gameObject;
    }
}
