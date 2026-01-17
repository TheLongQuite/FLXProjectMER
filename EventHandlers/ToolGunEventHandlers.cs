using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using ProjectMER.Features.ToolGun;

namespace ProjectMER.EventHandlers;

public partial class EventHandlers
{
    private static CoroutineHandle _toolGunCoroutine;

    public void OnServerRoundStartedToolGun()
    {
        Timing.KillCoroutines(_toolGunCoroutine);
        _toolGunCoroutine = Timing.RunCoroutine(ToolGunGUI());
    }

    private static IEnumerator<float> ToolGunGUI()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(0.1f);

            foreach (Player player in Player.List)
            {
                if (!player.CurrentItem.IsToolGun(out ToolGunItem _) &&
                    !ToolGunHandler.TryGetSelectedMapObject(player, out MapEditorObject _))
                    continue;

                string hud;
                try
                {
                    hud = ToolGunUI.GetHintHUD(player);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    hud = "ERROR: Check server console";
                }

                player.ShowHint(hud, 0.25f);
            }
        }
    }

    public void OnPlayerDryFiringWeapon(DryfiringWeaponEventArgs ev)
    {
        if (!ev.Firearm.IsToolGun(out ToolGunItem toolGun))
            return;

        ev.IsAllowed = false;
        toolGun.Shot(ev.Player);
    }

    public void OnPlayerReloadingWeapon(ReloadingWeaponEventArgs ev)
    {
        if (!ev.Firearm.IsToolGun(out ToolGunItem toolGun))
            return;

        ev.IsAllowed = false;
        toolGun.SelectedObjectToSpawn--;
    }

    public void OnPlayerDroppingItem(DroppingItemEventArgs ev)
    {
        if (!ev.Item.IsToolGun(out ToolGunItem toolGun))
            return;

        ev.IsAllowed = false;
        toolGun.SelectedObjectToSpawn++;
    }
}