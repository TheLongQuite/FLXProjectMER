using Exiled.Events.Features;
using ProjectMER.Events.Arguments;

namespace ProjectMER.Events.Handlers;

/// <summary>
/// Teleport related events.
/// </summary>
public static class Teleport
{
    /// <summary>
    /// Invoked before teleporting.
    /// </summary>
    public static Event<TeleportingEventArgs> Teleporting { get; set; } = new();

    /// <summary>
    /// Called before teleporting.
    /// </summary>
    /// <param name="ev">The <see cref="TeleportingEventArgs"/> instance.</param>
    public static void OnTeleporting(TeleportingEventArgs ev) => Teleporting.InvokeSafely(ev);
}