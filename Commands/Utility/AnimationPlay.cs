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
    public SchematicObject SchematicObject;

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission("mpr.create"))
        {
            response = "You don't have permission to execute this command. Required permission: mpr.create";
            return false;
        }
        
        if (arguments.Count != 1)
        {
            response = Description;
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
        bool animationFound = false;
        for (int i = 0; i != animatorCount; i++)
        {
            int animCount = anim.Animators[i].runtimeAnimatorController.animationClips.Length;
            for (int y = 0; y != animCount; y++)
            {
                if (anim.Animators[i].runtimeAnimatorController.animationClips[y].name == arguments.At(0))
                {
                    animationFound = true;
                    anim.Play(arguments.At(0), i);
                }
            }
        }

        if (!animationFound)
        {
            response = $"An animation with the specified name: [{arguments.At(0)}] does not exist.";
            return false;
        }
        response = "Animation have been played!";
        return true;
    }
}