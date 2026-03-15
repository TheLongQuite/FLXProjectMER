using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using ProjectMER.Features.ToolGun;


namespace ProjectMER.Commands.Utility;

public class AnimationPlay : ICommand
{
    public string Command => "play";

    public string[] Aliases => [ "pl" ];

    public string Description => "Play a animation of the selected schematic.";
    public string Des = "Animation_name, Index of the Animator (optional)";
    public SchematicObject SchematicObject;

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission("mpr.create"))
        {
            response = "You don't have permission to execute this command. Required permission: mpr.create";
            return false;
        }
        
        if (arguments.Count < 1)
        {
            response = Des;
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
        
        if (arguments.Count == 2)
        {
            if (!int.TryParse(arguments.At(1), out var animatorIndex))
            {
                response = $"Incorrect index of the animator: {animatorIndex}";
                return false;
            }
            anim.Play(arguments.At(0), animatorIndex);
            response = "All animation's have been played!";
            return true;
        }
        
        
        anim.Play(arguments.At(0));
        
        response = "Animation have been played!";
        return true;
    }
}