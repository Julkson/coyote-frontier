using Robust.Shared.Player;

namespace Content.Shared._Coyote;

/// <summary>
/// This handles...
/// </summary>
public abstract class StationPingShared : EntitySystem;

/// <summary>
/// Something wants to ping this station's crew consoles! See if we can do that.
/// </summary>
public sealed class PingStationEvent(
    EntityUid station,
    ICommonSession? player = null,
    string? message = null,
    string? stationName = null)
    : EntityEventArgs
{
    /// <summary>
    /// The station to ping.
    /// </summary>
    public EntityUid Station = station;

    /// <summary>
    /// The player who requested the ping.
    /// </summary>
    public ICommonSession? Player = player;

    /// <summary>
    /// The Message to send to the crew consoles.
    /// </summary>
    public string? Message = message;

    /// <summary>
    /// The name of the station, if any.
    /// </summary>
    public string? StationName = stationName;
}

/// <summary>
/// Asks the station pretty please can it ping the crew consoles.
/// </summary>
public sealed class CanPingStationEvent(
    EntityUid station,
    bool canPingByGhosts = false)
    : EntityEventArgs
{
    /// <summary>
    /// The station to ping.
    /// </summary>
    public EntityUid Station = station;

    /// <summary>
    /// If the ping can be done by ghosts or not.
    /// </summary>
    public bool CanPingByGhosts = canPingByGhosts;
}
