using AdminToys;
using Exiled.API.Features;
using Mirror;
using ProjectMER.Events.Arguments;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using UnityEngine;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;

namespace ProjectMER.Features.Serializable.Schematics;

public class SerializableSchematic : SerializableObject
{
    public string SchematicName { get; set; } = "None";
    public bool IsStatic { get; set; } = false;

    public override GameObject? SpawnOrUpdateObject(Room? room = null, GameObject? instance = null,
        bool isForced = false, bool manuallySpawned = false)
    {
        PrimitiveObjectToy schematic = instance == null ? UnityEngine.Object.Instantiate(PrefabManager.PrimitiveObject)
            : instance.GetComponent<PrimitiveObjectToy>();

        schematic.NetworkPrimitiveFlags = PrimitiveFlags.None;
        schematic.NetworkMovementSmoothing = 60;

        Vector3 position = room.GetAbsolutePosition(Position);
        Quaternion rotation = room.GetAbsoluteRotation(Rotation);
        _prevIndex = Index;

        schematic.name = $"CustomSchematic-{SchematicName}";
        schematic.transform.SetPositionAndRotation(position, rotation);
        schematic.transform.localScale = Scale;

        if (instance == null)
        {
            _ = MapUtils.TryGetSchematicDataByName(SchematicName, out SchematicObjectDataList? data) ? data : null;

            if (data == null)
            {
                GameObject.Destroy(schematic.gameObject);
                return null;
            }

            SchematicSpawningEventArgs ev = new(data, SchematicName, !manuallySpawned);
            Schematic.OnSchematicSpawning(ev);
            data = ev.Data;

            if (!ev.IsAllowed)
            {
                GameObject.Destroy(schematic.gameObject);
                return null;
            }

            NetworkServer.Spawn(schematic.gameObject);
            schematic.gameObject.AddComponent<SchematicObject>().Init(data, manuallySpawned);
        }

        return schematic.gameObject;
    }
}