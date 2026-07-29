using Mirror;
using PlayerStatsSystem;
using ProjectMER.Events.Arguments;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace ProjectMER.Features.Components;

public abstract class DamageableComponent : MonoBehaviour, IDestructible
{
    private uint _networkId;
    
    public uint NetworkId => _networkId;
    public Vector3 CenterOfMass => transform.position;
    public SchematicObject SchematicObject { get; set; }
    protected virtual bool ShouldSetHitboxLayer => true;

    public float Health;

    protected virtual void Start()
    {
        SchematicObject = GetComponentInParent<SchematicObject>();
        NetworkIdentity netIdentity = GetComponentInParent<NetworkIdentity>();
        _networkId = netIdentity != null ? netIdentity.netId : 0;

        if (ShouldSetHitboxLayer)
        {
            SetupProxies();
        }
    }

    private void SetupProxies()
    {
        int hitboxLayer = LayerMask.NameToLayer("Hitbox");
        if (hitboxLayer < 0)
            hitboxLayer = LayerMask.NameToLayer("Default");

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.isTrigger) 
                continue;

            if (col.GetComponent<IDestructible>() != null) 
                continue;

            col.gameObject.layer = hitboxLayer;

            DamageableProxy proxy = col.gameObject.AddComponent<DamageableProxy>();
            proxy.Init(this);
        }
    }

    public abstract bool Damage(float damage, DamageHandlerBase handler, Vector3 pos);

    public class DamageableProxy : MonoBehaviour, IDestructible
    {
        private DamageableComponent? _parent;

        public void Init(DamageableComponent parent)
        {
            _parent = parent;
        }

        public uint NetworkId => _parent != null ? _parent.NetworkId : 0;
        public Vector3 CenterOfMass => _parent != null ? _parent.CenterOfMass : transform.position;

        public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
        {
            if (_parent == null) 
                return false;

            SchematicDamagingEventArgs ev = new(_parent.SchematicObject,
                damage, handler.GetDamageType() ?? default);
            
            Schematic.OnSchematicDamaging(ev);
            
            return _parent.Damage(ev.Damage, handler, pos);
        }

        private void OnDestroy() => _parent = null;
    }
}