using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.ToolGun;
using System.Text;
using NorthwoodLib.Pools;


namespace ProjectMER.Commands.Utility;

public class AnimationList : ICommand
{
    public string Command => "AnimList";

    public string[] Aliases => [ "alist", "ali", "als" ];

    public string Description => "Show all existing animator and it's animation's of the selected schematic.";
    public SchematicObject SchematicObject;

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission("mpr.create"))
        {
            response = "You don't have permission to execute this command. Required permission: mpr.create";
            return false;
        }
        
        Player player = Player.Get(sender);
        
        if (!ToolGunHandler.TryGetSelectedMapObject(player, out var mapObject))
        {
            response = "You need to select an object first!";
            return false;
        }
        SchematicObject = mapObject.gameObject.GetComponent<SchematicObject>();
        AnimationController anim = AnimationController.Get(SchematicObject);
        int animatorCount = anim.Animators.Count;
        StringBuilder sb = StringBuilderPool.Shared.Rent();
        
        for (int i = 0; i != animatorCount; i++)
        {
            sb.AppendLine($"\n<size=21><b>[{i}] <color=yellow>{anim.Animators[i].name}:</color></b></size>\n");
            int animCount = anim.Animators[i].runtimeAnimatorController.animationClips.Length;
            for (int y = 0; y != animCount; y++)
            {
                sb.AppendLine($"({y + 1}) - {anim.Animators[i].runtimeAnimatorController.animationClips[y].name}");
            }
        }
        sb.Insert(0, "\n<u><b>The existing animator list and it's animation's</b></u>\n");
        
        response = StringBuilderPool.Shared.ToStringReturn(sb);
        return true;
    }
}