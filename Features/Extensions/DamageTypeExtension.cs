using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.API.Features.Items;
using InventorySystem.Items.Scp1509;
using PlayerRoles.PlayableScps.Scp1507;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using PlayerStatsSystem;
using DamageHandlerBase = PlayerStatsSystem.DamageHandlerBase;

namespace ProjectMER.Features.Extensions;

public static class DamageTypeExtension
{
    public static DamageType? GetDamageType(this DamageHandlerBase damageHandler)
    {
        switch (damageHandler)
        {
            case GenericDamageHandler genericDamageHandler:
                return GetDamageType(genericDamageHandler.Base);
            case CustomReasonDamageHandler:
                return DamageType.Custom;
            case WarheadDamageHandler:
                return DamageType.Warhead;
            case ExplosionDamageHandler:
                return DamageType.Explosion;
            case Scp018DamageHandler:
                return DamageType.Scp018;
            case Scp096DamageHandler:
                return DamageType.Scp096;
            case RecontainmentDamageHandler:
                return DamageType.Recontainment;
            case MicroHidDamageHandler:
                return DamageType.MicroHid;
            case DisruptorDamageHandler:
                return DamageType.ParticleDisruptor;
            case Scp939DamageHandler scp939DamageHandler:
                return scp939DamageHandler.Scp939DamageType switch
                {
                    Scp939DamageType.Claw => DamageType.Scp939Claw,
                    Scp939DamageType.LungeTarget => DamageType.Scp939LungeTarget,
                    Scp939DamageType.LungeSecondary => DamageType.Scp939LungeSecondary,
                    _ => DamageType.Scp939,
                };
            case JailbirdDamageHandler:
                return DamageType.Jailbird;
            case Scp1507DamageHandler:
                return DamageType.Scp1507;
            case Scp956DamageHandler:
                return DamageType.Scp956;
            case SnowballDamageHandler:
                return DamageType.SnowBall;
            case GrayCandyDamageHandler:
                return DamageType.GrayCandy;
            case Scp1509DamageHandler:
                return DamageType.Scp1509;
            case Scp3114DamageHandler scp3114DamageHandler:
                return scp3114DamageHandler.Subtype switch
                {
                    Scp3114DamageHandler.HandlerType.Strangulation => DamageType.Strangled,
                    Scp3114DamageHandler.HandlerType.SkinSteal => DamageType.Scp3114,
                    Scp3114DamageHandler.HandlerType.Slap => DamageType.Scp3114,
                    _ => DamageType.Unknown,
                };
            case Scp049DamageHandler scp049DamageHandler:
                return scp049DamageHandler.DamageSubType switch
                {
                    Scp049DamageHandler.AttackType.CardiacArrest => DamageType.CardiacArrest,
                    Scp049DamageHandler.AttackType.Instakill => DamageType.Scp049,
                    Scp049DamageHandler.AttackType.Scp0492 => DamageType.Scp0492,
                    _ => DamageType.Unknown,
                };
            case UniversalDamageHandler universal:
                DeathTranslation translation = DeathTranslations.TranslationsById[universal.TranslationId];

                if (DamageTypeExtensions.TranslationIdConversion.ContainsKey(translation.Id))
                    return DamageTypeExtensions.TranslationIdConversion[translation.Id];

                Log.Warn($"{nameof(DamageHandler)}: No matching {nameof(DamageType)} for {nameof(UniversalDamageHandler)} with ID {translation.Id}, type will be reported as {DamageType.Unknown}. Report this to EXILED Devs.");
                break;
            case PlayerStatsSystem.FirearmDamageHandler firearmDamageHandler:
                return Item.Get<Firearm>(firearmDamageHandler.Firearm).FirearmType switch
                {
                    FirearmType.A7 => DamageType.A7,
                    FirearmType.Com15 => DamageType.Com15,
                    FirearmType.Com18 => DamageType.Com18,
                    FirearmType.Crossvec => DamageType.Crossvec,
                    FirearmType.Logicer => DamageType.Logicer,
                    FirearmType.Revolver => DamageType.Revolver,
                    FirearmType.Scp127 => DamageType.Scp127,
                    FirearmType.Shotgun => DamageType.Shotgun,
                    FirearmType.AK => DamageType.AK,
                    FirearmType.ParticleDisruptor => DamageType.ParticleDisruptor,
                    FirearmType.E11SR => DamageType.E11Sr,
                    FirearmType.FSP9 => DamageType.Fsp9,
                    FirearmType.FRMG0 => DamageType.Frmg0,
                    _ => DamageType.Firearm
                };

            case PlayerStatsSystem.AttackerDamageHandler attackerDamageHandler:
                {
                    if (Player.TryGet(attackerDamageHandler.Attacker, out Player attacker) && attacker.CurrentItem?.Type == ItemType.MarshmallowItem)
                        return DamageType.Marshmallow;

                    break;
                }
        }

        return null;
    }
}