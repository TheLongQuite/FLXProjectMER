using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using FLXLib.Extensions;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.ThrowableProjectiles;
using ProjectMER.Events.Arguments;
using ProjectMER.Events.Handlers;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace ProjectMER.Features.Components;

using Firearm = InventorySystem.Items.Firearms.Firearm;
using PlayerEv = Exiled.Events.Handlers.Player;
using MapEv = Exiled.Events.Handlers.Map;

public class DamageableComponent : MonoBehaviour
{
    private SchematicObject _schematic;
    public float Health { get; set; }

    public void Start()
    {
        _schematic = gameObject.GetComponent<SchematicObject>();
        SubscribeEvents();
    }

    public void OnDestroy() => UnsubscribeEvents();

    public void SubscribeEvents()
    {
        PlayerEv.Shot += OnShot;
        MapEv.ExplodingGrenade += OnExploding;
    }

    public void UnsubscribeEvents()
    {
        PlayerEv.Shot -= OnShot;
        MapEv.ExplodingGrenade -= OnExploding;
    }

    private void OnShot(ShotEventArgs ev)
    {
        SchematicObject comp = ev.RaycastHit.transform.GetComponentInParent<SchematicObject>();
        if (!comp || _schematic != comp)
            return;

        Firearm firearmBase = ev.Firearm.Base;
        firearmBase.TryGetModule<HitscanHitregModuleBase>(out HitscanHitregModuleBase hitregModule);
        float damage = BodyArmorUtils.ProcessDamage(1, hitregModule.DamageAtDistance(ev.Distance),
            Mathf.RoundToInt(hitregModule.EffectivePenetration * 100f));

        if (!DamageTypeExtensions.ItemConversion.TryGetValue(ev.Firearm.Type, out DamageType damageType))
            damageType = DamageType.Firearm;

        Log.Debug($"Schematic {_schematic.Name} has been shot for {damage} with {damageType} ({
            ev.Firearm.GetCustomOrBasicType()})");

        Damage(damage, damageType, ev.Player);
    }

    private void OnExploding(ExplodingGrenadeEventArgs ev)
    {
        Log.Debug("Что-то взорвалось");

        if (ev.Projectile?.Base is not ExplosionGrenade grenade ||
            ev.Player?.CurrentItem?.Type == ItemType.ParticleDisruptor) // Ибо эта штука вызывает взрыв, как гранаты
            return;

        float damage = grenade._playerDamageOverDistance.Evaluate(Vector3.Distance(transform.position, ev.Position));
        if (damage <= 0)
            return;


        Log.Debug($"Schematic {_schematic.Name} has been exploded with {ev.Projectile.GetCustomOrBasicType()}");
        Damage(damage, DamageType.Explosion, ev.Player);
    }

    private void Damage(float damage, DamageType damageType, Player attacker)
    {
        Log.Debug($"Object has been damaged by {damage}");
        SchematicDamagingEventArgs schematicDamagingEventArgs = new(_schematic, _schematic.Name, damage, damageType);
        Schematic.OnSchematicDamaging(schematicDamagingEventArgs);
        if (!schematicDamagingEventArgs.IsAllowed)
        {
            Log.Debug($"Schematic {_schematic.Name} damage denied by event");
            return;
        }

        Health -= schematicDamagingEventArgs.Damage;
        attacker?.ShowHitMarker(0.7f);

        if (Health > 0)
            return;

        Health = 0;
        Destroy(gameObject);
    }
}