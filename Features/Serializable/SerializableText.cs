using AdminToys;
using Exiled.API.Features;
using Mirror;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Interfaces;
using UnityEngine;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;
using TextToy = AdminToys.TextToy;

namespace ProjectMER.Features.Serializable;

public class SerializableText : SerializableObject, IIndicatorDefinition
{
    public string Text { get; set; } = "Custom Text";

    public Vector2 DisplaySize { get; set; } = TextToy.DefaultDisplaySize;

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null,
        bool isForced = false, bool shouldBeOptimized = true)
    {
        TextToy text = instance == null ? UnityEngine.Object.Instantiate(PrefabManager.Text)
            : instance.GetComponent<TextToy>();

        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;

        text.transform.SetPositionAndRotation(position, rotation);
        text.transform.localScale = Scale;
        text.NetworkMovementSmoothing = 60;

        text.Network_textFormat = Text;
        text.Network_displaySize = DisplaySize;

        if (instance == null)
            NetworkServer.Spawn(text.gameObject);

        return text.gameObject;
    }

    public GameObject SpawnOrUpdateIndicator(Room room, GameObject? instance = null)
    {
        PrimitiveObjectToy cube;

        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);

        if (instance == null)
        {
            cube = UnityEngine.Object.Instantiate(PrefabManager.PrimitiveObject);
            cube.NetworkPrimitiveType = PrimitiveType.Cube;
            cube.NetworkPrimitiveFlags = PrimitiveFlags.Visible;
            cube.NetworkMaterialColor = new(1f, 1f, 1f, 0.9f);
            cube.transform.localScale = Vector3.one * 0.25f;
        }
        else
            cube = instance.GetComponent<PrimitiveObjectToy>();

        cube.transform.SetPositionAndRotation(position, rotation);

        return cube.gameObject;
    }
}