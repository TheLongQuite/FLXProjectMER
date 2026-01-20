using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Interfaces;
using ProjectMER.Events.Arguments.Interfaces;
using ProjectMER.Features.Objects;

namespace ProjectMER.Events.Arguments;

public class ButtonInteractedEventArgs : EventArgs, IPickupEvent, IPlayerEvent, ISchematicEvent
{
    public ButtonInteractedEventArgs(Pickup button, Player player, SchematicObject schematic, string buttonKey)
    {
        Button = button;
        Player = player;
        Schematic = schematic;
        ButtonKey = buttonKey;
    }

    public Pickup Button { get; }

    public Pickup Pickup => Button;

    public Player Player { get; }

    public SchematicObject Schematic { get; }

    public string ButtonKey { get; }

    public bool CheckButton(string key) => ButtonKey.ToLower().Contains(key.ToLower());
}