using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.ToolGun;


namespace ProjectMER.Commands.Utility;

public class AnimationStop : ICommand
{
    public string Command => "stop";

    public string[] Aliases => [];

    public string Description => "Stop a animation of the selected schematic.";
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

        if (animatorCount != 1 && arguments.Count == 1)
        {
            if (!int.TryParse(arguments.At(0), out var index))
            {
                response = $"Incorrect index: {index}";
                return false;
            }
            anim.Animators[index].enabled = !anim.Animators[index].enabled;
            response = anim.Animators[index].enabled ? $"Animator with index: {index} have been played!" : $"Animator with index: {index} have been stooped!";
            return true;
        }
        
        for (int i = 0; i != animatorCount; i++)
        {
           anim.Animators[i].enabled = !anim.Animators[i].enabled;
        }
        response = anim.Animators[0].enabled ? "All animation's have been played!" : "All animation's have been stooped!";
        return true;
    }
}